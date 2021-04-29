using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConanExilesDownloader.Logging;
using SteamKit2;

namespace ConanExilesDownloader.SteamDB
{
    internal class SteamSession
    {
        private SteamClient _steamClient;
        public SteamClient Client { get => _steamClient; }

        private SteamUser _steamUser;
        private CallbackManager _steamCallbackManager;

        private SteamApps _steamApps;
        private SteamCloud _steamCloud;

        private readonly TimeSpan STEAM_TIMEOUT = TimeSpan.FromSeconds(30);

        public delegate void LogOffAction();
        private LogOffAction _onLogOff;
        private Boolean _needReconnect = false;

        public ReadOnlyCollection<SteamApps.LicenseListCallback.License> Licenses { get; private set; }
        public Dictionary<UInt32, UInt64> PackageTokens { get; private set; } = new Dictionary<UInt32, UInt64>();
        public Dictionary<UInt32, UInt64> AppTokens { get; private set; } = new Dictionary<UInt32, UInt64>();
        public Dictionary<UInt32, SteamApps.PICSProductInfoCallback.PICSProductInfo> PackageInfo { get; private set; } = new Dictionary<UInt32, SteamApps.PICSProductInfoCallback.PICSProductInfo>();
        public Dictionary<UInt32, SteamApps.PICSProductInfoCallback.PICSProductInfo> AppInfo { get; private set; } = new Dictionary<UInt32, SteamApps.PICSProductInfoCallback.PICSProductInfo>();
        public ConcurrentDictionary<String, TaskCompletionSource<SteamApps.CDNAuthTokenCallback>> CDNAuthTokens { get; private set; } = new ConcurrentDictionary<String, TaskCompletionSource<SteamApps.CDNAuthTokenCallback>>();
        public Dictionary<UInt32, Byte[]> AppTickets { get; private set; } = new Dictionary<UInt32, Byte[]>();
        public Dictionary<UInt32, Byte[]> DepotKeys { get; private set; } = new Dictionary<UInt32, Byte[]>();

        public SteamSession()
        {
            _steamClient = new SteamClient();
            _steamCallbackManager = new CallbackManager(_steamClient);

            _steamUser = _steamClient.GetHandler<SteamUser>();
            _steamApps = _steamClient.GetHandler<SteamApps>();
            _steamCloud = _steamClient.GetHandler<SteamCloud>();

            _steamCallbackManager.Subscribe<SteamClient.ConnectedCallback>(ConnectedCallback);
            _steamCallbackManager.Subscribe<SteamClient.DisconnectedCallback>(DisconnectedCallback);
            _steamCallbackManager.Subscribe<SteamUser.LoggedOnCallback>(LogOnCallback);
            _steamCallbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);

            _steamCallbackManager.Subscribe<SteamUser.SessionTokenCallback>(SessionTokenCallback);
            _steamCallbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(MachineAuthCallback);
            _steamCallbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(UpdateMachineAuthCallback);
            // _steamCallbackManager.Subscribe<SteamUser.LoginKeyCallback>(LoginKeyCallback);

            _steamCallbackManager.Subscribe<SteamApps.LicenseListCallback>(LicenseListCallback);
        }

        /***
         * 
         * Requests
         * 
         * ***/

        public void Login()
        {
            FileLog.LogMessage("Login requested!");
            _steamClient.Connect();
        }

        public void Logoff(LogOffAction logOffAction)
        {
            FileLog.LogMessage("Logoff requested!");
            _onLogOff = logOffAction;
            _steamUser.LogOff();
        }

        public async Task RequestAppInfo(UInt32 appId)
        {
            var appTokens = await _steamApps.PICSGetAccessTokens(appId, null);
            foreach(var token in appTokens.AppTokens)
            {
                AppTokens[token.Key] = token.Value;
            }

            SteamApps.PICSRequest request = new SteamApps.PICSRequest(appId);
            if (AppTokens.ContainsKey(appId))
            {
                request.AccessToken = AppTokens[appId];
                request.Public = false;
            }

            var resultSet = await _steamApps.PICSGetProductInfo(new List<SteamApps.PICSRequest>() { request }, new List<SteamApps.PICSRequest>() { });
            if(resultSet.Complete)
            {
                foreach(var appInfo in resultSet.Results)
                {
                    foreach (var app_value in appInfo.Apps)
                    {
                        var app = app_value.Value;
                        AppInfo[app.ID] = app;
                    }

                    foreach (var app in appInfo.UnknownApps)
                    {
                        AppInfo[app] = null;
                    }
                }
            }
        }

