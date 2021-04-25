using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security;
using System.Runtime.InteropServices;

namespace ConanExilesDownloader.SteamDB
{
    internal class SteamUserCredentials
    {
        public SteamUserCredentials()
        {
            Passsword = new SecureString();
            AuthCode = null;
            TwoFactorCode = null;
        }


        public SteamUserCredentials(String username, String password)
            : this()
        {
            foreach (var ch in password)
                Passsword.AppendChar(ch);

            Username = username;
        }

        public String Username { get; set; }
        public SecureString Passsword { get; set; }

        public String AuthCode { get; set; }
        public String TwoFactorCode { get; set; }

        public String SentryFileName { get; set; }

        public UInt64 SessionToken { get; set; }

        public Boolean LoggedOn { get; set; }

        public String GetPlainTextPassword()
        {
            IntPtr valuePtr = IntPtr.Zero;

            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(Passsword);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
    }
}
