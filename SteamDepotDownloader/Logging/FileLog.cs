using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConanExilesDownloader.Logging
{
    public class FileLog
    {
        public static void LogMessage(String text)
        {
            var logFile = Path.Combine(Directory.GetCurrentDirectory(), "app.log");
            File.AppendAllText(logFile, $"{DateTime.Now} - {text}\n");
        }
    }
}
