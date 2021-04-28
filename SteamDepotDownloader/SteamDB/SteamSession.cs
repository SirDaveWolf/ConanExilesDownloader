using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteamKit2;

namespace ConanExilesDownloader.SteamDB
{
    internal class SteamSession
    {
        private SteamClient _steamClient;
        private SteamUser _steamUser;
        private CallbackManager _steamCallbackManager;

        private SteamApps _steamApps;
        private SteamCloud _steamCloud;

        private readonly TimeSpan STEAM_TIMEOUT = TimeSpan.FromSeconds(30);

        public delegate void LogOffAction();
        private LogOffAction _onLogOff;

        public ReadOnlyCollection<SteamApps.LicenseListCallback.License> Licenses { get; private set; }
        public Dictionary<UInt32, UInt64> PackageTokens { get; private set; } = new Dictionary<UInt32, UInt64>();

        public SteamSession()
        {
            _steamClient = new SteamClient();
            _steamCallbackManager = new CallbackManager(_steamClient);

            _steamUser = _steamClient.GetHandler<SteamUser>();
            _steamApps = _steamClient.GetHandler<SteamApps>();
            _steamCloud = _steamClient.GetHandler<SteamCloud>();

            _steamCallbackManager.Subscribe<SteamClient.ConnectedCallback>(ConnectedCallback);
            _steamCallbackManager.Subscribe<SteamClient.DisconnectedCallback>(DisconnectedCallback);
            _steamCallbackManager.Subscribe<SteamUser.LoggedOnCallback>(LogOnCallback);
            _steamCallbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);

            _steamCallbackManager.Subscribe<SteamUser.SessionTokenCallback>(SessionTokenCallback);
            _steamCallbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(MachineAuthCallback);
            _steamCallbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(UpdateMachineAuthCallback);
            // _steamCallbackManager.Subscribe<SteamUser.LoginKeyCallback>(LoginKeyCallback);

            _steamCallbackManager.Subscribe<SteamApps.LicenseListCallback>(LicenseListCallback);
        }

        /***
         * 
         * Requests
         * 
         * ***/

        public void Login()
        {
            _steamClient.Connect();
        }

        public void Logoff(LogOffAction logOffAction)
        {
            _onLogOff = logOffAction;
            _steamUser.LogOff();
        }

        public void RequestPackageInfo(List<UInt32> packageIds)
        {
        }

        /***
         * 
         * Callbacks
         * 
         * ***/

        public void RunCallbacks(TimeSpan waitTime)
        {
            _steamCallbackManager.RunWaitAllCallbacks(waitTime);
        }

        private void ConnectedCallback(SteamClient.ConnectedCallback callback)
        {
            byte[] sentryHash = null;
            if (File.Exists(Program.SteamUserCredentials.SentryFileName))
            {
                byte[] sentryFile = File.ReadAllBytes(Program.SteamUserCredentials.SentryFileName);
                sentryHash = CryptoHelper.SHAHash(sentryFile);
            }

            _steamUser.LogOn(new SteamUser.LogOnDetails()
            {
                Username = Program.SteamUserCredentials.Username,
                Password = Program.SteamUserCredentials.GetPlainTextPassword(),
                AuthCode = Program.SteamUserCredentials.AuthCode,
                TwoFactorCode = Program.SteamUserCredentials.TwoFactorCode,
                SentryFileHash = sentryHash
            });
        }

