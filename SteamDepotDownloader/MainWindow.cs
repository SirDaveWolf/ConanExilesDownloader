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

            Program.SteamSession.RunCallbacks(TimeSpan.FromSeconds(10));
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

        }

        private void buttonDownloadServer_Click(Object sender, EventArgs e)
        {

        }

        private void MainWindow_Load(Object sender, EventArgs e)
        {
            var conanClientDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ConanClient");
            var conanServerDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ConanServer");

            textBoxInstalllocationClient.Text = conanClientDirectory;
            textBoxInstalllocationServer.Text = conanServerDirectory;
        }
    }
}
