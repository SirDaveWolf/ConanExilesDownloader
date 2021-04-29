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
    public partial class LoginWindow : Form
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void buttonCancel_Click(Object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void buttonLogin_Click(Object sender, EventArgs e)
        {
            var username = textBoxUsername.Text;
            var password = textBoxPassword.Text;

            var isValid = !String.IsNullOrWhiteSpace(username);
            if (false == isValid)
                MessageBox.Show("Please enter a valid username!", Program.AppName);

            isValid = !String.IsNullOrWhiteSpace(password);
            if (false == isValid)
                MessageBox.Show("Please enter a valid password!", Program.AppName);

            if(isValid)
            {
                Program.SteamUserCredentials = new SteamDB.SteamUserCredentials(username, password);
                Program.SteamSession.Login();

                Program.ConnectingToSteamWindow.Show();
                Close();
            }
        }

        private void textBoxPassword_KeyPress(Object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar == 13)
            {
                buttonLogin_Click(sender, e);
            }
        }

        private void LoginWindow_Load(Object sender, EventArgs e)
        {
            CenterToScreen();
        }
    }
}
