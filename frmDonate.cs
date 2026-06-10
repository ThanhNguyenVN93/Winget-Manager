using System;
using System.Diagnostics;
using System.Linq;
using Guna.UI2.WinForms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace frm_winget_upgrade
{
    internal sealed partial class frmDonate : Form
    {
        public frmDonate()
        {
            InitializeComponent();
        }

        // ── Button handlers ───────────────────────────────────────────────────

        private void BtnYes_Click(object sender, EventArgs e)
        {
            if (_qrPanel.Visible) return;

            _qrPanel.Location     = new Point(0, CONTENT_HEIGHT);
            _footerPanel.Location = new Point(0, CONTENT_HEIGHT + QR_PANEL_HEIGHT);
            this.ClientSize       = new Size(FORM_WIDTH, CONTENT_HEIGHT + QR_PANEL_HEIGHT + FOOTER_HEIGHT);
            _qrPanel.Visible      = true;

            foreach (var btn in _footerPanel.Controls.OfType<Guna2Button>())
                btn.Enabled = false;
        }

        private void BtnNo_Click(object sender, EventArgs e) => this.Close();

        // ── Persistence ───────────────────────────────────────────────────────

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            DonateSettings.DontShowAgain = _chkDontShow.Checked;
            base.OnFormClosing(e);
        }

        // ── Drawing ───────────────────────────────────────────────────────────

        private static void Heart_Paint(object sender, PaintEventArgs e)
        {
            var pb = (PictureBox)sender;
            e.Graphics.SmoothingMode     = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            using (var font  = new Font("Segoe UI Emoji", 28F))
            using (var brush = new SolidBrush(ThemeColors.ErrorRed))
            using (var sf    = new StringFormat
                   {
                       Alignment     = StringAlignment.Center,
                       LineAlignment = StringAlignment.Center
                   })
                e.Graphics.DrawString("♥", font, brush,
                    new RectangleF(0, 0, pb.Width, pb.Height), sf);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Image TryLoadImage(string fileName)
        {
            // Try embedded resource first (distribution / single-EXE).
            string resourceName = "frm_winget_upgrade." + fileName;
            try
            {
                using (var stream = Assembly.GetExecutingAssembly()
                                            .GetManifestResourceStream(resourceName))
                    if (stream != null) return Image.FromStream(new MemoryStream(ReadAll(stream)));
            }
            catch { }

            // Fallback: load from file next to EXE (development convenience).
            string dir  = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            string path = Path.Combine(dir, fileName);
            try   { return File.Exists(path) ? Image.FromFile(path) : null; }
            catch { return null; }
        }

        private static byte[] ReadAll(Stream stream)
        {
            var buf = new byte[stream.Length];
            stream.Read(buf, 0, buf.Length);
            return buf;
        }

        private static void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { }
        }
    }
}
