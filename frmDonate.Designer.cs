using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace frm_winget_upgrade
{
    partial class frmDonate
    {
        private System.ComponentModel.IContainer components = null;

        private const int FORM_WIDTH      = 520;
        private const int CONTENT_HEIGHT  = 254;
        private const int FOOTER_HEIGHT   = 78;
        private const int QR_PANEL_HEIGHT = 306;

        private Panel         _footerPanel;
        private Panel         _qrPanel;
        private Guna2CheckBox _chkDontShow;
        private Guna2Button   _btnYes;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text            = "Support Winget Manager";
            this.ClientSize      = new Size(FORM_WIDTH, CONTENT_HEIGHT + FOOTER_HEIGHT);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition   = FormStartPosition.CenterParent;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.BackColor       = ThemeColors.DarkerBackground;
            this.Font            = new Font("Segoe UI", 9F);

            BuildContentArea();
            BuildQrPanel();
            BuildFooter();

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void BuildContentArea()
        {
            // Heart icon
            var heart = new PictureBox
            {
                Location  = new Point(20, 20),
                Size      = new Size(44, 44),
                BackColor = Color.Transparent
            };
            heart.Paint += Heart_Paint;
            this.Controls.Add(heart);

            // Title
            this.Controls.Add(new Label
            {
                Text      = "Thanks for using Winget Manager!",
                Font      = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(74, 22)
            });

            // Description
            this.Controls.Add(new Label
            {
                Text      = "If you found this tool helpful, please consider making a small\n" +
                            "donation to support development.",
                Font      = new Font("Segoe UI", 9.5F),
                ForeColor = ThemeColors.SecondaryText,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(20, 80)
            });

            // Emphasis
            this.Controls.Add(new Label
            {
                Text      = "Your support helps keep this project going!",
                Font      = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(20, 148)
            });

            // Sub-text
            this.Controls.Add(new Label
            {
                Text      = "Click 'Yes' to show your support!",
                Font      = new Font("Segoe UI", 9F),
                ForeColor = ThemeColors.SecondaryText,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(20, 172)
            });

            // Checkbox
            _chkDontShow = new Guna2CheckBox
            {
                Text      = "Don't show this again",
                Font      = new Font("Segoe UI", 9F),
                ForeColor = ThemeColors.SecondaryText,
                BackColor = Color.Transparent,
                Checked   = DonateSettings.DontShowAgain,
                AutoSize  = true,
                Location  = new Point(20, 206)
            };
            _chkDontShow.CheckedState.FillColor     = ThemeColors.ElectricBlue;
            _chkDontShow.CheckedState.BorderColor   = ThemeColors.ElectricBlue;
            _chkDontShow.UncheckedState.BorderColor = Color.FromArgb(80, 80, 80);
            this.Controls.Add(_chkDontShow);

            // Separator
            this.Controls.Add(new Panel
            {
                Location  = new Point(0, CONTENT_HEIGHT - 1),
                Size      = new Size(FORM_WIDTH, 1),
                BackColor = Color.FromArgb(55, 55, 55)
            });
        }

        private void BuildQrPanel()
        {
            _qrPanel = new Panel
            {
                Location  = new Point(0, CONTENT_HEIGHT),
                Size      = new Size(FORM_WIDTH, QR_PANEL_HEIGHT),
                BackColor = Color.FromArgb(22, 22, 30),
                Visible   = false
            };

            // TCB QR
            _qrPanel.Controls.Add(new PictureBox
            {
                Location    = new Point(20, 14),
                Size        = new Size(218, 210),
                SizeMode    = PictureBoxSizeMode.Zoom,
                BackColor   = Color.FromArgb(32, 32, 42),
                BorderStyle = BorderStyle.None,
                Image       = TryLoadImage("tcb.jpg")
            });
            _qrPanel.Controls.Add(new Label
            {
                Text      = "Techcombank",
                Font      = new Font("Segoe UI", 8.5F),
                ForeColor = ThemeColors.SecondaryText,
                BackColor = Color.Transparent,
                Location  = new Point(20, 228),
                Size      = new Size(218, 18),
                TextAlign = ContentAlignment.MiddleCenter
            });

            // MoMo QR
            _qrPanel.Controls.Add(new PictureBox
            {
                Location    = new Point(268, 14),
                Size        = new Size(218, 210),
                SizeMode    = PictureBoxSizeMode.Zoom,
                BackColor   = Color.FromArgb(32, 32, 42),
                BorderStyle = BorderStyle.None,
                Image       = TryLoadImage("momo.jpg")
            });
            _qrPanel.Controls.Add(new Label
            {
                Text      = "MoMo",
                Font      = new Font("Segoe UI", 8.5F),
                ForeColor = ThemeColors.SecondaryText,
                BackColor = Color.Transparent,
                Location  = new Point(268, 228),
                Size      = new Size(218, 18),
                TextAlign = ContentAlignment.MiddleCenter
            });

            // Ko-fi link (full-width, centered)
            var link = new LinkLabel
            {
                Text      = "ko-fi.com/thanhnguyen150993",
                Font      = new Font("Segoe UI", 9F),
                BackColor = Color.Transparent,
                Location  = new Point(0, 256),
                Size      = new Size(FORM_WIDTH, 22),
                TextAlign = ContentAlignment.MiddleCenter
            };
            link.LinkColor        = ThemeColors.ElectricBlue;
            link.ActiveLinkColor  = ThemeColors.ElectricBlue;
            link.VisitedLinkColor = ThemeColors.ElectricBlue;
            link.LinkClicked     += (s, e) => OpenUrl("https://ko-fi.com/thanhnguyen150993");
            _qrPanel.Controls.Add(link);

            // Bottom separator
            _qrPanel.Controls.Add(new Panel
            {
                Location  = new Point(0, QR_PANEL_HEIGHT - 1),
                Size      = new Size(FORM_WIDTH, 1),
                BackColor = Color.FromArgb(55, 55, 55)
            });

            this.Controls.Add(_qrPanel);
        }

        private void BuildFooter()
        {
            _footerPanel = new Panel
            {
                Location  = new Point(0, CONTENT_HEIGHT),
                Size      = new Size(FORM_WIDTH, FOOTER_HEIGHT),
                BackColor = ThemeColors.DeepCharcoal
            };

            _btnYes = new Guna2Button
            {
                Text            = "Yes",
                Size            = new Size(220, 38),
                Location        = new Point(20, 20),
                FillColor       = ThemeColors.ElectricBlue,
                ForeColor       = Color.Black,
                Font            = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                BorderRadius    = ThemeConstants.CornerMedium,
                BorderColor     = Color.Transparent,
                BorderThickness = 0
            };
            _btnYes.DisabledState.FillColor = Color.FromArgb(0, 100, 120);
            _btnYes.DisabledState.ForeColor = Color.FromArgb(0, 60, 80);
            _btnYes.Click += BtnYes_Click;
            _footerPanel.Controls.Add(_btnYes);

            var btnNo = new Guna2Button
            {
                Text            = "No",
                Size            = new Size(220, 38),
                Location        = new Point(260, 20),
                FillColor       = ThemeColors.DarkCharcoal,
                ForeColor       = ThemeColors.SecondaryText,
                Font            = new Font("Segoe UI", 9.5F),
                BorderRadius    = ThemeConstants.CornerMedium,
                BorderColor     = Color.FromArgb(60, 60, 60),
                BorderThickness = 1
            };
            btnNo.Click += BtnNo_Click;
            _footerPanel.Controls.Add(btnNo);

            this.Controls.Add(_footerPanel);
        }
    }
}
