using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConanExilesDownloader
{
    public partial class ConnectingToSteam : Form
    {
        public ConnectingToSteam()
        {
            InitializeComponent();
        }

        public void PauseCallbacks()
        {
            timerSteamCallbacks.Stop();
        }

        public void ContinueCallbacks()
        {
            timerSteamCallbacks.Start();
        }

        private void ConnectingToSteam_Load(Object sender, EventArgs e)
        {
            timerSteamCallbacks.Tick += TimerSteamCallbacks_Tick;
            timerSteamCallbacks.Start();
        }

        private void TimerSteamCallbacks_Tick(Object sender, EventArgs e)
        {
            Program.SteamSession.RunCallbacks(true, TimeSpan.FromSeconds(1));
        }

        private void ConnectingToSteam_FormClosing(Object sender, FormClosingEventArgs e)
        {
            timerSteamCallbacks.Stop();
        }
    }
}
