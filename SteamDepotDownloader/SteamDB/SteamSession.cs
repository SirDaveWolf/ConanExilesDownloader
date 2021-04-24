using System;
using System.Collections.Generic;
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

        public SteamSession()
        {
            _steamClient = new SteamClient();
            _steamCallbackManager = new CallbackManager(_steamClient);

            _steamUser = _steamClient.GetHandler<SteamUser>();

            _steamCallbackManager.Subscribe<SteamClient.ConnectedCallback>(ConnectedCallback);
            _steamCallbackManager.Subscribe<SteamClient.DisconnectedCallback>(DisconnectedCallback);

            _steamCallbackManager.Subscribe<SteamUser.LoggedOnCallback>(LogOnCallback);
            _steamCallbackManager.Subscribe<SteamUser.SessionTokenCallback>(SessionTokenCallback);
            _steamCallbackManager.Subscribe<SteamApps.LicenseListCallback>(LicenseListCallback);
            _steamCallbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(UpdateMachineAuthCallback);
            _steamCallbackManager.Subscribe<SteamUser.LoginKeyCallback>(LoginKeyCallback);
            _steamCallbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);

            _steamCallbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(MachineAuthCallback);
        }

        public void Login()
        {
            _steamClient.Connect();
        }

        public void RunCallbacks()
        {
            _steamCallbackManager.RunCallbacks();
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
            Thread.Sleep(TimeSpan.FromSeconds(5));

            _steamClient.Connect();
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
                Program.MainWindow.Show();
                Program.ConnectingToSteamWindow.Close();
            }
        }

        private void MachineAuthCallback(SteamUser.UpdateMachineAuthCallback obj)
        {
            Program.ConnectingToSteamWindow.PauseCallbacks();

            int fileSize;
            byte[] sentryHash;

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var sentryFile = Path.Combine(localAppData, obj.FileName);

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

        public void Logoff()
        {
            _steamUser.LogOff();
        }

        private void LoginKeyCallback(SteamUser.LoginKeyCallback obj)
        {
            throw new NotImplementedException();
        }

        private void UpdateMachineAuthCallback(SteamUser.UpdateMachineAuthCallback obj)
        {
            throw new NotImplementedException();
        }

        private void SessionTokenCallback(SteamUser.SessionTokenCallback obj)
        {
            throw new NotImplementedException();
        }

        private void LicenseListCallback(SteamApps.LicenseListCallback obj)
        {
            throw new NotImplementedException();
        }
    }
}