        private void DisconnectedCallback(SteamClient.DisconnectedCallback callback)
        {
            if (false == callback.UserInitiated)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
                _steamClient.Connect();
            }
            else
            {
                if(_onLogOff != null) _onLogOff();
            }
        }

        private void LogOnCallback(SteamUser.LoggedOnCallback obj)
        {
            var steamGuardRequired = obj.Result == EResult.AccountLogonDenied;
            var isTwoFactorAuth = obj.Result == EResult.AccountLoginDeniedNeedTwoFactor;

            if (steamGuardRequired || isTwoFactorAuth)
            {
                var text = "Default";
                if (isTwoFactorAuth)
                {
                    text = "Please enter your 2 factor auth code from your authenticator app: ";
                }
                else
                {
                    text = $"Please enter the auth code sent to the email at {obj.EmailDomain}:";
                }

                if (Program.AuthCodeWindow == null)
                    Program.AuthCodeWindow = new AuthCodeWindow();

                if (false == Program.AuthCodeWindow.Visible)
                {
                    Program.AuthCodeWindow.SetInformation(isTwoFactorAuth, text);
                    Program.AuthCodeWindow.Show();
                }

                return;
            }

            if(obj.Result == EResult.RateLimitExceeded)
            {
                Program.ConnectingToSteamWindow.PauseCallbacks();
                MessageBox.Show($"There have been too many login failures from your network in a short time period.\nPlease wait and try again later.", Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Restart();
                return;
            }

            if(obj.Result != EResult.OK)
            {
                Program.ConnectingToSteamWindow.PauseCallbacks();
                MessageBox.Show($"Unable to login to this account! Error: {obj.Result}", Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Restart();
            }
            else
            {
                Program.SteamUserCredentials.LoggedOn = true;
                Program.MainWindow.Show();
                Program.ConnectingToSteamWindow.Close();
            }
        }

        private void MachineAuthCallback(SteamUser.UpdateMachineAuthCallback obj)
        {
            int fileSize;
            byte[] sentryHash;
            var sentryFile = Path.Combine(Program.LocalConfigFolder, obj.FileName);

            using (var fs = File.Open(sentryFile, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                fs.Seek(obj.Offset, SeekOrigin.Begin);
                fs.Write(obj.Data, 0, obj.BytesToWrite);
                fileSize = (int)fs.Length;

                fs.Seek(0, SeekOrigin.Begin);
                using (var sha = SHA1.Create())
                {
                    sentryHash = sha.ComputeHash(fs);
                }
            }

            Program.SteamUserCredentials.SentryFileName = sentryFile;
            Program.SteamUserCredentials.AuthCode = null;
            Program.SteamUserCredentials.TwoFactorCode = null;

            _steamUser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails()
            {
                JobID = obj.JobID,
                FileName = obj.FileName,
                BytesWritten = obj.BytesToWrite,
                FileSize = fileSize,
                Offset = obj.Offset,
                Result = EResult.OK,
                LastError = 0,
                OneTimePassword = obj.OneTimePassword,
                SentryFileHash = sentryHash
            });

            _steamClient.Disconnect();
        }

        private void OnLoggedOff(SteamUser.LoggedOffCallback obj)
        {
            _steamClient.Disconnect();
        }

        //private void LoginKeyCallback(SteamUser.LoginKeyCallback obj)
        //{
        //    throw new NotImplementedException();
        //}

        private void UpdateMachineAuthCallback(SteamUser.UpdateMachineAuthCallback obj)
        {
            int fileSize;
            byte[] sentryHash;
            var sentryFile = Path.Combine(Program.LocalConfigFolder, obj.FileName);

            using (var fs = File.Open(sentryFile, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                fs.Seek(obj.Offset, SeekOrigin.Begin);
                fs.Write(obj.Data, 0, obj.BytesToWrite);
                fileSize = (int)fs.Length;

                fs.Seek(0, SeekOrigin.Begin);
                using (var sha = SHA1.Create())
                {
                    sentryHash = sha.ComputeHash(fs);
                }
            }

            _steamUser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails()
            {
                JobID = obj.JobID,
                FileName = obj.FileName,
                BytesWritten = obj.BytesToWrite,
                FileSize = fileSize,
                Offset = obj.Offset,
                Result = EResult.OK,
                LastError = 0,
                OneTimePassword = obj.OneTimePassword,
                SentryFileHash = sentryHash
            });
        }

        private void SessionTokenCallback(SteamUser.SessionTokenCallback obj)
        {
            Program.SteamUserCredentials.SessionToken = obj.SessionToken;
        }

        private void LicenseListCallback(SteamApps.LicenseListCallback obj)
        {
            if(obj.Result != EResult.OK)
            {
                MessageBox.Show($"Unable top receive license list! Error: {obj.Result}", Program.AppName);

                return;
            }

            Licenses = obj.LicenseList;

            foreach (var license in obj.LicenseList)
            {
                if (license.AccessToken > 0)
                {
                    if(false == PackageTokens.ContainsKey(license.PackageID))
                        PackageTokens.Add(license.PackageID, license.AccessToken);
                }
            }
        }

        public delegate Boolean WaitCondition();
        private object steamLock = new object();
        private Int32 seq;

        public Boolean WaitUntilCallback(Action submitter, WaitCondition waiter)
        {
            while (!waiter())
            {
                lock (steamLock)
                {
                    submitter();
                }

                int seq = this.seq;
                do
                {
                    lock (steamLock)
                    {
                        WaitForCallbacks();
                    }
                }
                while (this.seq == seq && !waiter());
            }

            return true;
        }

        private void WaitForCallbacks()
        {
            _steamCallbackManager.RunWaitCallbacks(TimeSpan.FromSeconds(1));

            //TimeSpan diff = DateTime.Now - connectTime;

            //if (diff > STEAM3_TIMEOUT && !bConnected)
            //{
            //    Console.WriteLine("Timeout connecting to Steam3.");
            //    Abort();

            //    return;
            //}
        }
    }
}
