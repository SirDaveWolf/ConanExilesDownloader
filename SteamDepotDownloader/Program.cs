using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConanExilesDownloader
{
    static class Program
    {
        public static SteamDB.SteamSession SteamSession { get; set; } = new SteamDB.SteamSession();
        public static SteamDB.SteamUserCredentials SteamUserCredentials { get; set; }

        public static ConnectingToSteam ConnectingToSteamWindow { get; set; }
        public static AuthCodeWindow AuthCodeWindow { get; set; }

        public static MainWindow MainWindow { get; set; }

        public static String AppName = "Conan Exiles Downloader";
        public static String LocalConfigFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SteamContentDownloader");

        private static ApplicationContext _appContext;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Directory.CreateDirectory(LocalConfigFolder);

            _appContext = new ApplicationContext();
            _appContext.MainForm = new LoginWindow();
            _appContext.MainForm.FormClosed += MainForm_FormClosed;

            ConnectingToSteamWindow = new ConnectingToSteam();
            AuthCodeWindow = new AuthCodeWindow();
            MainWindow = new MainWindow();

            Application.Run(_appContext);

            Environment.Exit(0);
        }

        private static void MainForm_FormClosed(Object sender, EventArgs e)
        {
            if (sender == null) throw new ArgumentNullException(nameof(sender));
            if (e == null) throw new ArgumentNullException(nameof(e));

            if (_appContext.MainForm != sender || Application.OpenForms.Count == 0)
                Application.Exit();


            bool exitApplication = false;
            foreach (Form form in Application.OpenForms)
            {
                if (form == sender && Application.OpenForms.Count == 1)
                {
                    exitApplication = true;
                    break;
                }

                if (form == sender) continue;

                _appContext.MainForm = form;
                _appContext.MainForm.Closed += MainForm_FormClosed;
                return;
            }

            if (exitApplication) Application.Exit();
        }

        static void FormClosed(object sender, FormClosedEventArgs e)
        {
            ((Form)sender).FormClosed -= FormClosed;
            if (Application.OpenForms.Count == 0) Application.ExitThread();
            else Application.OpenForms[0].FormClosed += FormClosed;
        }
    }
}
