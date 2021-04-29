using ConanExilesDownloader.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConanExilesDownloader
{
    public partial class MainWindow : Form
    {
        private const UInt32 AppIdClient = 440900;
        private const UInt32 AppIdServer = 443030;
        private const UInt32 DepotIdClientContent = 440901;
        private const UInt32 DepotIdClientBinaries = 440902;
        private const UInt32 DepotIdServer = 443031;

        public MainWindow()
        {
            InitializeComponent();
        }

        private delegate void PGB(Int32 v);
        private PGB _setProgressBar;

        public void SetProgressBar(Int32 value)
        {
            if (this.progressBarDownload.InvokeRequired)
                this.Invoke(_setProgressBar, value);
            this.progressBarDownload.Value = value;
        }

        private void linkLabel1_LinkClicked(Object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://steamdb.info/app/440900/depots/");
        }

        private void linkLabelGoToContent_LinkClicked(Object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://steamdb.info/depot/440901/manifests/");
        }

        private void linkLabelGoToBinaries_LinkClicked(Object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://steamdb.info/depot/440902/manifests/");
        }

        private void linkLabelGoToServer_LinkClicked(Object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://steamdb.info/depot/443031/manifests/");
        }

        private void buttonQuit_Click(Object sender, EventArgs e)
        {
            Program.SteamSession.Logoff(() =>
            {
                Close();
            });

            Program.SteamSession.RunCallbacks(true, TimeSpan.FromSeconds(10));
        }

        private void BrowsePathClient(Object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.SelectedPath = textBoxInstalllocationClient.Text;
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    textBoxInstalllocationClient.Text = fbd.SelectedPath;
                }
            }
        }

        private void BrowsePathServer(Object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.SelectedPath = textBoxInstalllocationServer.Text;
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    textBoxInstalllocationServer.Text = fbd.SelectedPath;
                }
            }
        }

        private void buttonDownloadClient_Click(Object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace(textBoxManifestContent.Text))
            {
                MessageBox.Show("Please enter a valid manifest ID for the client content!", Program.AppName);
                return;
            }

            if (String.IsNullOrWhiteSpace(textBoxInstalllocationClient.Text))
            {
                MessageBox.Show("Please provide a client install folder!", Program.AppName);
                return;
            }

            SetControlStatus(false);

            try
            {
                UInt64 manifestContent = Convert.ToUInt64(textBoxManifestContent.Text);
                Task.Run(async () =>
                {
                    await SteamDB.ContentDownloader.DownloadAppAsync(AppIdClient, DepotIdClientContent, manifestContent, textBoxInstalllocationClient.Text);
                    MessageBox.Show("Client Content download finished!", Program.AppName);
                });

                if (false == String.IsNullOrWhiteSpace(textBoxManifestBinaries.Text))
                {
                    UInt64 manifestBinaries = Convert.ToUInt64(textBoxManifestBinaries.Text);

                    Task.Run(async () =>
                    {
                        await SteamDB.ContentDownloader.DownloadAppAsync(AppIdClient, DepotIdClientBinaries, manifestBinaries, textBoxInstalllocationClient.Text);
                        MessageBox.Show("Client Binaries download finished!", Program.AppName);
                    });
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Error: {ex}", Program.AppName);
                FileLog.LogMessage($"Error at download: {ex}");
            }
            finally
            {
                SetControlStatus(true);
            }
        }

        private void buttonDownloadServer_Click(Object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace(textBoxManifestServer.Text))
            {
                MessageBox.Show("Please enter a valid manifest ID for the server content!", Program.AppName);
                return;
            }

            if (String.IsNullOrWhiteSpace(textBoxInstalllocationServer.Text))
            {
                MessageBox.Show("Please provide a server install folder!", Program.AppName);
                return;
            }

            SetControlStatus(false);

            try
            {
                UInt64 manifestContent = Convert.ToUInt64(textBoxManifestServer.Text);
                Task.Run(async () =>
                {
                    await SteamDB.ContentDownloader.DownloadAppAsync(AppIdServer, DepotIdServer, manifestContent, textBoxInstalllocationServer.Text);
                    MessageBox.Show("Server download finished!", Program.AppName);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex}", Program.AppName);
                FileLog.LogMessage($"Error at download: {ex}");
            }
            finally
            {
                SetControlStatus(true);
            }
        }

        private void MainWindow_Load(Object sender, EventArgs e)
        {
            CenterToScreen();

            _setProgressBar = SetProgressBar;

            var conanClientDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ConanClient");
            var conanServerDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ConanServer");

            textBoxInstalllocationClient.Text = conanClientDirectory;
            textBoxInstalllocationServer.Text = conanServerDirectory;

            timerCallbacks.Start();
        }

        private void timerCallbacks_Tick(Object sender, EventArgs e)
        {
            Program.SteamSession.RunCallbacks();
        }

        private void SetControlStatus(Boolean enabled)
        {
            buttonDownloadClient.Enabled = enabled;
            buttonDownloadServer.Enabled = enabled;
        }
    }
}
