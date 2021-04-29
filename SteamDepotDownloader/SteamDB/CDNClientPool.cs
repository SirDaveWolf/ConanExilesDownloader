using SteamKit2;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ConanExilesDownloader.SteamDB
{
    /// <summary>
    /// CDNClientPool provides a pool of connections to CDN endpoints, requesting CDN tokens as needed
    /// </summary>
    class CDNClientPool
    {
        public CDNClient CDNClient { get; }

        private const Int32 ServerEndpointMinimumSize = 8;

        private readonly UInt32 _appId;

#if STEAMKIT_UNRELEASED
        public CDNClient.Server ProxyServer { get; private set; }
#endif

        private readonly ConcurrentStack<CDNClient.Server> _activeConnectionPool;
        private readonly BlockingCollection<CDNClient.Server> _availableServerEndpoints;

        private readonly AutoResetEvent _populatePoolEvent;
        private readonly Task _monitorTask;
        private readonly CancellationTokenSource _shutdownToken;

        private ConcurrentDictionary<String, Int32> ContentServerPenalty { get; set; } = new ConcurrentDictionary<String, Int32>();

        public CancellationTokenSource ExhaustedToken { get; set; }

        public CDNClientPool(UInt32 appId)
        {
            this._appId = appId;

            CDNClient = new CDNClient(Program.SteamSession.Client);

            _activeConnectionPool = new ConcurrentStack<CDNClient.Server>();
            _availableServerEndpoints = new BlockingCollection<CDNClient.Server>();

            _populatePoolEvent = new AutoResetEvent(true);
            _shutdownToken = new CancellationTokenSource();

            _monitorTask = Task.Factory.StartNew(ConnectionPoolMonitorAsync).Unwrap();
        }

        public void Shutdown()
        {
            _shutdownToken.Cancel();
            _monitorTask.Wait();
        }

        private async Task<IReadOnlyCollection<CDNClient.Server>> FetchBootstrapServerListAsync()
        {
            var backoffDelay = 0;

            while (!_shutdownToken.IsCancellationRequested)
            {
                try
                {
                    var cdnServers = await ContentServerDirectoryService.LoadAsync(Program.SteamSession.Client.Configuration);
                    if (cdnServers != null)
                    {
                        return cdnServers;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to retrieve content server list: {0}", ex.Message);

                    if (ex is SteamKitWebRequestException e && e.StatusCode == (HttpStatusCode)429)
                    {
                        // If we're being throttled, add a delay to the next request
                        backoffDelay = Math.Min(5, ++backoffDelay);
                        await Task.Delay(TimeSpan.FromSeconds(backoffDelay));
                    }
                }
            }

            return null;
        }

        private async Task ConnectionPoolMonitorAsync()
        {
            var didPopulate = false;

            while (!_shutdownToken.IsCancellationRequested)
            {
                _populatePoolEvent.WaitOne(TimeSpan.FromSeconds(1));

                // We want the Steam session so we can take the CellID from the session and pass it through to the ContentServer Directory Service
                if (_availableServerEndpoints.Count < ServerEndpointMinimumSize && Program.SteamSession.Client.IsConnected)
                {
                    var servers = await FetchBootstrapServerListAsync().ConfigureAwait(false);

                    if (servers == null || servers.Count == 0)
                    {
                        ExhaustedToken?.Cancel();
                        return;
                    }

#if STEAMKIT_UNRELEASED
                    ProxyServer = servers.Where(x => x.UseAsProxy).FirstOrDefault();
#endif

                    var weightedCdnServers = servers
                        .Where(server =>
                        {
#if STEAMKIT_UNRELEASED
                            var isEligibleForApp = server.AllowedAppIds == null || server.AllowedAppIds.Contains(appId);
                            return isEligibleForApp && (server.Type == "SteamCache" || server.Type == "CDN");
#else
                            return server.Type == "SteamCache" || server.Type == "CDN";
#endif
                        })
                        .Select(server =>
                        {
                            ContentServerPenalty.TryGetValue(server.Host, out var penalty);

                            return (server, penalty);
                        })
                        .OrderBy(pair => pair.penalty).ThenBy(pair => pair.server.WeightedLoad);

                    foreach (var (server, weight) in weightedCdnServers)
                    {
                        for (var i = 0; i < server.NumEntries; i++)
                        {
                            _availableServerEndpoints.Add(server);
                        }
                    }

                    didPopulate = true;
                }
                else if (_availableServerEndpoints.Count == 0 
                    && false == Program.SteamSession.Client.IsConnected 
                    && didPopulate)
                {
                    ExhaustedToken?.Cancel();
                    return;
                }
            }
        }

        private CDNClient.Server BuildConnection(CancellationToken token)
        {
            if (_availableServerEndpoints.Count < ServerEndpointMinimumSize)
            {
                _populatePoolEvent.Set();
            }

            return _availableServerEndpoints.Take(token);
        }

        public CDNClient.Server GetConnection(CancellationToken token)
        {
            if (!_activeConnectionPool.TryPop(out var connection))
            {
                connection = BuildConnection(token);
            }

            return connection;
        }

        public async Task<string> AuthenticateConnection(UInt32 appId, UInt32 depotId, CDNClient.Server server)
        {
            var host = ResolveCDNTopLevelHost(server.Host);
            var cdnKey = $"{depotId:D}:{host}";

            var result = await Program.SteamSession.RequestCDNAuthToken(appId, depotId, host, cdnKey);
            if (result)
            {

                if (Program.SteamSession.CDNAuthTokens.TryGetValue(cdnKey, out var authTokenCallbackPromise))
                {
                    var taskResult = await authTokenCallbackPromise.Task;
                    return taskResult.Token;
                }
                else
                {
                    throw new Exception($"Failed to retrieve CDN token for server {server.Host} depot {depotId}");
                }
            }
            else
            {
                throw new Exception("Unable to request CDN Auth Token!");
            }
        }

        public string ResolveCDNTopLevelHost(string host)
        {
            // SteamPipe CDN shares tokens with all hosts
            if (host.EndsWith(".steampipe.steamcontent.com"))
            {
                return "steampipe.steamcontent.com";
            }
            else if (host.EndsWith(".steamcontent.com"))
            {
                return "steamcontent.com";
            }

            return host;
        }

        public void ReturnConnection(CDNClient.Server server)
        {
            if (server == null) return;

            _activeConnectionPool.Push(server);
        }

        public void ReturnBrokenConnection(CDNClient.Server server)
        {
            if (server == null) return;

            // Broken connections are not returned to the pool
        }
    }
}
