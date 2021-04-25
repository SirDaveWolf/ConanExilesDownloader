
namespace ConanExilesDownloader
{
    partial class MainWindow
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxAppId = new System.Windows.Forms.TextBox();
            this.textBoxDepotIdContent = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxDepotIdBinaries = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.linkLabelGoToContent = new System.Windows.Forms.LinkLabel();
            this.linkLabelGoToBinaries = new System.Windows.Forms.LinkLabel();
            this.linkLabelGoToServer = new System.Windows.Forms.LinkLabel();
            this.label5 = new System.Windows.Forms.Label();
            this.textBoxInstalllocationClient = new System.Windows.Forms.TextBox();
            this.textBoxInstalllocationServer = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.buttonBrowsePathClient = new System.Windows.Forms.Button();
            this.buttonBrowsePathServer = new System.Windows.Forms.Button();
            this.progressBarDownload = new System.Windows.Forms.ProgressBar();
            this.buttonDownloadClient = new System.Windows.Forms.Button();
            this.buttonDownloadServer = new System.Windows.Forms.Button();
            this.buttonQuit = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(81, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Steam DB Link:";
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(147, 9);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(209, 13);
            this.linkLabel1.TabIndex = 1;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "https://steamdb.info/app/440900/depots/";
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(119, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Manifest Client Content:";
            // 
            // textBoxAppId
            // 
            this.textBoxAppId.Location = new System.Drawing.Point(150, 37);
            this.textBoxAppId.Name = "textBoxAppId";
            this.textBoxAppId.Size = new System.Drawing.Size(206, 20);
            this.textBoxAppId.TabIndex = 3;
            // 
            // textBoxDepotIdContent
            // 
            this.textBoxDepotIdContent.Location = new System.Drawing.Point(150, 63);
            this.textBoxDepotIdContent.Name = "textBoxDepotIdContent";
            this.textBoxDepotIdContent.Size = new System.Drawing.Size(206, 20);
            this.textBoxDepotIdContent.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 66);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(119, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Manifest Client Binaries:";
            // 
            // textBoxDepotIdBinaries
            // 
            this.textBoxDepotIdBinaries.Location = new System.Drawing.Point(150, 89);
            this.textBoxDepotIdBinaries.Name = "textBoxDepotIdBinaries";
            this.textBoxDepotIdBinaries.Size = new System.Drawing.Size(206, 20);
            this.textBoxDepotIdBinaries.TabIndex = 7;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 92);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(84, 13);
            this.label4.TabIndex = 6;
            this.label4.Text = "Manifest Server:";
            // 
            // linkLabelGoToContent
            // 
            this.linkLabelGoToContent.AutoSize = true;
            this.linkLabelGoToContent.Location = new System.Drawing.Point(362, 40);
            this.linkLabelGoToContent.Name = "linkLabelGoToContent";
            this.linkLabelGoToContent.Size = new System.Drawing.Size(77, 13);
            this.linkLabelGoToContent.TabIndex = 8;
            this.linkLabelGoToContent.TabStop = true;
            this.linkLabelGoToContent.Text = "Where to find?";
            this.linkLabelGoToContent.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelGoToContent_LinkClicked);
            // 
            // linkLabelGoToBinaries
            // 
            this.linkLabelGoToBinaries.AutoSize = true;
            this.linkLabelGoToBinaries.Location = new System.Drawing.Point(362, 66);
            this.linkLabelGoToBinaries.Name = "linkLabelGoToBinaries";
            this.linkLabelGoToBinaries.Size = new System.Drawing.Size(77, 13);
            this.linkLabelGoToBinaries.TabIndex = 9;
            this.linkLabelGoToBinaries.TabStop = true;
            this.linkLabelGoToBinaries.Text = "Where to find?";
            this.linkLabelGoToBinaries.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelGoToBinaries_LinkClicked);
            // 
            // linkLabelGoToServer
            // 
            this.linkLabelGoToServer.AutoSize = true;
            this.linkLabelGoToServer.Location = new System.Drawing.Point(362, 92);
            this.linkLabelGoToServer.Name = "linkLabelGoToServer";
            this.linkLabelGoToServer.Size = new System.Drawing.Size(77, 13);
            this.linkLabelGoToServer.TabIndex = 10;
            this.linkLabelGoToServer.TabStop = true;
            this.linkLabelGoToServer.Text = "Where to find?";
            this.linkLabelGoToServer.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelGoToServer_LinkClicked);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 146);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(110, 13);
            this.label5.TabIndex = 11;
            this.label5.Text = "Install-Location Client:";
            // 
            // textBoxInstalllocationClient
            // 
            this.textBoxInstalllocationClient.Location = new System.Drawing.Point(15, 162);
            this.textBoxInstalllocationClient.Name = "textBoxInstalllocationClient";
            this.textBoxInstalllocationClient.Size = new System.Drawing.Size(424, 20);
            this.textBoxInstalllocationClient.TabIndex = 12;
            // 
            // textBoxInstalllocationServer
            // 
            this.textBoxInstalllocationServer.Location = new System.Drawing.Point(15, 201);
            this.textBoxInstalllocationServer.Name = "textBoxInstalllocationServer";
            this.textBoxInstalllocationServer.Size = new System.Drawing.Size(424, 20);
            this.textBoxInstalllocationServer.TabIndex = 14;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 185);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(115, 13);
            this.label6.TabIndex = 13;
            this.label6.Text = "Install-Location Server:";
            // 
            // buttonBrowsePathClient
            // 
            this.buttonBrowsePathClient.Location = new System.Drawing.Point(445, 160);
            this.buttonBrowsePathClient.Name = "buttonBrowsePathClient";
            this.buttonBrowsePathClient.Size = new System.Drawing.Size(75, 23);
            this.buttonBrowsePathClient.TabIndex = 15;
            this.buttonBrowsePathClient.Text = "Browse";
            this.buttonBrowsePathClient.UseVisualStyleBackColor = true;
            this.buttonBrowsePathClient.Click += new System.EventHandler(this.BrowsePath);
            // 
            // buttonBrowsePathServer
            // 
            this.buttonBrowsePathServer.Location = new System.Drawing.Point(445, 201);
            this.buttonBrowsePathServer.Name = "buttonBrowsePathServer";
            this.buttonBrowsePathServer.Size = new System.Drawing.Size(75, 23);
            this.buttonBrowsePathServer.TabIndex = 16;
            this.buttonBrowsePathServer.Text = "Browse";
            this.buttonBrowsePathServer.UseVisualStyleBackColor = true;
            this.buttonBrowsePathServer.Click += new System.EventHandler(this.BrowsePath);
            // 
            // progressBarDownload
            // 
            this.progressBarDownload.Location = new System.Drawing.Point(15, 255);
            this.progressBarDownload.Name = "progressBarDownload";
            this.progressBarDownload.Size = new System.Drawing.Size(505, 23);
            this.progressBarDownload.TabIndex = 17;
            // 
            // buttonDownloadClient
            // 
            this.buttonDownloadClient.Location = new System.Drawing.Point(420, 284);
            this.buttonDownloadClient.Name = "buttonDownloadClient";
            this.buttonDownloadClient.Size = new System.Drawing.Size(100, 23);
            this.buttonDownloadClient.TabIndex = 18;
            this.buttonDownloadClient.Text = "Download Client";
            this.buttonDownloadClient.UseVisualStyleBackColor = true;
            this.buttonDownloadClient.Click += new System.EventHandler(this.buttonDownloadClient_Click);
            // 
            // buttonDownloadServer
            // 
            this.buttonDownloadServer.Location = new System.Drawing.Point(314, 284);
            this.buttonDownloadServer.Name = "buttonDownloadServer";
            this.buttonDownloadServer.Size = new System.Drawing.Size(100, 23);
            this.buttonDownloadServer.TabIndex = 19;
            this.buttonDownloadServer.Text = "Download Server";
            this.buttonDownloadServer.UseVisualStyleBackColor = true;
            this.buttonDownloadServer.Click += new System.EventHandler(this.buttonDownloadServer_Click);
            // 
            // buttonQuit
            // 
            this.buttonQuit.Location = new System.Drawing.Point(15, 285);
            this.buttonQuit.Name = "buttonQuit";
            this.buttonQuit.Size = new System.Drawing.Size(75, 23);
            this.buttonQuit.TabIndex = 20;
            this.buttonQuit.Text = "Quit";
            this.buttonQuit.UseVisualStyleBackColor = true;
            this.buttonQuit.Click += new System.EventHandler(this.buttonQuit_Click);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(530, 320);
            this.Controls.Add(this.buttonQuit);
            this.Controls.Add(this.buttonDownloadServer);
            this.Controls.Add(this.buttonDownloadClient);
            this.Controls.Add(this.progressBarDownload);
            this.Controls.Add(this.buttonBrowsePathServer);
            this.Controls.Add(this.buttonBrowsePathClient);
            this.Controls.Add(this.textBoxInstalllocationServer);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.textBoxInstalllocationClient);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.linkLabelGoToServer);
            this.Controls.Add(this.linkLabelGoToBinaries);
            this.Controls.Add(this.linkLabelGoToContent);
            this.Controls.Add(this.textBoxDepotIdBinaries);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textBoxDepotIdContent);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBoxAppId);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.linkLabel1);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "MainWindow";
            this.Text = "Conan Exiles Downloader";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxAppId;
        private System.Windows.Forms.TextBox textBoxDepotIdContent;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxDepotIdBinaries;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.LinkLabel linkLabelGoToContent;
        private System.Windows.Forms.LinkLabel linkLabelGoToBinaries;
        private System.Windows.Forms.LinkLabel linkLabelGoToServer;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBoxInstalllocationClient;
        private System.Windows.Forms.TextBox textBoxInstalllocationServer;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button buttonBrowsePathClient;
        private System.Windows.Forms.Button buttonBrowsePathServer;
        private System.Windows.Forms.ProgressBar progressBarDownload;
        private System.Windows.Forms.Button buttonDownloadClient;
        private System.Windows.Forms.Button buttonDownloadServer;
        private System.Windows.Forms.Button buttonQuit;
    }
}

