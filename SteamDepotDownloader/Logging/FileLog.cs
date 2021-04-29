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
        private static object _fileLock = new object();

        public static void LogMessage(String format, params Object[] args)
        {
            LogMessage(String.Format(format, args));
        }

        public static void LogMessage(String text)
        {
            lock (_fileLock)
            {
                var logFile = Path.Combine(Directory.GetCurrentDirectory(), "app.log");
                File.AppendAllText(logFile, $"{DateTime.Now} - {text}\n");
            }
        }
    }
}
