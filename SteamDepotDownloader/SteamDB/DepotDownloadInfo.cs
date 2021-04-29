using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConanExilesDownloader.SteamDB
{
    public class DepotDownloadInfo
    {
        public DepotDownloadInfo(UInt32 depotid, UInt64 manifestId, String installDir, String contentName)
        {
            this.id = depotid;
            this.manifestId = manifestId;
            this.installDir = installDir;
            this.contentName = contentName;
        }

        public UInt32 id { get; private set; }
        public String installDir { get; private set; }
        public String contentName { get; private set; }

        public UInt64 manifestId { get; private set; }
        public Byte[] depotKey;
    }
}
