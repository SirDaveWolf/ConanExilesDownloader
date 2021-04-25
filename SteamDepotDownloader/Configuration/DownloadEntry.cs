using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConanExilesDownloader.Configuration
{
    internal class DownloadEntry
    {
        public String ManifestContent { get; set; }
        public String ManifestBinaries { get; set; }
        public String ManifestServer { get; set; }
        public DateTime ReleaseDate { get; set; }
        public Boolean WasDownloaded { get; set; }
        public String DownloadLocation { get; set; }
    }
}
