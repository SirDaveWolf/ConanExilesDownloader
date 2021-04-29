using ConanExilesDownloader.Logging;
using SteamKit2;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConanExilesDownloader.SteamDB
{
    internal class ContentDownloader
    {
        private static readonly String CONFIG_DIR = ".DepotDownloader";
        private static readonly String STAGING_DIR = Path.Combine(CONFIG_DIR, "staging");

        const UInt64 INVALID_MANIFEST_ID = UInt64.MaxValue;

        public static DownloadConfig Config = new DownloadConfig();

        public static async Task DownloadAppAsync(UInt32 appId, UInt32 depotId, UInt64 manifestId, String downloadFolder)
        {
            await Program.SteamSession.RequestAppInfo(appId);

            if (await AccountHasAccess(appId))
            {
                var cdnPool = new CDNClientPool(appId);
                var info = await GetDepotInfo(depotId, appId, manifestId, "public", downloadFolder);

                try
                {
                    await DownloadSteam3Async(appId, info, cdnPool).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    FileLog.LogMessage($"App {appId} was not completely downloaded.");
                    throw;
                }
                finally
                {
                    cdnPool.Shutdown();
                }
            }
            else
            {
                FileLog.LogMessage("This account is not allowed to download Conan Exiles! Please purchase the product first!");
                MessageBox.Show("This account is not allowed to download Conan Exiles! Please purchase the product first!", Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static Boolean CreateDirectories(UInt32 depotId, UInt32 depotVersion, String baseDir, out String installDir)
        {
            var result = true;
            installDir = "";

            try
            {
                Directory.CreateDirectory(baseDir);

                installDir = baseDir;
                Config.InstallDirectory = installDir;

                Directory.CreateDirectory(Path.Combine(installDir, CONFIG_DIR));
                Directory.CreateDirectory(Path.Combine(installDir, STAGING_DIR));
            }
            catch (Exception ex)
            {
                FileLog.LogMessage($"Could not create directories! Error: {ex}");
                result = false;
            }

            return result;
        }

        internal static KeyValue GetSteam3AppSection(UInt32 appId, EAppInfoSection section)
        {
            SteamApps.PICSProductInfoCallback.PICSProductInfo app;
            if (false == Program.SteamSession.AppInfo.TryGetValue(appId, out app) || app == null)
            {
                return null;
            }

            KeyValue appinfo = app.KeyValues;
            string section_key;

            switch (section)
            {
                case EAppInfoSection.Common:
                    section_key = "common";
                    break;
                case EAppInfoSection.Extended:
                    section_key = "extended";
                    break;
                case EAppInfoSection.Config:
                    section_key = "config";
                    break;
                case EAppInfoSection.Depots:
                    section_key = "depots";
                    break;
                default:
                    throw new NotImplementedException();
            }

            KeyValue section_kv = appinfo.Children.Where(c => c.Name == section_key).FirstOrDefault();
            return section_kv;
        }

        private static async Task<DepotDownloadInfo> GetDepotInfo(UInt32 depotId, UInt32 appId, UInt64 manifestId, String branch, String baseDir)
        {
            await Program.SteamSession.RequestAppInfo(appId);

            string contentName = GetAppOrDepotName(depotId, appId);

            if (false == (await AccountHasAccess(depotId)))
            {
                FileLog.LogMessage($"Depot {depotId} ({contentName}) is not available from this account.");
                MessageBox.Show($"Depot {depotId} ({contentName}) is not available from this account.", Program.AppName);
                return null;
            }

            // Skip requesting an app ticket
            Program.SteamSession.AppTickets[depotId] = null;

            if (manifestId == INVALID_MANIFEST_ID)
            {
                manifestId = await GetSteam3DepotManifest(depotId, appId, branch);
                if (manifestId == INVALID_MANIFEST_ID && branch != "public")
                {
                    FileLog.LogMessage($"Warning: Depot {depotId} does not have branch named \"{branch}\". Trying public branch.");
                    branch = "public";
                    manifestId = await GetSteam3DepotManifest(depotId, appId, branch);
                }

                if (manifestId == INVALID_MANIFEST_ID)
                {
                    FileLog.LogMessage($"Depot {depotId} ({contentName}) missing public subsection or manifest section.");
                    return null;
                }
            }

            string installDir;
            if (!CreateDirectories(depotId, 0, baseDir, out installDir))
            {
                FileLog.LogMessage("Unable to download files to the target location! Missing IO permissions!");
                return null;
            }

            await Program.SteamSession.RequestDepotKey(depotId, appId);
            if (false == Program.SteamSession.DepotKeys.ContainsKey(depotId))
            {
                FileLog.LogMessage($"No valid depot key for {depotId}, unable to download.");
                return null;
            }

            var depotKey = Program.SteamSession.DepotKeys[depotId];

            var info = new DepotDownloadInfo(depotId, manifestId, installDir, contentName);
            info.depotKey = depotKey;
            return info;
        }

        //public static async Task DownloadUGC(UInt32 appId, UInt64 ugcId)
        //{

        //}

        //public static async Task DownloadPubfile(UInt32 appId, UInt64 publisherFileId)
        //{
        //}

        private static async Task<Boolean> AccountHasAccess(UInt32 depotId)
        {
            var result = false;

            var licenseQuery = Program.SteamSession.Licenses.Select(x => x.PackageID).Distinct();

            await Program.SteamSession.RequestPackageInfo(licenseQuery);

            foreach (var license in licenseQuery)
            {
                SteamApps.PICSProductInfoCallback.PICSProductInfo package;
                if (Program.SteamSession.PackageInfo.TryGetValue(license, out package) && package != null)
                {
                    if (package.KeyValues["appids"].Children.Any(child => child.AsUnsignedInteger() == depotId))
                        result = true;

                    if (package.KeyValues["depotids"].Children.Any(child => child.AsUnsignedInteger() == depotId))
                        result = true;
                }
            }

            return result;
        }

        private static string GetAppOrDepotName(uint depotId, uint appId)
        {
            if (depotId == UInt32.MaxValue)
            {
                KeyValue info = GetSteam3AppSection(appId, EAppInfoSection.Common);

                if (info == null)
                    return String.Empty;

                return info["name"].AsString();
            }
            else
            {
                KeyValue depots = GetSteam3AppSection(appId, EAppInfoSection.Depots);

                if (depots == null)
                    return String.Empty;

                KeyValue depotChild = depots[depotId.ToString()];

                if (depotChild == null)
                    return String.Empty;

                return depotChild["name"].AsString();
            }
        }

        private static async Task<UInt64> GetSteam3DepotManifest(UInt32 depotId, UInt32 appId, String branch)
        {
            KeyValue depots = GetSteam3AppSection(appId, EAppInfoSection.Depots);
            KeyValue depotChild = depots[depotId.ToString()];

            if (depotChild == KeyValue.Invalid)
                return INVALID_MANIFEST_ID;

            // Shared depots can either provide manifests, or leave you relying on their parent app.
            // It seems that with the latter, "sharedinstall" will exist (and equals 2 in the one existance I know of).
            // Rather than relay on the unknown sharedinstall key, just look for manifests. Test cases: 111710, 346680.
            if (depotChild["manifests"] == KeyValue.Invalid && depotChild["depotfromapp"] != KeyValue.Invalid)
            {
                uint otherAppId = depotChild["depotfromapp"].AsUnsignedInteger();
                if (otherAppId == appId)
                {
                    // This shouldn't ever happen, but ya never know with Valve. Don't infinite loop.
                    FileLog.LogMessage("App {0}, Depot {1} has depotfromapp of {2}!",
                        appId, depotId, otherAppId);
                    return INVALID_MANIFEST_ID;
                }

                await Program.SteamSession.RequestAppInfo(otherAppId);

                return await GetSteam3DepotManifest(depotId, otherAppId, branch);
            }

            var manifests = depotChild["manifests"];
            var manifests_encrypted = depotChild["encryptedmanifests"];

            if (manifests.Children.Count == 0 && manifests_encrypted.Children.Count == 0)
                return INVALID_MANIFEST_ID;

            var node = manifests[branch];

            if (node.Value == null)
                return INVALID_MANIFEST_ID;

            return UInt64.Parse(node.Value);
        }

        private static async Task DownloadSteam3Async(uint appId, DepotDownloadInfo depot, CDNClientPool cdnPool)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            cdnPool.ExhaustedToken = cts;

            GlobalDownloadCounter downloadCounter = new GlobalDownloadCounter();
            var allFileNamesAllDepots = new HashSet<String>();

            // First, fetch all the manifests for each depot (including previous manifests) and perform the initial setup
            var depotFileData = await ProcessDepotManifestAndFiles(cts, appId, depot, cdnPool);

            if (depotFileData != null)
            {
                allFileNamesAllDepots.UnionWith(depotFileData.allFileNames);
            }

            cts.Token.ThrowIfCancellationRequested();

            await DownloadSteam3AsyncDepotFiles(cts, appId, downloadCounter, depotFileData, allFileNamesAllDepots, cdnPool);

            FileLog.LogMessage($"Total downloaded: {downloadCounter.TotalBytesCompressed} bytes ({downloadCounter.TotalBytesUncompressed} bytes uncompressed) from depot");
        }

        private static async Task<DepotFilesData> ProcessDepotManifestAndFiles(CancellationTokenSource cts,
            UInt32 appId, DepotDownloadInfo depot, CDNClientPool cdnPool)
        {
            DepotDownloadCounter depotCounter = new DepotDownloadCounter();

            FileLog.LogMessage("Processing depot {0} - {1}", depot.id, depot.contentName);

            ProtoManifest oldProtoManifest = null;
            ProtoManifest newProtoManifest = null;
            string configDir = Path.Combine(depot.installDir, CONFIG_DIR);

            ulong lastManifestId = INVALID_MANIFEST_ID;
            //DepotConfigStore.Instance.InstalledManifestIDs.TryGetValue(depot.id, out lastManifestId);

            //// In case we have an early exit, this will force equiv of verifyall next run.
            //DepotConfigStore.Instance.InstalledManifestIDs[depot.id] = INVALID_MANIFEST_ID;
            //DepotConfigStore.Save();

            //if (lastManifestId != INVALID_MANIFEST_ID)
            //{
            //    var oldManifestFileName = Path.Combine(configDir, string.Format("{0}_{1}.bin", depot.id, lastManifestId));

            //    if (File.Exists(oldManifestFileName))
            //    {
            //        byte[] expectedChecksum, currentChecksum;

            //        try
            //        {
            //            expectedChecksum = File.ReadAllBytes(oldManifestFileName + ".sha");
            //        }
            //        catch (IOException)
            //        {
            //            expectedChecksum = null;
            //        }

            //        oldProtoManifest = ProtoManifest.LoadFromFile(oldManifestFileName, out currentChecksum);

            //        if (expectedChecksum == null || !expectedChecksum.SequenceEqual(currentChecksum))
            //        {
            //            // We only have to show this warning if the old manifest ID was different
            //            if (lastManifestId != depot.manifestId)
            //                FileLog.LogMessage("Manifest {0} on disk did not match the expected checksum.", lastManifestId);
            //            oldProtoManifest = null;
            //        }
            //    }
            //}

            if (lastManifestId == depot.manifestId && oldProtoManifest != null)
            {
                newProtoManifest = oldProtoManifest;
                FileLog.LogMessage("Already have manifest {0} for depot {1}.", depot.manifestId, depot.id);
            }
            else
            {
                var newManifestFileName = Path.Combine(configDir, string.Format("{0}_{1}.bin", depot.id, depot.manifestId));
                if (newManifestFileName != null)
                {
                    byte[] expectedChecksum, currentChecksum;

                    try
                    {
                        expectedChecksum = File.ReadAllBytes(newManifestFileName + ".sha");
                    }
                    catch (IOException)
                    {
                        expectedChecksum = null;
                    }

                    newProtoManifest = ProtoManifest.LoadFromFile(newManifestFileName, out currentChecksum);

                    if (newProtoManifest != null && (expectedChecksum == null || !expectedChecksum.SequenceEqual(currentChecksum)))
                    {
                        FileLog.LogMessage("Manifest {0} on disk did not match the expected checksum.", depot.manifestId);
                        newProtoManifest = null;
                    }
                }

                if (newProtoManifest != null)
                {
                    FileLog.LogMessage("Already have manifest {0} for depot {1}.", depot.manifestId, depot.id);
                }
                else
                {
                    Console.Write("Downloading depot manifest...");

                    DepotManifest depotManifest = null;

                    do
                    {
                        cts.Token.ThrowIfCancellationRequested();

                        CDNClient.Server connection = null;

                        try
                        {
                            connection = cdnPool.GetConnection(cts.Token);
                            var cdnToken = await cdnPool.AuthenticateConnection(appId, depot.id, connection);

#if STEAMKIT_UNRELEASED
                            depotManifest = await cdnPool.CDNClient.DownloadManifestAsync(depot.id, depot.manifestId,
                                connection, cdnToken, depot.depotKey, proxyServer: cdnPool.ProxyServer).ConfigureAwait(false);
#else
                            depotManifest = await cdnPool.CDNClient.DownloadManifestAsync(depot.id, depot.manifestId,
                                connection, cdnToken, depot.depotKey).ConfigureAwait(false);
#endif

                            cdnPool.ReturnConnection(connection);
                        }
                        catch (TaskCanceledException)
                        {
                            FileLog.LogMessage("Connection timeout downloading depot manifest {0} {1}", depot.id, depot.manifestId);
                        }
                        catch (SteamKitWebRequestException e)
                        {
                            cdnPool.ReturnBrokenConnection(connection);

                            if (e.StatusCode == HttpStatusCode.Unauthorized || e.StatusCode == HttpStatusCode.Forbidden)
                            {
                                FileLog.LogMessage("Encountered 401 for depot manifest {0} {1}. Aborting.", depot.id, depot.manifestId);
                                break;
                            }
                            else if (e.StatusCode == HttpStatusCode.NotFound)
                            {
                                FileLog.LogMessage("Encountered 404 for depot manifest {0} {1}. Aborting.", depot.id, depot.manifestId);
                                break;
                            }
                            else
                            {
                                FileLog.LogMessage("Encountered error downloading depot manifest {0} {1}: {2}", depot.id, depot.manifestId, e.StatusCode);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception e)
                        {
                            cdnPool.ReturnBrokenConnection(connection);
                            FileLog.LogMessage("Encountered error downloading manifest for depot {0} {1}: {2}", depot.id, depot.manifestId, e.Message);
                        }
                    }
                    while (depotManifest == null);

                    if (depotManifest == null)
                    {
                        FileLog.LogMessage("\nUnable to download manifest {0} for depot {1}", depot.manifestId, depot.id);
                        cts.Cancel();
                    }

                    // Throw the cancellation exception if requested so that this task is marked failed
                    cts.Token.ThrowIfCancellationRequested();

                    byte[] checksum;

                    newProtoManifest = new ProtoManifest(depotManifest, depot.manifestId);
                    newProtoManifest.SaveToFile(newManifestFileName, out checksum);
                    File.WriteAllBytes(newManifestFileName + ".sha", checksum);

                    FileLog.LogMessage(" Done!");
                }
            }

            newProtoManifest.Files.Sort((x, y) => string.Compare(x.FileName, y.FileName, StringComparison.Ordinal));

            FileLog.LogMessage("Manifest {0} ({1})", depot.manifestId, newProtoManifest.CreationTime);

            //if (Config.DownloadManifestOnly)
            //{
            //    StringBuilder manifestBuilder = new StringBuilder();
            //    string txtManifest = Path.Combine(depot.installDir, string.Format("manifest_{0}_{1}.txt", depot.id, depot.manifestId));
            //    manifestBuilder.Append(string.Format("{0}\n\n", newProtoManifest.CreationTime));

            //    foreach (var file in newProtoManifest.Files)
            //    {
            //        if (file.Flags.HasFlag(EDepotFileFlag.Directory))
            //            continue;

            //        manifestBuilder.Append(string.Format("{0}\n", file.FileName));
            //        manifestBuilder.Append(string.Format("\t{0}\n", file.TotalSize));
            //        manifestBuilder.Append(string.Format("\t{0}\n", BitConverter.ToString(file.FileHash).Replace("-", "")));
            //    }

            //    File.WriteAllText(txtManifest, manifestBuilder.ToString());
            //    return null;
            //}

            string stagingDir = Path.Combine(depot.installDir, STAGING_DIR);

            var filesAfterExclusions = newProtoManifest.Files.AsParallel().Where(f => TestIsFileIncluded(f.FileName)).ToList();
            var allFileNames = new HashSet<string>(filesAfterExclusions.Count);

            // Pre-process
            filesAfterExclusions.ForEach(file =>
            {
                allFileNames.Add(file.FileName);

                var fileFinalPath = Path.Combine(depot.installDir, file.FileName);
                var fileStagingPath = Path.Combine(stagingDir, file.FileName);

                if (file.Flags.HasFlag(EDepotFileFlag.Directory))
                {
                    Directory.CreateDirectory(fileFinalPath);
                    Directory.CreateDirectory(fileStagingPath);
                }
                else
                {
                    // Some manifests don't explicitly include all necessary directories
                    Directory.CreateDirectory(Path.GetDirectoryName(fileFinalPath));
                    Directory.CreateDirectory(Path.GetDirectoryName(fileStagingPath));

                    depotCounter.CompleteDownloadSize += file.TotalSize;
                }
            });

            return new DepotFilesData
            {
                depotDownloadInfo = depot,
                depotCounter = depotCounter,
                stagingDir = stagingDir,
                manifest = newProtoManifest,
                previousManifest = oldProtoManifest,
                filteredFiles = filesAfterExclusions,
                allFileNames = allFileNames
            };
        }

        static bool TestIsFileIncluded(string filename)
        {
            //foreach (string fileListEntry in Config.FilesToDownload)
            //{
            //    if (fileListEntry.Equals(filename, StringComparison.OrdinalIgnoreCase))
            //        return true;
            //}

            //foreach (Regex rgx in Config.FilesToDownloadRegex)
            //{
            //    Match m = rgx.Match(filename);

            //    if (m.Success)
            //        return true;
            //}

            return true;
        }

        private static async Task DownloadSteam3AsyncDepotFiles(CancellationTokenSource cts, UInt32 appId,
            GlobalDownloadCounter downloadCounter, DepotFilesData depotFilesData, HashSet<String> allFileNamesAllDepots, CDNClientPool cdnPool)
        {
            var depot = depotFilesData.depotDownloadInfo;
            var depotCounter = depotFilesData.depotCounter;

            FileLog.LogMessage("Downloading depot {0} - {1}", depot.id, depot.contentName);

            var files = depotFilesData.filteredFiles.Where(f => !f.Flags.HasFlag(EDepotFileFlag.Directory)).ToArray();
            var networkChunkQueue = new ConcurrentQueue<(FileStreamData fileStreamData, ProtoManifest.FileData fileData, ProtoManifest.ChunkData chunk)>();

            await Util.InvokeAsync(
                files.Select(file => new Func<Task>(async () =>
                    await Task.Run(() => DownloadSteam3AsyncDepotFile(cts, depotFilesData, file, networkChunkQueue)))),
                maxDegreeOfParallelism: 8
            );

            await Util.InvokeAsync(
                networkChunkQueue.Select(q => new Func<Task>(async () =>
                    await Task.Run(() => DownloadSteam3AsyncDepotFileChunk(cts, appId, downloadCounter, depotFilesData,
                        q.fileData, q.fileStreamData, q.chunk, cdnPool)))),
                maxDegreeOfParallelism: 8
            );

            // Check for deleted files if updating the depot.
            if (depotFilesData.previousManifest != null)
            {
                var previousFilteredFiles = depotFilesData.previousManifest.Files.AsParallel().Where(f => TestIsFileIncluded(f.FileName)).Select(f => f.FileName).ToHashSet();

                // Check if we are writing to a single output directory. If not, each depot folder is managed independently
                if (string.IsNullOrWhiteSpace(ContentDownloader.Config.InstallDirectory))
                {
                    // Of the list of files in the previous manifest, remove any file names that exist in the current set of all file names
                    previousFilteredFiles.ExceptWith(depotFilesData.allFileNames);
                }
                else
                {
                    // Of the list of files in the previous manifest, remove any file names that exist in the current set of all file names across all depots being downloaded
                    previousFilteredFiles.ExceptWith(allFileNamesAllDepots);
                }

                foreach (var existingFileName in previousFilteredFiles)
                {
                    string fileFinalPath = Path.Combine(depot.installDir, existingFileName);

                    if (!File.Exists(fileFinalPath))
                        continue;

                    File.Delete(fileFinalPath);
                    FileLog.LogMessage("Deleted {0}", fileFinalPath);
                }
            }

            //DepotConfigStore.Instance.InstalledManifestIDs[depot.id] = depot.manifestId;
            //DepotConfigStore.Save();

            FileLog.LogMessage("Depot {0} - Downloaded {1} bytes ({2} bytes uncompressed)", depot.id, depotCounter.DepotBytesCompressed, depotCounter.DepotBytesUncompressed);
        }

        private static void DownloadSteam3AsyncDepotFile(
            CancellationTokenSource cts,
            DepotFilesData depotFilesData,
            ProtoManifest.FileData file,
            ConcurrentQueue<(FileStreamData, ProtoManifest.FileData, ProtoManifest.ChunkData)> networkChunkQueue)
        {
            cts.Token.ThrowIfCancellationRequested();

            var depot = depotFilesData.depotDownloadInfo;
            var stagingDir = depotFilesData.stagingDir;
            var depotDownloadCounter = depotFilesData.depotCounter;
            var oldProtoManifest = depotFilesData.previousManifest;

            string fileFinalPath = Path.Combine(depot.installDir, file.FileName);
            string fileStagingPath = Path.Combine(stagingDir, file.FileName);

            // This may still exist if the previous run exited before cleanup
            if (File.Exists(fileStagingPath))
            {
                File.Delete(fileStagingPath);
            }

            FileStream fs = null;
            List<ProtoManifest.ChunkData> neededChunks;
            FileInfo fi = new FileInfo(fileFinalPath);
            if (!fi.Exists)
            {
                FileLog.LogMessage("Pre-allocating {0}", fileFinalPath);

                // create new file. need all chunks
                fs = File.Create(fileFinalPath);
                fs.SetLength((long)file.TotalSize);
                neededChunks = new List<ProtoManifest.ChunkData>(file.Chunks);
            }
            else
            {
                // open existing
                ProtoManifest.FileData oldManifestFile = null;
                if (oldProtoManifest != null)
                {
                    oldManifestFile = oldProtoManifest.Files.SingleOrDefault(f => f.FileName == file.FileName);
                }

                if (oldManifestFile != null)
                {
                    neededChunks = new List<ProtoManifest.ChunkData>();

                    if (!oldManifestFile.FileHash.SequenceEqual(file.FileHash))
                    {
                        // we have a version of this file, but it doesn't fully match what we want
                        //if (Config.VerifyAll)
                        //{
                        //    FileLog.LogMessage("Validating {0}", fileFinalPath);
                        //}

                        var matchingChunks = new List<ChunkMatch>();

                        foreach (var chunk in file.Chunks)
                        {
                            var oldChunk = oldManifestFile.Chunks.FirstOrDefault(c => c.ChunkID.SequenceEqual(chunk.ChunkID));
                            if (oldChunk != null)
                            {
                                matchingChunks.Add(new ChunkMatch(oldChunk, chunk));
                            }
                            else
                            {
                                neededChunks.Add(chunk);
                            }
                        }

                        var orderedChunks = matchingChunks.OrderBy(x => x.OldChunk.Offset);

                        File.Move(fileFinalPath, fileStagingPath);

                        fs = File.Open(fileFinalPath, FileMode.Create);
                        fs.SetLength((long)file.TotalSize);

                        using (var fsOld = File.Open(fileStagingPath, FileMode.Open))
                        {
                            foreach (var match in orderedChunks)
                            {
                                fsOld.Seek((long)match.OldChunk.Offset, SeekOrigin.Begin);

                                byte[] tmp = new byte[match.OldChunk.UncompressedLength];
                                fsOld.Read(tmp, 0, tmp.Length);

                                byte[] adler = Util.AdlerHash(tmp);
                                if (!adler.SequenceEqual(match.OldChunk.Checksum))
                                {
                                    neededChunks.Add(match.NewChunk);
                                }
                                else
                                {
                                    fs.Seek((long)match.NewChunk.Offset, SeekOrigin.Begin);
                                    fs.Write(tmp, 0, tmp.Length);
                                }
                            }
                        }

                        File.Delete(fileStagingPath);
                    }
                }
                else
                {
                    // No old manifest or file not in old manifest. We must validate.

                    fs = File.Open(fileFinalPath, FileMode.Open);
                    if ((ulong)fi.Length != file.TotalSize)
                    {
                        fs.SetLength((long)file.TotalSize);
                    }

                    FileLog.LogMessage("Validating {0}", fileFinalPath);
                    neededChunks = Util.ValidateSteam3FileChecksums(fs, file.Chunks.OrderBy(x => x.Offset).ToArray());
                }

                if (neededChunks.Count() == 0)
                {
                    lock (depotDownloadCounter)
                    {
                        depotDownloadCounter.SizeDownloaded += (ulong)file.TotalSize;
                        FileLog.LogMessage("{0,6:#00.00}% {1}", ((float)depotDownloadCounter.SizeDownloaded / (float)depotDownloadCounter.CompleteDownloadSize) * 100.0f, fileFinalPath);
                    }

                    if (fs != null)
                        fs.Dispose();
                    return;
                }
                else
                {
                    var sizeOnDisk = (file.TotalSize - (ulong)neededChunks.Select(x => (long)x.UncompressedLength).Sum());
                    lock (depotDownloadCounter)
                    {
                        depotDownloadCounter.SizeDownloaded += sizeOnDisk;
                    }
                }
            }

            FileStreamData fileStreamData = new FileStreamData
            {
                fileStream = fs,
                fileLock = new SemaphoreSlim(1),
                chunksToDownload = neededChunks.Count
            };

            foreach (var chunk in neededChunks)
            {
                networkChunkQueue.Enqueue((fileStreamData, file, chunk));
            }
        }

        private static async Task DownloadSteam3AsyncDepotFileChunk(
            CancellationTokenSource cts, uint appId,
            GlobalDownloadCounter downloadCounter,
            DepotFilesData depotFilesData,
            ProtoManifest.FileData file,
            FileStreamData fileStreamData,
            ProtoManifest.ChunkData chunk,
            CDNClientPool cdnPool)
        {
            cts.Token.ThrowIfCancellationRequested();

            var depot = depotFilesData.depotDownloadInfo;
            var depotDownloadCounter = depotFilesData.depotCounter;

            string chunkID = Util.EncodeHexString(chunk.ChunkID);

            DepotManifest.ChunkData data = new DepotManifest.ChunkData();
            data.ChunkID = chunk.ChunkID;
            data.Checksum = chunk.Checksum;
            data.Offset = chunk.Offset;
            data.CompressedLength = chunk.CompressedLength;
            data.UncompressedLength = chunk.UncompressedLength;

            CDNClient.DepotChunk chunkData = null;

            do
            {
                cts.Token.ThrowIfCancellationRequested();

                CDNClient.Server connection = null;

                try
                {
                    connection = cdnPool.GetConnection(cts.Token);
                    var cdnToken = await cdnPool.AuthenticateConnection(appId, depot.id, connection);

#if STEAMKIT_UNRELEASED
                    chunkData = await cdnPool.CDNClient.DownloadDepotChunkAsync(depot.id, data,
                        connection, cdnToken, depot.depotKey, proxyServer: cdnPool.ProxyServer).ConfigureAwait(false);
#else
                    chunkData = await cdnPool.CDNClient.DownloadDepotChunkAsync(depot.id, data,
                        connection, cdnToken, depot.depotKey).ConfigureAwait(false);
#endif

                    cdnPool.ReturnConnection(connection);
                }
                catch (TaskCanceledException)
                {
                    FileLog.LogMessage("Connection timeout downloading chunk {0}", chunkID);
                }
                catch (SteamKitWebRequestException e)
                {
                    cdnPool.ReturnBrokenConnection(connection);

                    if (e.StatusCode == HttpStatusCode.Unauthorized || e.StatusCode == HttpStatusCode.Forbidden)
                    {
                        FileLog.LogMessage("Encountered 401 for chunk {0}. Aborting.", chunkID);
                        break;
                    }
                    else
                    {
                        FileLog.LogMessage("Encountered error downloading chunk {0}: {1}", chunkID, e.StatusCode);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    cdnPool.ReturnBrokenConnection(connection);
                    FileLog.LogMessage("Encountered unexpected error downloading chunk {0}: {1}", chunkID, e.Message);
                }
            }
            while (chunkData == null);

            if (chunkData == null)
            {
                FileLog.LogMessage("Failed to find any server with chunk {0} for depot {1}. Aborting.", chunkID, depot.id);
                cts.Cancel();
            }

            // Throw the cancellation exception if requested so that this task is marked failed
            cts.Token.ThrowIfCancellationRequested();

            try
            {
                await fileStreamData.fileLock.WaitAsync().ConfigureAwait(false);

                fileStreamData.fileStream.Seek((long)chunkData.ChunkInfo.Offset, SeekOrigin.Begin);
                await fileStreamData.fileStream.WriteAsync(chunkData.Data, 0, chunkData.Data.Length);
            }
            finally
            {
                fileStreamData.fileLock.Release();
            }

            int remainingChunks = Interlocked.Decrement(ref fileStreamData.chunksToDownload);
            if (remainingChunks == 0)
            {
                fileStreamData.fileStream.Dispose();
                fileStreamData.fileLock.Dispose();
            }

            ulong sizeDownloaded = 0;
            lock (depotDownloadCounter)
            {
                sizeDownloaded = depotDownloadCounter.SizeDownloaded + (ulong)chunkData.Data.Length;
                depotDownloadCounter.SizeDownloaded = sizeDownloaded;
                depotDownloadCounter.DepotBytesCompressed += chunk.CompressedLength;
                depotDownloadCounter.DepotBytesUncompressed += chunk.UncompressedLength;
            }

            lock (downloadCounter)
            {
                downloadCounter.TotalBytesCompressed += chunk.CompressedLength;
                downloadCounter.TotalBytesUncompressed += chunk.UncompressedLength;
            }

            if (remainingChunks == 0)
            {
                var fileFinalPath = Path.Combine(depot.installDir, file.FileName);
                FileLog.LogMessage("{0,6:#00.00}% {1}", ((float)sizeDownloaded / (float)depotDownloadCounter.CompleteDownloadSize) * 100.0f, fileFinalPath);
                Program.MainWindow.SetProgressBar((Int32)(((float)sizeDownloaded / (float)depotDownloadCounter.CompleteDownloadSize)) * 100);
            }

        }

        private class ChunkMatch
        {
            public ChunkMatch(ProtoManifest.ChunkData oldChunk, ProtoManifest.ChunkData newChunk)
            {
                OldChunk = oldChunk;
                NewChunk = newChunk;
            }
            public ProtoManifest.ChunkData OldChunk { get; private set; }
            public ProtoManifest.ChunkData NewChunk { get; private set; }
        }

        private class DepotFilesData
        {
            public DepotDownloadInfo depotDownloadInfo;
            public DepotDownloadCounter depotCounter;
            public string stagingDir;
            public ProtoManifest manifest;
            public ProtoManifest previousManifest;
            public List<ProtoManifest.FileData> filteredFiles;
            public HashSet<string> allFileNames;
        }

        private class FileStreamData
        {
            public FileStream fileStream;
            public SemaphoreSlim fileLock;
            public int chunksToDownload;
        }

        private class GlobalDownloadCounter
        {
            public ulong TotalBytesCompressed;
            public ulong TotalBytesUncompressed;
        }

        private class DepotDownloadCounter
        {
            public ulong CompleteDownloadSize;
            public ulong SizeDownloaded;
            public ulong DepotBytesCompressed;
            public ulong DepotBytesUncompressed;

        }
    }
}
