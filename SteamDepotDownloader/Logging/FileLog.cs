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

        public static Boolean IsEnabled { get; set; } = true;

        public static void LogMessage(String format, params Object[] args)
        {
            LogMessage(String.Format(format, args));
        }

        public static void LogMessage(String text)
        {
            if (IsEnabled)
            {
                lock (_fileLock)
                {
                    if (IsEnabled)
                    {
                        var logFile = Path.Combine(Directory.GetCurrentDirectory(), "app.log");
                        File.AppendAllText(logFile, $"{DateTime.Now} - {text}\n");
                    }
                }
            }
        }
    }
}
