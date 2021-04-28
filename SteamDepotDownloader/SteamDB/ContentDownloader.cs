using SteamKit2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConanExilesDownloader.SteamDB
{
    internal class ContentDownloader
    {
        private static readonly String CONFIG_DIR = ".DepotDownloader";
        private static readonly String STAGING_DIR = Path.Combine(CONFIG_DIR, "staging");

        public ContentDownloader()
        {

        }

        public static String CreateDirectories(String baseDir, UInt32 depotId, UInt32 depotVersion)
        {
            var installDir = "";
            try
            {
                Directory.CreateDirectory(baseDir);

                var depotPath = Path.Combine(baseDir, depotId.ToString());
                Directory.CreateDirectory(depotPath);

                installDir = Path.Combine(depotPath, depotVersion.ToString());
                Directory.CreateDirectory(installDir);

                Directory.CreateDirectory(Path.Combine(installDir, CONFIG_DIR));
                Directory.CreateDirectory(Path.Combine(installDir, STAGING_DIR));
            }
            catch(Exception ex)
            {
                // ToDo: Logging
            }

            return installDir;
        }

        public static void DownloadUGC(UInt32 appId, UInt64 ugcId)
        {

        }

        public static void DownloadPubfile(UInt32 appId, UInt64 publisherFileId)
        {
        }

        private static Boolean AccountHasAccess(UInt32 depotId)
        {
            var result = false;

            var licenseQuery = Program.SteamSession.Licenses.Select(x => x.PackageID).Distinct().ToList();

            Program.SteamSession.RequestPackageInfo(licenseQuery);

            foreach (var license in licenseQuery)
            {
                SteamApps.PICSProductInfoCallback.PICSProductInfo package;
                //if (Program.SteamSession.PackageInfo.TryGetValue(license, out package) && package != null)
                //{
                //    if (package.KeyValues["appids"].Children.Any(child => child.AsUnsignedInteger() == depotId))
                //        return true;

                //    if (package.KeyValues["depotids"].Children.Any(child => child.AsUnsignedInteger() == depotId))
                //        return true;
                //}
            }

            return result;
        }
    }
}
