using System;
using System.Drawing;
using System.Windows.Forms;

namespace frm_winget_upgrade
{
    internal static class ReleaseNotesDialog
    {
        public static DialogResult Show(IWin32Window owner, string title, string header, string body,
                                        string primaryText, string secondaryText)
        {
            using (var dlg = new Form
            {
                Text            = title,
                StartPosition   = FormStartPosition.CenterParent,
                ClientSize      = new Size(580, 420),
                BackColor       = ThemeColors.DeepCharcoal,
                ForeColor       = ThemeColors.PrimaryText,
                Font            = new Font("Segoe UI", 9F),
                MinimizeBox     = false,
                MaximizeBox     = false,
                ShowInTaskbar   = false,
                FormBorderStyle = FormBorderStyle.FixedDialog
            })
            {
                var lblHeader = new Label
                {
                    Text         = header,
                    Font         = new Font("Segoe UI", 11F, FontStyle.Bold),
                    ForeColor    = ThemeColors.ElectricBlue,
                    Location     = new Point(16, 14),
                    Size         = new Size(548, 26),
                    AutoEllipsis = true
                };

                var txtBody = new TextBox
                {
                    Multiline   = true,
                    ReadOnly    = true,
                    ScrollBars  = ScrollBars.Vertical,
                    BackColor   = ThemeColors.DarkerBackground,
                    ForeColor   = ThemeColors.SecondaryText,
                    BorderStyle = BorderStyle.FixedSingle,
                    Location    = new Point(16, 48),
                    Size        = new Size(548, 310),
                    Text        = Tidy(body)
                };

                var btnPrimary = BuildButton(primaryText, new Point(434, 370), true);
                btnPrimary.DialogResult = DialogResult.Yes;

                bool hasPrimary  = !string.IsNullOrEmpty(primaryText);
                var  btnSecondary = BuildButton(secondaryText, new Point(hasPrimary ? 296 : 434, 370), false);
                btnSecondary.DialogResult = DialogResult.No;

                btnPrimary.Visible = hasPrimary;

                dlg.Controls.AddRange(new Control[] { lblHeader, txtBody, btnPrimary, btnSecondary });
                dlg.AcceptButton = hasPrimary ? btnPrimary : btnSecondary;
                dlg.CancelButton = btnSecondary;
                txtBody.Select(0, 0);

                return dlg.ShowDialog(owner);
            }
        }

        private static string Tidy(string body) =>
            (body ?? string.Empty)
                .Replace("**", string.Empty)
                .Replace("`", string.Empty)
                .Replace("\r\n", "\n")
                .Replace("\n", "\r\n");

        private static Button BuildButton(string text, Point location, bool primary) => new Button
        {
            Text      = text,
            Location  = location,
            Size      = new Size(130, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? ThemeColors.ElectricBlue : ThemeColors.DarkCharcoal,
            ForeColor = primary ? Color.Black : ThemeColors.SecondaryText,
            Font      = new Font("Segoe UI", 9F, primary ? FontStyle.Bold : FontStyle.Regular)
        };
    }
}
