using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConanExilesDownloader.SteamDB
{
    class DownloadConfig
    {
        public Int32 CellID { get; set; }
        public Boolean DownloadAllPlatforms { get; set; } = false;
        public Boolean DownloadAllLanguages { get; set; } = false;
        public Boolean DownloadManifestOnly { get; set; } = false;
        public String InstallDirectory { get; set; }

        public Boolean UsingFileList { get; set; } = false;
        public List<String> FilesToDownload { get; set; } = new List<String>();
        public List<Regex> FilesToDownloadRegex { get; set; } = new List<Regex>();

        public Boolean UsingExclusionList { get; set; } = false;

        public String BetaPassword { get; set; } = String.Empty;

        public Boolean VerifyAll { get; set; } = false;

        public Int32 MaxServers { get; set; } = 20;
        public Int32 MaxDownloads { get; set; } = 8;

        public String SuppliedPassword { get; set; } = String.Empty;
        public Boolean RememberPassword { get; set; } = false;
    }
}