        public async Task RequestPackageInfo(IEnumerable<UInt32> packageIds)
        {
            var packages = packageIds.ToList();
            packages.RemoveAll(pid => PackageInfo.ContainsKey(pid));

            var packageRequests = new List<SteamApps.PICSRequest>();
            foreach (var package in packages)
            {
                var request = new SteamApps.PICSRequest(package);

                if (PackageTokens.TryGetValue(package, out var token))
                {
                    request.AccessToken = token;
                    request.Public = false;
                }

                packageRequests.Add(request);
            }

            var resultSet = await _steamApps.PICSGetProductInfo(new List<SteamApps.PICSRequest>(), packageRequests);
            if (resultSet.Complete)
            {
                foreach (var packageInfo in resultSet.Results)
                {
                    foreach (var package_value in packageInfo.Packages)
                    {
                        var package = package_value.Value;
                        PackageInfo[package.ID] = package;
                    }

                    foreach (var package in packageInfo.UnknownPackages)
                    {
                        PackageInfo[package] = null;
                    }
                }
            }
        }

        public async Task<Boolean> RequestCDNAuthToken(UInt32 appId, UInt32 depotId, String host, String cdnKey)
        {
            if (false == CDNAuthTokens.TryAdd(cdnKey, new TaskCompletionSource<SteamApps.CDNAuthTokenCallback>()))
                return true;

            var token = await _steamApps.GetCDNAuthToken(appId, depotId, host);
            if(token.Result == EResult.OK)
            {
                CDNAuthTokens[cdnKey].TrySetResult(token);
                return true;
            }

            return false;
        }

        /***
         * 
         * Callbacks
         * 
         * ***/

        public void RunCallbacks(Boolean all = false, TimeSpan waitTime = default(TimeSpan))
        {
            if (all)
            {
                _steamCallbackManager.RunWaitAllCallbacks(waitTime);
            }
            else
            {
                _steamCallbackManager.RunCallbacks();
            }
        }

        private void ConnectedCallback(SteamClient.ConnectedCallback callback)
        {
            FileLog.LogMessage($"Steam connected!");
            byte[] sentryHash = null;
            if (File.Exists(Program.SteamUserCredentials.SentryFileName))
            {
                byte[] sentryFile = File.ReadAllBytes(Program.SteamUserCredentials.SentryFileName);
                sentryHash = CryptoHelper.SHAHash(sentryFile);
            }

            _steamUser.LogOn(new SteamUser.LogOnDetails()
            {
                Username = Program.SteamUserCredentials.Username,
                Password = Program.SteamUserCredentials.GetPlainTextPassword(),
                AuthCode = Program.SteamUserCredentials.AuthCode,
                TwoFactorCode = Program.SteamUserCredentials.TwoFactorCode,
                SentryFileHash = sentryHash,
            });
        }

