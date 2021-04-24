using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConanExilesDownloader.SteamDB
{
    public class DownloadInformation
    {
        public Int32 AppId { get; set; }
        public Int32 DepotId { get; set; }
        public String ManifestId { get; set; }
    }
}
