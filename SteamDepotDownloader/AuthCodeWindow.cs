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
    public partial class AuthCodeWindow : Form
    {
        private Boolean _isTwoFactorAuth;

        public AuthCodeWindow()
        {
            InitializeComponent();
        }

        public void SetInformation(Boolean isTwoFactorAuth, String text)
        {
            _isTwoFactorAuth = isTwoFactorAuth;
            label1.Text = text;
        }

        private void buttonCancel_Click(Object sender, EventArgs e)
        {
            Close();
            Application.Restart();
        }

        private void buttonOK_Click(Object sender, EventArgs e)
        {
            if (false == String.IsNullOrWhiteSpace(textBoxAuthCode.Text))
            {
                if (_isTwoFactorAuth)
                    Program.SteamUserCredentials.TwoFactorCode = textBoxAuthCode.Text;
                else
                    Program.SteamUserCredentials.AuthCode = textBoxAuthCode.Text;

                // Continue callbacks and wait for Steam's response
                Program.ConnectingToSteamWindow.ContinueCallbacks();
                Close();
            }
            else
            {
                MessageBox.Show("Please enter a valid code!", Program.AppName);
            }
        }

        private void AuthCodeWindow_Shown(Object sender, EventArgs e)
        {
            // Pause callbacks, so we won't spam login requests whilst waiting for SteamGuard
            Program.ConnectingToSteamWindow.PauseCallbacks();
        }

        private void textBoxAuthCode_KeyPress(Object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                buttonOK_Click(sender, e);
            }
        }

        private void AuthCodeWindow_FormClosed(Object sender, FormClosedEventArgs e)
        {
            Program.AuthCodeWindow = null;
        }
    }
}
