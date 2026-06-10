namespace frm_winget_upgrade
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private Guna.UI2.WinForms.Guna2Panel              topStatusBar;
        private Guna.UI2.WinForms.Guna2Panel              sidebarPanel;
        private Guna.UI2.WinForms.Guna2Panel              mainContentPanel;
        private Guna.UI2.WinForms.Guna2Panel              headerPanel;
        private Guna.UI2.WinForms.Guna2TextBox            searchBox;
        private Guna.UI2.WinForms.Guna2Button             btnRefresh;
        private Guna.UI2.WinForms.Guna2Button             btnCheckUpdates;
        private Guna.UI2.WinForms.Guna2Button             btnSettings;
        private Guna.UI2.WinForms.Guna2DataGridView       packagesGrid;
        private Guna.UI2.WinForms.Guna2ProgressBar        updateProgressBar;
        private Guna.UI2.WinForms.Guna2CircleProgressBar  overallProgress;
        private System.Windows.Forms.RichTextBox          logOutput;
        private Guna.UI2.WinForms.Guna2Button             navDashboard;
        private Guna.UI2.WinForms.Guna2Button             navInstalled;
        private Guna.UI2.WinForms.Guna2Button             navUpdates;
        private Guna.UI2.WinForms.Guna2Button             navSettings;
        private Guna.UI2.WinForms.Guna2Button             navLogs;
        private System.Windows.Forms.Label                lblTitle;
        private System.Windows.Forms.Label                lblSubtitle;
        private System.Windows.Forms.Label                lblProgressLabel;
        private System.Windows.Forms.Label                lblLogOutput;

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.topStatusBar = new Guna.UI2.WinForms.Guna2Panel();
            this.lblAppTitle = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
            this.sidebarPanel = new Guna.UI2.WinForms.Guna2Panel();
            this.lblSidebarDivider = new System.Windows.Forms.Label();
            this.navDashboard = new Guna.UI2.WinForms.Guna2Button();
            this.navInstalled = new Guna.UI2.WinForms.Guna2Button();
            this.navUpdates = new Guna.UI2.WinForms.Guna2Button();
            this.navSettings = new Guna.UI2.WinForms.Guna2Button();
            this.navLogs = new Guna.UI2.WinForms.Guna2Button();
            this.mainContentPanel = new Guna.UI2.WinForms.Guna2Panel();
            this.headerPanel = new Guna.UI2.WinForms.Guna2Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.btnRefresh = new Guna.UI2.WinForms.Guna2Button();
            this.btnCheckUpdates = new Guna.UI2.WinForms.Guna2Button();
            this.btnSettings = new Guna.UI2.WinForms.Guna2Button();
            this.searchBox = new Guna.UI2.WinForms.Guna2TextBox();
            this.packagesGrid = new Guna.UI2.WinForms.Guna2DataGridView();
            this.lblProgressLabel = new System.Windows.Forms.Label();
            this.updateProgressBar = new Guna.UI2.WinForms.Guna2ProgressBar();
            this.overallProgress = new Guna.UI2.WinForms.Guna2CircleProgressBar();
            this.lblLogOutput = new System.Windows.Forms.Label();
            this.logOutput = new System.Windows.Forms.RichTextBox();
            this.topStatusBar.SuspendLayout();
            this.sidebarPanel.SuspendLayout();
            this.mainContentPanel.SuspendLayout();
            this.headerPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.packagesGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // topStatusBar
            // 
            this.topStatusBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this.topStatusBar.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(217)))), ((int)(((byte)(255)))));
            this.topStatusBar.BorderThickness = 1;
            this.topStatusBar.Controls.Add(this.lblAppTitle);
            this.topStatusBar.Controls.Add(this.lblVersion);
            this.topStatusBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.topStatusBar.Location = new System.Drawing.Point(0, 0);
            this.topStatusBar.Name = "topStatusBar";
            this.topStatusBar.Size = new System.Drawing.Size(1100, 40);
            this.topStatusBar.TabIndex = 0;
            // 
            // lblAppTitle
            // 
            this.lblAppTitle.AutoSize = true;
            this.lblAppTitle.BackColor = System.Drawing.Color.Transparent;
            this.lblAppTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblAppTitle.ForeColor = System.Drawing.Color.White;
            this.lblAppTitle.Location = new System.Drawing.Point(14, 10);
            this.lblAppTitle.Name = "lblAppTitle";
            this.lblAppTitle.Size = new System.Drawing.Size(126, 20);
            this.lblAppTitle.TabIndex = 0;
            this.lblAppTitle.Text = "Winget Manager";
            // 
            // lblVersion
            // 
            this.lblVersion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblVersion.AutoSize = true;
            this.lblVersion.BackColor = System.Drawing.Color.Transparent;
            this.lblVersion.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblVersion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
            this.lblVersion.Location = new System.Drawing.Point(1058, 13);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(27, 13);
            this.lblVersion.TabIndex = 1;
            this.lblVersion.Text = "v1.0";
            // 
            // sidebarPanel
            // 
            this.sidebarPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this.sidebarPanel.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this.sidebarPanel.Controls.Add(this.lblSidebarDivider);
            this.sidebarPanel.Controls.Add(this.navDashboard);
            this.sidebarPanel.Controls.Add(this.navInstalled);
            this.sidebarPanel.Controls.Add(this.navUpdates);
            this.sidebarPanel.Controls.Add(this.navSettings);
            this.sidebarPanel.Controls.Add(this.navLogs);
            this.sidebarPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.sidebarPanel.Location = new System.Drawing.Point(0, 40);
            this.sidebarPanel.Name = "sidebarPanel";
            this.sidebarPanel.Size = new System.Drawing.Size(200, 640);
            this.sidebarPanel.TabIndex = 1;
            // 
            // lblSidebarDivider
            // 
            this.lblSidebarDivider.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.lblSidebarDivider.Location = new System.Drawing.Point(12, 6);
            this.lblSidebarDivider.Name = "lblSidebarDivider";
            this.lblSidebarDivider.Size = new System.Drawing.Size(176, 1);
            this.lblSidebarDivider.TabIndex = 0;
            // 
            // navDashboard
            // 
            this.navDashboard.BorderColor = System.Drawing.Color.Transparent;
            this.navDashboard.BorderRadius = 5;
            this.navDashboard.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.navDashboard.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.navDashboard.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.navDashboard.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.navDashboard.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.navDashboard.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.navDashboard.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(217)))), ((int)(((byte)(255)))));
            this.navDashboard.Location = new System.Drawing.Point(8, 10);
            this.navDashboard.Name = "navDashboard";
            this.navDashboard.Size = new System.Drawing.Size(184, 36);
            this.navDashboard.TabIndex = 0;
            this.navDashboard.Text = "📊  Dashboard";
            // 
            // navInstalled
            // 
            this.navInstalled.BorderColor = System.Drawing.Color.Transparent;
            this.navInstalled.BorderRadius = 5;
            this.navInstalled.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.navInstalled.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.navInstalled.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.navInstalled.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.navInstalled.FillColor = System.Drawing.Color.Transparent;
            this.navInstalled.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.navInstalled.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.navInstalled.Location = new System.Drawing.Point(8, 52);
            this.navInstalled.Name = "navInstalled";
            this.navInstalled.Size = new System.Drawing.Size(184, 36);
            this.navInstalled.TabIndex = 1;
            this.navInstalled.Text = "📦  Installed";
            // 
            // navUpdates
            // 
            this.navUpdates.BorderColor = System.Drawing.Color.Transparent;
            this.navUpdates.BorderRadius = 5;
            this.navUpdates.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.navUpdates.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.navUpdates.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.navUpdates.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.navUpdates.FillColor = System.Drawing.Color.Transparent;
            this.navUpdates.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.navUpdates.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.navUpdates.Location = new System.Drawing.Point(8, 94);
            this.navUpdates.Name = "navUpdates";
            this.navUpdates.Size = new System.Drawing.Size(184, 36);
            this.navUpdates.TabIndex = 2;
            this.navUpdates.Text = "⬆️  Updates";
            // 
            // navSettings
            // 
            this.navSettings.BorderColor = System.Drawing.Color.Transparent;
            this.navSettings.BorderRadius = 5;
            this.navSettings.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.navSettings.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.navSettings.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.navSettings.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.navSettings.FillColor = System.Drawing.Color.Transparent;
            this.navSettings.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.navSettings.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.navSettings.Location = new System.Drawing.Point(8, 136);
            this.navSettings.Name = "navSettings";
            this.navSettings.Size = new System.Drawing.Size(184, 36);
            this.navSettings.TabIndex = 3;
            this.navSettings.Text = "⚙️  Settings";
            // 
            // navLogs
            // 
            this.navLogs.BorderColor = System.Drawing.Color.Transparent;
            this.navLogs.BorderRadius = 5;
            this.navLogs.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.navLogs.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.navLogs.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.navLogs.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.navLogs.FillColor = System.Drawing.Color.Transparent;
            this.navLogs.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.navLogs.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.navLogs.Location = new System.Drawing.Point(8, 178);
            this.navLogs.Name = "navLogs";
            this.navLogs.Size = new System.Drawing.Size(184, 36);
            this.navLogs.TabIndex = 4;
            this.navLogs.Text = "📄  Logs";
            // 
            // mainContentPanel
            // 
            this.mainContentPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.mainContentPanel.Controls.Add(this.headerPanel);
            this.mainContentPanel.Controls.Add(this.searchBox);
            this.mainContentPanel.Controls.Add(this.packagesGrid);
            this.mainContentPanel.Controls.Add(this.lblProgressLabel);
            this.mainContentPanel.Controls.Add(this.updateProgressBar);
            this.mainContentPanel.Controls.Add(this.overallProgress);
            this.mainContentPanel.Controls.Add(this.lblLogOutput);
            this.mainContentPanel.Controls.Add(this.logOutput);
            this.mainContentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainContentPanel.Location = new System.Drawing.Point(200, 40);
            this.mainContentPanel.Name = "mainContentPanel";
            this.mainContentPanel.Padding = new System.Windows.Forms.Padding(12);
            this.mainContentPanel.Size = new System.Drawing.Size(900, 640);
            this.mainContentPanel.TabIndex = 2;
            // 
            // headerPanel
            // 
            this.headerPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this.headerPanel.BorderRadius = 6;
            this.headerPanel.Controls.Add(this.lblTitle);
            this.headerPanel.Controls.Add(this.lblSubtitle);
            this.headerPanel.Controls.Add(this.btnRefresh);
            this.headerPanel.Controls.Add(this.btnCheckUpdates);
            this.headerPanel.Controls.Add(this.btnSettings);
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerPanel.Location = new System.Drawing.Point(12, 12);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Size = new System.Drawing.Size(876, 56);
            this.headerPanel.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.BackColor = System.Drawing.Color.Transparent;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(14, 6);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(169, 25);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Installed Packages";
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.BackColor = System.Drawing.Color.Transparent;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(140)))), ((int)(((byte)(140)))));
            this.lblSubtitle.Location = new System.Drawing.Point(14, 32);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(244, 15);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "Manage and update your Windows packages";
            // 
            // btnRefresh
            // 
            this.btnRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRefresh.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.btnRefresh.BorderRadius = 5;
            this.btnRefresh.BorderThickness = 1;
            this.btnRefresh.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnRefresh.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnRefresh.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnRefresh.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnRefresh.FillColor = System.Drawing.Color.Transparent;
            this.btnRefresh.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnRefresh.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(160)))), ((int)(((byte)(160)))));
            this.btnRefresh.Location = new System.Drawing.Point(548, 12);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(88, 30);
            this.btnRefresh.TabIndex = 1;
            this.btnRefresh.Text = "🔄 Refresh";
            // 
            // btnCheckUpdates
            // 
            this.btnCheckUpdates.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCheckUpdates.BorderColor = System.Drawing.Color.Transparent;
            this.btnCheckUpdates.BorderRadius = 5;
            this.btnCheckUpdates.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnCheckUpdates.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnCheckUpdates.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnCheckUpdates.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnCheckUpdates.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(217)))), ((int)(((byte)(255)))));
            this.btnCheckUpdates.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnCheckUpdates.ForeColor = System.Drawing.Color.Black;
            this.btnCheckUpdates.Location = new System.Drawing.Point(645, 12);
            this.btnCheckUpdates.Name = "btnCheckUpdates";
            this.btnCheckUpdates.Size = new System.Drawing.Size(122, 30);
            this.btnCheckUpdates.TabIndex = 2;
            this.btnCheckUpdates.Text = "⬆️ Check Updates";
            // 
            // btnSettings
            // 
            this.btnSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSettings.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.btnSettings.BorderRadius = 5;
            this.btnSettings.BorderThickness = 1;
            this.btnSettings.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnSettings.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnSettings.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnSettings.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnSettings.FillColor = System.Drawing.Color.Transparent;
            this.btnSettings.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnSettings.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(160)))), ((int)(((byte)(160)))));
            this.btnSettings.Location = new System.Drawing.Point(776, 12);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(86, 30);
            this.btnSettings.TabIndex = 3;
            this.btnSettings.Text = "⚙️ Settings";
            // 
            // searchBox
            // 
            this.searchBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this.searchBox.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.searchBox.BorderRadius = 5;
            this.searchBox.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.searchBox.DefaultText = "";
            this.searchBox.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(131)))), ((int)(((byte)(129)))), ((int)(((byte)(129)))));
            this.searchBox.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.searchBox.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.searchBox.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.searchBox.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(217)))), ((int)(((byte)(255)))));
            this.searchBox.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.searchBox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(160)))), ((int)(((byte)(160)))));
            this.searchBox.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(217)))), ((int)(((byte)(255)))));
            this.searchBox.Location = new System.Drawing.Point(12, 80);
            this.searchBox.Margin = new System.Windows.Forms.Padding(0);
            this.searchBox.Name = "searchBox";
            this.searchBox.PlaceholderText = "🔍  Search packages...";
            this.searchBox.SelectedText = "";
            this.searchBox.Size = new System.Drawing.Size(876, 34);
            this.searchBox.TabIndex = 1;
            // 
            // packagesGrid
            // 
            this.packagesGrid.AllowUserToAddRows = false;
            this.packagesGrid.AllowUserToDeleteRows = false;
            this.packagesGrid.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(36)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(180)))), ((int)(((byte)(210)))));
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.Color.White;
            this.packagesGrid.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.packagesGrid.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(217)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(217)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.Black;
            this.packagesGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.packagesGrid.ColumnHeadersHeight = 34;
            this.packagesGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(180)))), ((int)(((byte)(210)))));
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.packagesGrid.DefaultCellStyle = dataGridViewCellStyle3;
            this.packagesGrid.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.packagesGrid.Location = new System.Drawing.Point(12, 122);
            this.packagesGrid.Name = "packagesGrid";
            this.packagesGrid.RowHeadersVisible = false;
            this.packagesGrid.RowTemplate.Height = 30;
            this.packagesGrid.Size = new System.Drawing.Size(876, 262);
            this.packagesGrid.TabIndex = 2;
            this.packagesGrid.ThemeStyle.AlternatingRowsStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(36)))));
            this.packagesGrid.ThemeStyle.AlternatingRowsStyle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.packagesGrid.ThemeStyle.AlternatingRowsStyle.ForeColor = System.Drawing.Color.White;
            this.packagesGrid.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(180)))), ((int)(((byte)(210)))));
            this.packagesGrid.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = System.Drawing.Color.White;
            this.packagesGrid.ThemeStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.packagesGrid.ThemeStyle.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.packagesGrid.ThemeStyle.HeaderStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this.packagesGrid.ThemeStyle.HeaderStyle.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.packagesGrid.ThemeStyle.HeaderStyle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(217)))), ((int)(((byte)(255)))));
            this.packagesGrid.ThemeStyle.HeaderStyle.Height = 34;
            this.packagesGrid.ThemeStyle.RowsStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.packagesGrid.ThemeStyle.RowsStyle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.packagesGrid.ThemeStyle.RowsStyle.ForeColor = System.Drawing.Color.White;
            this.packagesGrid.ThemeStyle.RowsStyle.Height = 30;
            this.packagesGrid.ThemeStyle.RowsStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(180)))), ((int)(((byte)(210)))));
            this.packagesGrid.ThemeStyle.RowsStyle.SelectionForeColor = System.Drawing.Color.White;
            // 
            // lblProgressLabel
            // 
            this.lblProgressLabel.AutoSize = true;
            this.lblProgressLabel.BackColor = System.Drawing.Color.Transparent;
            this.lblProgressLabel.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblProgressLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(160)))), ((int)(((byte)(160)))));
            this.lblProgressLabel.Location = new System.Drawing.Point(12, 400);
            this.lblProgressLabel.Name = "lblProgressLabel";
            this.lblProgressLabel.Size = new System.Drawing.Size(55, 15);
            this.lblProgressLabel.TabIndex = 3;
            this.lblProgressLabel.Text = "Progress";
            // 
            // updateProgressBar
            // 
            this.updateProgressBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.updateProgressBar.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.updateProgressBar.Location = new System.Drawing.Point(80, 402);
            this.updateProgressBar.Name = "updateProgressBar";
            this.updateProgressBar.ProgressColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(217)))), ((int)(((byte)(255)))));
            this.updateProgressBar.ProgressColor2 = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(217)))), ((int)(((byte)(255)))));
            this.updateProgressBar.Size = new System.Drawing.Size(540, 6);
            this.updateProgressBar.TabIndex = 3;
            this.updateProgressBar.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
            // 
            // overallProgress
            // 
            this.overallProgress.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.overallProgress.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.overallProgress.ForeColor = System.Drawing.Color.White;
            this.overallProgress.Location = new System.Drawing.Point(682, 386);
            this.overallProgress.Minimum = 0;
            this.overallProgress.Name = "overallProgress";
            this.overallProgress.ProgressColor = System.Drawing.Color.FromArgb(((int)(((byte)(57)))), ((int)(((byte)(255)))), ((int)(((byte)(20)))));
            this.overallProgress.Size = new System.Drawing.Size(58, 58);
            this.overallProgress.TabIndex = 4;
            this.overallProgress.Text = "0%";
            // 
            // lblLogOutput
            // 
            this.lblLogOutput.AutoSize = true;
            this.lblLogOutput.BackColor = System.Drawing.Color.Transparent;
            this.lblLogOutput.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblLogOutput.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(160)))), ((int)(((byte)(160)))));
            this.lblLogOutput.Location = new System.Drawing.Point(12, 454);
            this.lblLogOutput.Name = "lblLogOutput";
            this.lblLogOutput.Size = new System.Drawing.Size(70, 15);
            this.lblLogOutput.TabIndex = 5;
            this.lblLogOutput.Text = "Log Output";
            // 
            // logOutput
            // 
            this.logOutput.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.logOutput.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.logOutput.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.logOutput.DetectUrls = false;
            this.logOutput.Font = new System.Drawing.Font("Consolas", 8.5F);
            this.logOutput.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(57)))), ((int)(((byte)(255)))), ((int)(((byte)(20)))));
            this.logOutput.Location = new System.Drawing.Point(12, 472);
            this.logOutput.Margin = new System.Windows.Forms.Padding(0);
            this.logOutput.Name = "logOutput";
            this.logOutput.ReadOnly = true;
            this.logOutput.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.logOutput.Size = new System.Drawing.Size(876, 104);
            this.logOutput.TabIndex = 5;
            this.logOutput.Text = "";
            this.logOutput.WordWrap = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ClientSize = new System.Drawing.Size(1100, 680);
            this.Controls.Add(this.mainContentPanel);
            this.Controls.Add(this.sidebarPanel);
            this.Controls.Add(this.topStatusBar);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(900, 520);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Winget Manager";
            this.topStatusBar.ResumeLayout(false);
            this.topStatusBar.PerformLayout();
            this.sidebarPanel.ResumeLayout(false);
            this.mainContentPanel.ResumeLayout(false);
            this.mainContentPanel.PerformLayout();
            this.headerPanel.ResumeLayout(false);
            this.headerPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.packagesGrid)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblAppTitle;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Label lblSidebarDivider;
    }
}