        private void DisconnectedCallback(SteamClient.DisconnectedCallback callback)
        {
            FileLog.LogMessage($"Steam disconnected! WasUser? {callback.UserInitiated}");
            if (false == callback.UserInitiated || _needReconnect)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
                _needReconnect = false;
                _steamClient.Connect();
            }
            else
            {
                if(_onLogOff != null) _onLogOff();
            }
        }

        internal async Task RequestDepotKey(UInt32 depotId, UInt32 appId)
        {
            var key = await _steamApps.GetDepotDecryptionKey(depotId, appId);
            if(key.Result == EResult.OK)
            {
                DepotKeys[key.DepotID] = key.DepotKey;
            }
            else
            {
                FileLog.LogMessage($"Unable to get depot key for {depotId}! Result: {key.Result}");
            }
        }

        private void LogOnCallback(SteamUser.LoggedOnCallback obj)
        {
            var steamGuardRequired = obj.Result == EResult.AccountLogonDenied;
            var isTwoFactorAuth = obj.Result == EResult.AccountLoginDeniedNeedTwoFactor;

            FileLog.LogMessage($"Steam logged on! SteamGuard? {steamGuardRequired}, 2FA? {isTwoFactorAuth}");

            if (steamGuardRequired || isTwoFactorAuth)
            {
                var text = "Default";
                if (isTwoFactorAuth)
                {
                    text = "Please enter your 2 factor auth code from your authenticator app: ";
                }
                else
                {
                    text = $"Please enter the auth code sent to the email at {obj.EmailDomain}:";
                }

                if (Program.AuthCodeWindow == null)
                    Program.AuthCodeWindow = new AuthCodeWindow();

                if (false == Program.AuthCodeWindow.Visible)
                {
                    Program.AuthCodeWindow.SetInformation(isTwoFactorAuth, text);
                    Program.AuthCodeWindow.Show();
                }

                return;
            }

            if(obj.Result == EResult.RateLimitExceeded)
            {
                Program.ConnectingToSteamWindow.PauseCallbacks();
                MessageBox.Show($"There have been too many login failures from your network in a short time period.\nPlease wait and try again later.", Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                FileLog.LogMessage($"There have been too many login failures from your network in a short time period.\nPlease wait and try again later.");
                Application.Restart();
                return;
            }

            if(obj.Result != EResult.OK)
            {
                Program.ConnectingToSteamWindow.PauseCallbacks();
                MessageBox.Show($"Unable to login to this account! Error: {obj.Result}", Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                FileLog.LogMessage($"Unable to login to this account! Error: {obj.Result}");
                Application.Restart();
            }
            else
            {
                Program.SteamUserCredentials.LoggedOn = true;
                Program.MainWindow.Show();
                Program.ConnectingToSteamWindow.Close();
            }
        }

        private void MachineAuthCallback(SteamUser.UpdateMachineAuthCallback obj)
        {
            FileLog.LogMessage($"MachineAuthCallback called! Sentryfile: {obj.FileName}");
            int fileSize;
            byte[] sentryHash;
            var sentryFile = Path.Combine(Program.LocalConfigFolder, obj.FileName);

            using (var fs = File.Open(sentryFile, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                fs.Seek(obj.Offset, SeekOrigin.Begin);
                fs.Write(obj.Data, 0, obj.BytesToWrite);
                fileSize = (int)fs.Length;

                fs.Seek(0, SeekOrigin.Begin);
                using (var sha = SHA1.Create())
                {
                    sentryHash = sha.ComputeHash(fs);
                }
            }

            Program.SteamUserCredentials.SentryFileName = sentryFile;
            Program.SteamUserCredentials.AuthCode = null;
            Program.SteamUserCredentials.TwoFactorCode = null;

            _steamUser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails()
            {
                JobID = obj.JobID,
                FileName = obj.FileName,
                BytesWritten = obj.BytesToWrite,
                FileSize = fileSize,
                Offset = obj.Offset,
                Result = EResult.OK,
                LastError = 0,
                OneTimePassword = obj.OneTimePassword,
                SentryFileHash = sentryHash
            });

            //_needReconnect = true;
            //_steamClient.Disconnect();
        }

        private void OnLoggedOff(SteamUser.LoggedOffCallback obj)
        {
            FileLog.LogMessage($"Steam logged off! {obj.Result}");
            _steamClient.Disconnect();
        }

        //private void LoginKeyCallback(SteamUser.LoginKeyCallback obj)
        //{
        //    throw new NotImplementedException();
        //}

        private void UpdateMachineAuthCallback(SteamUser.UpdateMachineAuthCallback obj)
        {
            FileLog.LogMessage($"UpdateMachineAuthCallback called! New sentryfile: {obj.FileName}");

            int fileSize;
            byte[] sentryHash;
            var sentryFile = Path.Combine(Program.LocalConfigFolder, obj.FileName);

            using (var fs = File.Open(sentryFile, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                fs.Seek(obj.Offset, SeekOrigin.Begin);
                fs.Write(obj.Data, 0, obj.BytesToWrite);
                fileSize = (int)fs.Length;

                fs.Seek(0, SeekOrigin.Begin);
                using (var sha = SHA1.Create())
                {
                    sentryHash = sha.ComputeHash(fs);
                }
            }

            _steamUser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails()
            {
                JobID = obj.JobID,
                FileName = obj.FileName,
                BytesWritten = obj.BytesToWrite,
                FileSize = fileSize,
                Offset = obj.Offset,
                Result = EResult.OK,
                LastError = 0,
                OneTimePassword = obj.OneTimePassword,
                SentryFileHash = sentryHash
            });
        }

        private void SessionTokenCallback(SteamUser.SessionTokenCallback obj)
        {
            FileLog.LogMessage($"SessionTokenCallback called!");
            Program.SteamUserCredentials.SessionToken = obj.SessionToken;
        }

        private void LicenseListCallback(SteamApps.LicenseListCallback obj)
        {
            FileLog.LogMessage($"LicenseListCallback called!");
            if (obj.Result != EResult.OK)
            {
                MessageBox.Show($"Unable top receive license list! Error: {obj.Result}", Program.AppName);
                return;
            }

            Licenses = obj.LicenseList;

            foreach (var license in obj.LicenseList)
            {
                if (license.AccessToken > 0)
                {
                    if(false == PackageTokens.ContainsKey(license.PackageID))
                        PackageTokens.Add(license.PackageID, license.AccessToken);
                }
            }
        }
    }
}
