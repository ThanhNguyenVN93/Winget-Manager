using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace frm_winget_upgrade
{
    public partial class Form1 : Form
    {
        private const int SIDEBAR_EXPANDED      = ThemeConstants.SidebarWidthExpanded;
        private const int SIDEBAR_COLLAPSED     = ThemeConstants.SidebarWidthCollapsed;
        private const int RESPONSIVE_BREAKPOINT = ThemeConstants.ResponsiveBreakpointSmall;

        // Column indices — reconfigured per view
        private const int COL_SELECT    = 0;
        private const int COL_NAME      = 1;
        private const int COL_ID        = 2;
        private const int COL_VERSION   = 3;
        private const int COL_AVAILABLE = 4;
        private const int COL_STATUS    = 5;   // Updates view
        private const int COL_SOURCE    = 4;   // Installed view (reuses slot 4)

        private const int LOG_MAX_LINES  = 1000;
        private const int LOG_KEEP_LINES = 500;

        private readonly WingetService       _service     = new WingetService();
        private          List<WingetPackage> _allPackages = new List<WingetPackage>();
        private          Guna2Button         _btnAction;   // upgrade or uninstall depending on view
        private          bool                _sidebarCollapsed;
        private          bool                _isBusy;
        private          CancellationTokenSource _cts;
        private          CancellationTokenSource _viewLoadCts;
        private          Panel               _settingsPanel;
        private          string              _activeView = "Dashboard";

        private static readonly string _logFilePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "WingetManager", "upgrade.log");
        private static readonly object _logFileLock = new object();

        private readonly System.Windows.Forms.Timer _resizeDebounce;
        private readonly System.Windows.Forms.Timer _searchDebounce;

        public Form1()
        {
            InitializeComponent();
            using (var s = System.Reflection.Assembly.GetExecutingAssembly()
                               .GetManifestResourceStream("frm_winget_upgrade.winget_manager.ico"))
                if (s != null) this.Icon = new System.Drawing.Icon(s);
            _resizeDebounce       = new System.Windows.Forms.Timer { Interval = 150 };
            _resizeDebounce.Tick += ResizeDebounce_Tick;
            _searchDebounce       = new System.Windows.Forms.Timer { Interval = 200 };
            _searchDebounce.Tick += SearchDebounce_Tick;
            this.Load        += Form1_Load;
            this.Resize      += Form1_Resize;
            this.FormClosing += Form1_FormClosing;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            if (!IsRunningAsAdministrator())
            {
                var choice = MessageBox.Show(
                    "Winget Manager requires Administrator privileges to install " +
                    "and upgrade system packages.\n\n" +
                    "Would you like to restart the application as Administrator now?",
                    "Administrator Privileges Required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button1);

                if (choice == DialogResult.Yes) TryRestartAsAdministrator();
                Application.Exit();
                return;
            }

            try
            {
                ApplyTheme(this);
                InitializeUpdatesGrid();
                EnableGridDoubleBuffering(packagesGrid);
                SetupEventHandlers();
                await ScanForUpdatesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup error:\n\n{ex.Message}\n\n{ex.StackTrace}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isBusy)
            {
                var choice = MessageBox.Show(
                    "Tác vụ đang diễn ra. Bạn có chắc chắn muốn thoát?",
                    "Đang thực thi",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (choice == DialogResult.No) { e.Cancel = true; return; }
                _cts?.Cancel();
            }

            if (!DonateSettings.DontShowAgain)
            {
                using (var donateForm = new frmDonate())
                    donateForm.ShowDialog(this);
            }
        }

        private static bool IsRunningAsAdministrator()
        {
            using (var identity = WindowsIdentity.GetCurrent())
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void TryRestartAsAdministrator()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName        = Application.ExecutablePath,
                    UseShellExecute = true,
                    Verb            = "runas"
                });
            }
            catch { }
        }

        // ── Resize debounce ───────────────────────────────────────────────────

        private void Form1_Resize(object sender, EventArgs e)
        {
            _resizeDebounce.Stop();
            _resizeDebounce.Start();
        }

        private void ResizeDebounce_Tick(object sender, EventArgs e)
        {
            _resizeDebounce.Stop();
            ApplyResponsiveLayout();
        }

        private void ApplyResponsiveLayout()
        {
            if (this.ClientSize.Width < RESPONSIVE_BREAKPOINT && !_sidebarCollapsed)
                CollapseSidebar();
            else if (this.ClientSize.Width >= RESPONSIVE_BREAKPOINT && _sidebarCollapsed)
                ExpandSidebar();

            if (_activeView == "Logs")
                logOutput.Size = new Size(
                    mainContentPanel.ClientSize.Width  - 24,
                    mainContentPanel.ClientSize.Height - 105 - 12);
            else if (_activeView == "Settings" && _settingsPanel != null)
                _settingsPanel.Size = new Size(mainContentPanel.ClientSize.Width - 24, 360);
        }

        // ── Grid initialisation ───────────────────────────────────────────────

        private void InitializeUpdatesGrid()
        {
            packagesGrid.Columns.Clear();

            packagesGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name               = "Select",
                HeaderText         = "✓",
                Width              = 40,
                TrueValue          = true,
                FalseValue         = false,
                IndeterminateValue = false,
                ReadOnly           = false
            });

            var cols = new[]
            {
                new { H = "Package",   W = 215, Fill = false },
                new { H = "ID",        W = 180, Fill = false },
                new { H = "Version",   W =  90, Fill = false },
                new { H = "Available", W =  90, Fill = false },
                new { H = "Status",    W = 100, Fill = true  }
            };

            foreach (var c in cols)
            {
                packagesGrid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name         = c.H,
                    HeaderText   = c.H,
                    Width        = c.W,
                    AutoSizeMode = c.Fill
                        ? DataGridViewAutoSizeColumnMode.Fill
                        : DataGridViewAutoSizeColumnMode.None,
                    ReadOnly = true,
                    SortMode = DataGridViewColumnSortMode.Automatic
                });
            }
        }

        private void InitializeInstalledGrid()
        {
            packagesGrid.Columns.Clear();

            packagesGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name               = "Select",
                HeaderText         = "✓",
                Width              = 40,
                TrueValue          = true,
                FalseValue         = false,
                IndeterminateValue = false,
                ReadOnly           = false
            });

            var cols = new[]
            {
                new { H = "Package", W = 240, Fill = false },
                new { H = "ID",      W = 220, Fill = false },
                new { H = "Version", W = 110, Fill = false },
                new { H = "Source",  W = 100, Fill = true  }
            };

            foreach (var c in cols)
            {
                packagesGrid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name         = c.H,
                    HeaderText   = c.H,
                    Width        = c.W,
                    AutoSizeMode = c.Fill
                        ? DataGridViewAutoSizeColumnMode.Fill
                        : DataGridViewAutoSizeColumnMode.None,
                    ReadOnly = true,
                    SortMode = DataGridViewColumnSortMode.Automatic
                });
            }
        }

        // ── Event wiring ──────────────────────────────────────────────────────

        private void SetupEventHandlers()
        {
            btnCheckUpdates.Click += BtnScanUpdates_Click;
            btnRefresh.Click      += BtnRefresh_Click;
            btnSettings.Click     += (s, e) => { HighlightNavButton(navSettings); NavigateTo("Settings", "Configure Winget Manager preferences"); };
            searchBox.TextChanged += SearchBox_TextChanged;
            packagesGrid.ColumnHeaderMouseClick       += PackagesGrid_HeaderClick;
            packagesGrid.CurrentCellDirtyStateChanged += PackagesGrid_CurrentCellDirtyStateChanged;
            packagesGrid.CellValueChanged             += PackagesGrid_CellValueChanged;

            WireNavButton(navDashboard, "Dashboard",          "Overview of your package management system");
            WireNavButton(navInstalled, "Installed Packages", "Manage and update your Windows packages");
            WireNavButton(navUpdates,   "Available Updates",  "Packages ready for update");
            WireNavButton(navSettings,  "Settings",           "Configure Winget Manager preferences");
            navLogs.Click += (s, e) => Process.Start(new ProcessStartInfo("https://www.facebook.com/thanhitma9x") { UseShellExecute = true });

            int btnX = overallProgress.Right + ThemeConstants.Spacing8;
            int btnW = mainContentPanel.ClientSize.Width - ThemeConstants.Spacing12 - btnX;

            _btnAction = new Guna2Button
            {
                Name         = "btnAction",
                Text         = "⬆  Upgrade Selected",
                Location     = new Point(btnX, overallProgress.Top),
                Size         = new Size(Math.Max(btnW, 80), overallProgress.Height),
                Anchor       = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                FillColor    = ThemeColors.ElectricBlue,
                ForeColor    = Color.Black,
                Font         = new Font("Segoe UI", 9F, FontStyle.Bold),
                BorderRadius = ThemeConstants.CornerMedium,
                BorderColor  = Color.Transparent,
                Enabled      = false
            };
            _btnAction.DisabledState.FillColor = Color.FromArgb(48, 48, 48);
            _btnAction.DisabledState.ForeColor = Color.FromArgb(90, 90, 90);
            _btnAction.Click += BtnAction_Click;
            mainContentPanel.Controls.Add(_btnAction);
        }

        private async void BtnScanUpdates_Click(object sender, EventArgs e)
        {
            _viewLoadCts?.Cancel();
            _viewLoadCts?.Dispose();
            _viewLoadCts = new CancellationTokenSource();
            await ScanForUpdatesAsync(_viewLoadCts.Token);
        }

        private void BtnRefresh_Click(object sender, EventArgs e) => RefreshGridDisplay();

        private async void BtnAction_Click(object sender, EventArgs e)
        {
            if (_activeView == "Installed Packages")
                await ExecuteSelectedUninstallsAsync();
            else
                await ExecuteSelectedUpgradesAsync();
        }

        private void RefreshGridDisplay()
        {
            if (_allPackages.Count == 0)
            {
                AddLogEntry("No scan data — use Check Updates first.", ThemeColors.InfoBlue);
                return;
            }
            searchBox.Clear();
            PopulateGrid(_allPackages);
            AddLogEntry($"Display refreshed — {_allPackages.Count} package(s).", ThemeColors.InfoBlue);
        }

        private void PackagesGrid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (packagesGrid.CurrentCell?.ColumnIndex == COL_SELECT)
                packagesGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void PackagesGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != COL_SELECT) return;
            UpdateActionButtonEnabled();
        }

        private void UpdateActionButtonEnabled()
        {
            if (_btnAction == null) return;
            bool anyChecked = packagesGrid.Rows
                .Cast<DataGridViewRow>()
                .Any(r => r.Cells[COL_SELECT].Value is true && r.Visible);
            SetActionEnabled(anyChecked);
        }

        private void WireNavButton(Guna2Button btn, string title, string subtitle)
        {
            btn.Click += (s, e) => { HighlightNavButton(btn); NavigateTo(title, subtitle); };
        }

        // ── Scan for updates ──────────────────────────────────────────────────

        private async Task ScanForUpdatesAsync(CancellationToken ct = default)
        {
            string viewAtStart = _activeView;

            SetControlsEnabled(false);
            SetActionEnabled(false);
            packagesGrid.Rows.Clear();
            _allPackages.Clear();
            UpdateProgress(0);
            AddLogEntry("Scanning for available updates via winget…", ThemeColors.InfoBlue);

            try
            {
                var packages = await _service.GetAvailableUpdatesAsync(ct);

                if (_activeView != viewAtStart) return;

                _allPackages = packages;

                if (_allPackages.Count == 0)
                {
                    AddLogEntry("All packages are up to date.", ThemeColors.SuccessGreen);
                }
                else
                {
                    PopulateGrid(_allPackages);
                    AddLogEntry($"{_allPackages.Count} update(s) available.", ThemeColors.SuccessGreen);
                    SetActionEnabled(true);
                }

                UpdateProgress(100);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                AddLogEntry($"Scan failed: {ex.Message}", ThemeColors.ErrorRed);
            }
            finally
            {
                SetControlsEnabled(true);
            }
        }

        // ── Load installed packages ───────────────────────────────────────────

        private async Task LoadInstalledPackagesAsync(CancellationToken ct = default)
        {
            string viewAtStart = _activeView;

            SetControlsEnabled(false);
            SetActionEnabled(false);
            packagesGrid.Rows.Clear();
            _allPackages.Clear();
            UpdateProgress(0);
            AddLogEntry("Loading installed packages via winget…", ThemeColors.InfoBlue);

            try
            {
                var packages = await _service.GetInstalledPackagesAsync(ct);

                // User navigated away while scanning — abort to prevent column-mismatch exceptions.
                if (_activeView != viewAtStart) return;

                _allPackages = packages;

                if (_allPackages.Count == 0)
                {
                    AddLogEntry("No installed packages found.", ThemeColors.WarningOrange);
                }
                else
                {
                    PopulateInstalledGrid(_allPackages);
                    AddLogEntry($"{_allPackages.Count} installed package(s) found.", ThemeColors.SuccessGreen);
                }

                UpdateProgress(100);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                AddLogEntry($"Load failed: {ex.Message}", ThemeColors.ErrorRed);
            }
            finally
            {
                SetControlsEnabled(true);
            }
        }

        // ── Grid population ───────────────────────────────────────────────────

        private void PopulateGrid(IEnumerable<WingetPackage> packages)
        {
            if (packagesGrid.InvokeRequired) { packagesGrid.Invoke(new Action(() => PopulateGrid(packages))); return; }

            packagesGrid.Rows.Clear();
            foreach (var pkg in packages)
            {
                var (back, fore) = GetStatusColors(pkg.CurrentStatus);
                int idx = packagesGrid.Rows.Add(false, pkg.Name, pkg.Id,
                                                pkg.InstalledVersion, pkg.AvailableVersion,
                                                pkg.CurrentStatus);
                ApplyStatusCellStyle(idx, pkg.CurrentStatus, back, fore);
            }

            packagesGrid.Columns[COL_SELECT].HeaderText = "✓";
        }

        private void PopulateInstalledGrid(IEnumerable<WingetPackage> packages)
        {
            if (packagesGrid.InvokeRequired) { packagesGrid.Invoke(new Action(() => PopulateInstalledGrid(packages))); return; }

            packagesGrid.Rows.Clear();
            foreach (var pkg in packages)
            {
                bool isLocal = pkg.Source == "Local Registry";
                int idx = packagesGrid.Rows.Add(false, pkg.Name, pkg.Id,
                                                pkg.InstalledVersion, pkg.Source);

                // Disable checkbox for local-registry rows — no winget ID to uninstall against.
                var checkCell = packagesGrid.Rows[idx].Cells[COL_SELECT] as DataGridViewCheckBoxCell;
                if (checkCell != null && isLocal)
                {
                    checkCell.ReadOnly = true;
                    checkCell.Style.BackColor = Color.FromArgb(40, 40, 40);
                    checkCell.Style.ForeColor = Color.FromArgb(70, 70, 70);
                }

                // Dim the entire row for local packages.
                if (isLocal)
                {
                    foreach (DataGridViewCell cell in packagesGrid.Rows[idx].Cells)
                    {
                        cell.Style.ForeColor = Color.FromArgb(90, 90, 90);
                    }
                }
            }

            packagesGrid.Columns[COL_SELECT].HeaderText = "✓";
        }

        private void ApplyStatusCellStyle(int rowIndex, string text, Color back, Color fore)
        {
            if (packagesGrid.InvokeRequired)
            {
                packagesGrid.Invoke(new Action(() => ApplyStatusCellStyle(rowIndex, text, back, fore)));
                return;
            }
            if (rowIndex < 0 || rowIndex >= packagesGrid.Rows.Count) return;

            var cell             = packagesGrid.Rows[rowIndex].Cells[COL_STATUS];
            cell.Value           = text;
            cell.Style.BackColor = back;
            cell.Style.ForeColor = fore;
            cell.Style.Font      = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        }

        private void SetPackageStatus(int rowIndex, string packageId, string statusText, Color back, Color fore)
        {
            var pkg = _allPackages.FirstOrDefault(p => p.Id == packageId);
            if (pkg != null) pkg.CurrentStatus = statusText;
            ApplyStatusCellStyle(rowIndex, statusText, back, fore);
        }

        private static (Color Back, Color Fore) GetStatusColors(string status)
        {
            if (status?.StartsWith("✓") == true) return (ThemeColors.SuccessGreen,  Color.Black);
            if (status?.StartsWith("✗") == true) return (ThemeColors.ErrorRed,       Color.White);
            if (status?.Contains("Upgrading")    == true ||
                status?.Contains("Uninstalling") == true ||
                status?.Contains("⏳")           == true)
                                                  return (ThemeColors.WarningOrange, Color.Black);
            return (ThemeColors.ElectricBlue, Color.Black);
        }

        private void PackagesGrid_HeaderClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex != COL_SELECT) return;
            if (packagesGrid.Rows.Count == 0) return;

            bool allChecked = packagesGrid.Rows
                .Cast<DataGridViewRow>()
                .Where(r => r.Visible && !r.Cells[COL_SELECT].ReadOnly)
                .All(r => r.Cells[COL_SELECT].Value is true);

            bool newState = !allChecked;
            foreach (DataGridViewRow row in packagesGrid.Rows)
            {
                if (row.Visible && !row.Cells[COL_SELECT].ReadOnly)
                    row.Cells[COL_SELECT].Value = newState;
            }

            packagesGrid.Columns[COL_SELECT].HeaderText = newState ? "■" : "✓";
            packagesGrid.RefreshEdit();
            UpdateActionButtonEnabled();
        }

        // ── Search / filter ───────────────────────────────────────────────────

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            _searchDebounce.Stop();
            _searchDebounce.Start();
        }

        private void SearchDebounce_Tick(object sender, EventArgs e)
        {
            _searchDebounce.Stop();
            ApplySearchFilter();
        }

        private void ApplySearchFilter()
        {
            if (packagesGrid.InvokeRequired) { packagesGrid.Invoke(new Action(ApplySearchFilter)); return; }

            string term = searchBox.Text.Trim().ToLowerInvariant();
            packagesGrid.CurrentCell = null;

            foreach (DataGridViewRow row in packagesGrid.Rows)
            {
                if (string.IsNullOrEmpty(term)) { row.Visible = true; continue; }
                string name = row.Cells[COL_NAME].Value?.ToString()?.ToLowerInvariant() ?? string.Empty;
                string id   = row.Cells[COL_ID].Value?.ToString()?.ToLowerInvariant()   ?? string.Empty;
                row.Visible = name.Contains(term) || id.Contains(term);
            }

            UpdateActionButtonEnabled();
        }

        // ── Upgrade selected packages ─────────────────────────────────────────

        private async Task ExecuteSelectedUpgradesAsync()
        {
            var targets = GetSelectedTargets();
            if (targets.Count == 0)
            {
                MessageBox.Show(
                    "No packages selected.\n\nTick the ✓ column for each package to upgrade.",
                    "Nothing Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _cts     = new CancellationTokenSource();
            _isBusy  = true;
            var token = _cts.Token;

            SetControlsEnabled(false);
            SetActionEnabled(false);
            UpdateProgress(0);
            AddLogEntry($"Starting upgrade for {targets.Count} package(s)…", ThemeColors.InfoBlue);

            int  succeeded    = 0;
            int  failed       = 0;
            bool wasCancelled = false;

            try
            {
                foreach (var pkg in targets)
                {
                    if (token.IsCancellationRequested) { wasCancelled = true; break; }

                    int rowIdx = FindRowIndexById(pkg.Id);
                    if (rowIdx >= 0)
                        SetPackageStatus(rowIdx, pkg.Id, "⏳ Upgrading…", ThemeColors.WarningOrange, Color.Black);

                    AddLogEntry($"→ {pkg.Name}  [{pkg.Id}]", ThemeColors.InfoBlue);

                    bool ok = await _service.UpgradePackageAsync(pkg.Id, token, new Progress<string>(AppendRawLog));

                    if (token.IsCancellationRequested) { wasCancelled = true; break; }

                    rowIdx = FindRowIndexById(pkg.Id);
                    if (ok)
                    {
                        succeeded++;
                        if (rowIdx >= 0)
                            SetPackageStatus(rowIdx, pkg.Id, "✓ Done", ThemeColors.SuccessGreen, Color.Black);
                        AddLogEntry($"✓ {pkg.Name} — upgraded successfully.", ThemeColors.SuccessGreen);
                    }
                    else
                    {
                        failed++;
                        if (rowIdx >= 0)
                            SetPackageStatus(rowIdx, pkg.Id, "✗ Failed", ThemeColors.ErrorRed, Color.White);
                        AddLogEntry($"✗ {pkg.Name} — upgrade failed.", ThemeColors.ErrorRed);
                    }

                    UpdateProgress((int)((succeeded + failed) / (double)targets.Count * 100));
                }
            }
            catch (OperationCanceledException)
            {
                wasCancelled = true;
            }
            finally
            {
                _isBusy = false;
                _cts.Dispose();
                _cts = null;
            }

            FinishBulkOperation("upgrade", succeeded, failed, wasCancelled, targets.Count);
        }

        // ── Uninstall selected packages ───────────────────────────────────────

        private async Task ExecuteSelectedUninstallsAsync()
        {
            var targets = GetSelectedTargets();
            if (targets.Count == 0)
            {
                MessageBox.Show(
                    "No packages selected.\n\nTick the ✓ column for each package to uninstall.",
                    "Nothing Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"You are about to uninstall {targets.Count} package(s):\n\n" +
                string.Join("\n", targets.Select(t => $"  • {t.Name}")) +
                "\n\nThis action cannot be undone. Continue?",
                "Confirm Uninstall",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirm != DialogResult.Yes) return;

            _cts    = new CancellationTokenSource();
            _isBusy = true;
            var token = _cts.Token;

            SetControlsEnabled(false);
            SetActionEnabled(false);
            UpdateProgress(0);
            AddLogEntry($"Starting uninstall for {targets.Count} package(s)…", ThemeColors.WarningOrange);

            int  succeeded    = 0;
            int  failed       = 0;
            bool wasCancelled = false;

            try
            {
                foreach (var pkg in targets)
                {
                    if (token.IsCancellationRequested) { wasCancelled = true; break; }

                    int rowIdx = FindRowIndexById(pkg.Id);
                    if (rowIdx >= 0)
                        SetInstalledRowStatus(rowIdx, "⏳ Uninstalling…", ThemeColors.WarningOrange, Color.Black);

                    AddLogEntry($"→ Uninstalling {pkg.Name}  [{pkg.Id}]", ThemeColors.WarningOrange);

                    bool ok = await _service.UninstallPackageAsync(pkg.Id, token, new Progress<string>(AppendRawLog));

                    if (token.IsCancellationRequested) { wasCancelled = true; break; }

                    rowIdx = FindRowIndexById(pkg.Id);
                    if (ok)
                    {
                        succeeded++;
                        RemoveGridRowById(pkg.Id);   // removes from grid + _allPackages on the UI thread
                        AddLogEntry($"✓ {pkg.Name} — uninstalled successfully.", ThemeColors.SuccessGreen);
                    }
                    else
                    {
                        failed++;
                        if (rowIdx >= 0)
                            SetInstalledRowStatus(rowIdx, "✗ Failed", ThemeColors.ErrorRed, Color.White);
                        AddLogEntry($"✗ {pkg.Name} — uninstall failed.", ThemeColors.ErrorRed);
                    }

                    UpdateProgress((int)((succeeded + failed) / (double)targets.Count * 100));
                }
            }
            catch (OperationCanceledException)
            {
                wasCancelled = true;
            }
            finally
            {
                _isBusy = false;
                _cts.Dispose();
                _cts = null;
            }

            FinishBulkOperation("uninstall", succeeded, failed, wasCancelled, targets.Count);
        }

        // ── Shared bulk-operation helpers ─────────────────────────────────────

        private List<(string Id, string Name)> GetSelectedTargets() =>
            packagesGrid.Rows
                .Cast<DataGridViewRow>()
                .Where(r => r.Cells[COL_SELECT].Value is true && !r.Cells[COL_SELECT].ReadOnly)
                .Select(r => (
                    Id:   r.Cells[COL_ID].Value?.ToString()   ?? string.Empty,
                    Name: r.Cells[COL_NAME].Value?.ToString() ?? string.Empty))
                .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                .ToList();

        private void FinishBulkOperation(string verb, int succeeded, int failed,
                                          bool wasCancelled, int total)
        {
            if (IsDisposed) return;

            if (wasCancelled)
            {
                AddLogEntry($"{char.ToUpper(verb[0])}{verb.Substring(1)} cancelled by user.", ThemeColors.WarningOrange);
                SetControlsEnabled(true);
                SetActionEnabled(_allPackages.Count > 0);
                return;
            }

            AddLogEntry($"Session complete — {succeeded}/{total} succeeded, {failed}/{total} failed.",
                        ThemeColors.SuccessGreen);

            SetControlsEnabled(true);
            SetActionEnabled(_allPackages.Any(
                p => p.CurrentStatus.StartsWith("⬆") || p.CurrentStatus.Contains("Failed")));

            string summary = failed == 0
                ? $"All {total} package(s) {verb}ed successfully."
                : $"{succeeded} of {total} package(s) {verb}ed successfully.\n" +
                  $"{failed} package(s) failed — please review the Log Output for details.";

            MessageBox.Show(summary, $"{char.ToUpper(verb[0])}{verb.Substring(1)} Session Complete",
                MessageBoxButtons.OK,
                failed == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

            if (failed > 0 && File.Exists(_logFilePath))
                Process.Start(new ProcessStartInfo(_logFilePath) { UseShellExecute = true });
        }

        // Called via Progress<string> — always marshalled through BeginInvoke.
        private void AppendRawLog(string line)
        {
            if (!logOutput.IsHandleCreated || logOutput.IsDisposed) return;
            string formatted = $"[{DateTime.Now:HH:mm:ss}]    {line}";
            // File I/O is synchronous and must not block the UI message pump.
            Task.Run(() => WriteToLogFile(formatted));
            logOutput.BeginInvoke(new Action(() =>
            {
                logOutput.SelectionStart  = logOutput.TextLength;
                logOutput.SelectionLength = 0;
                logOutput.SelectionColor  = ThemeColors.SecondaryText;
                logOutput.AppendText(formatted + Environment.NewLine);
                logOutput.SelectionStart = logOutput.TextLength;
                logOutput.ScrollToCaret();
                TrimLogBuffer();
            }));
        }

        // Iterates ALL rows (including hidden ones) so an active search filter
        // does not cause IndexOutOfRangeException during a mid-flight operation.
        private int FindRowIndexById(string packageId)
        {
            if (string.IsNullOrWhiteSpace(packageId)) return -1;
            foreach (DataGridViewRow row in packagesGrid.Rows)
            {
                if (string.Equals(row.Cells[COL_ID].Value?.ToString(), packageId,
                        StringComparison.OrdinalIgnoreCase))
                    return row.Index;
            }
            return -1;
        }

        private void RemoveGridRowById(string packageId)
        {
            if (packagesGrid.InvokeRequired)
            {
                packagesGrid.Invoke(new Action(() => RemoveGridRowById(packageId)));
                return;
            }
            // Both mutations run on the UI thread so _allPackages and the grid stay in sync.
            int idx = FindRowIndexById(packageId);
            if (idx >= 0) packagesGrid.Rows.RemoveAt(idx);

            var match = _allPackages.FirstOrDefault(p =>
                string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase));
            if (match != null) _allPackages.Remove(match);

            UpdateActionButtonEnabled();
        }

        private void SetInstalledRowStatus(int rowIndex, string text, Color back, Color fore)
        {
            if (packagesGrid.InvokeRequired)
            {
                packagesGrid.Invoke(new Action(() => SetInstalledRowStatus(rowIndex, text, back, fore)));
                return;
            }
            if (rowIndex < 0 || rowIndex >= packagesGrid.Rows.Count) return;

            // In installed view, COL_SOURCE (slot 4) holds source — write a transient status there.
            var cell             = packagesGrid.Rows[rowIndex].Cells[COL_SOURCE];
            cell.Value           = text;
            cell.Style.BackColor = back;
            cell.Style.ForeColor = fore;
            cell.Style.Font      = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        }

        // ── UI-state helpers ──────────────────────────────────────────────────

        private void SetControlsEnabled(bool enabled)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => SetControlsEnabled(enabled))); return; }
            btnCheckUpdates.Enabled = enabled;
            btnRefresh.Enabled      = enabled;
            searchBox.Enabled       = enabled;
            packagesGrid.Enabled    = enabled;
        }

        private void SetActionEnabled(bool enabled)
        {
            if (_btnAction == null) return;
            if (_btnAction.InvokeRequired) { _btnAction.Invoke(new Action(() => SetActionEnabled(enabled))); return; }
            _btnAction.Enabled = enabled;
        }

        private void ConfigureActionButtonForView(string view)
        {
            if (_btnAction == null) return;
            if (view == "Installed Packages")
            {
                _btnAction.Text      = "🗑  Uninstall Selected";
                _btnAction.FillColor = ThemeColors.ErrorRed;
                _btnAction.ForeColor = Color.White;
            }
            else
            {
                _btnAction.Text      = "⬆  Upgrade Selected";
                _btnAction.FillColor = ThemeColors.ElectricBlue;
                _btnAction.ForeColor = Color.Black;
            }
        }

        private void EnableGridDoubleBuffering(DataGridView dgv)
        {
            typeof(DataGridView)
                .GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(dgv, true, null);
        }

        // ── Theme ─────────────────────────────────────────────────────────────

        public void ApplyTheme(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                ApplyControlTheme(ctrl);
                if (ctrl.HasChildren) ApplyTheme(ctrl);
            }
        }

        private void ApplyControlTheme(Control ctrl)
        {
            if (ctrl is Guna2Panel panel)
            {
                panel.BorderColor  = ThemeColors.BorderGray;
                panel.BorderRadius = ThemeConstants.CornerLarge;
            }
            else if (ctrl is Guna2DataGridView grid)
            {
                grid.BackgroundColor = ThemeColors.DeepCharcoal;
                grid.GridColor       = ThemeColors.BorderGray;
            }
            else if (ctrl is RichTextBox richTextBox)
            {
                richTextBox.BackColor = ThemeColors.DarkerBackground;
                richTextBox.ForeColor = ThemeColors.TerminalGreen;
            }
            else if (ctrl is Guna2TextBox textBox)
            {
                textBox.BackColor = ThemeColors.DarkCharcoal;
                textBox.ForeColor = ThemeColors.SecondaryText;
                textBox.FocusedState.BorderColor = ThemeColors.ElectricBlue;
                textBox.HoverState.BorderColor   = ThemeColors.ElectricBlue;
            }
            else if (ctrl is Label lbl)
            {
                lbl.BackColor = Color.Transparent;
                lbl.ForeColor = ThemeColors.SecondaryText;
            }
        }

        // ── Navigation ────────────────────────────────────────────────────────

        private void NavigateTo(string title, string subtitle)
        {
            lblTitle.Text    = title;
            lblSubtitle.Text = subtitle;
            _activeView      = title;
            AddLogEntry($"→ {title}", ThemeColors.InfoBlue);

            // Abort any background load still running for the previous view.
            _viewLoadCts?.Cancel();
            _viewLoadCts?.Dispose();
            _viewLoadCts = null;

            if (title == "Logs")
            {
                SwitchToLogsView();
                return;
            }

            if (title == "Settings")
            {
                SwitchToSettingsView();
                return;
            }

            SwitchToGridView();
            ConfigureActionButtonForView(title);

            if (title == "Installed Packages")
            {
                InitializeInstalledGrid();
                SetActionEnabled(false);
                _viewLoadCts = new CancellationTokenSource();
                _ = LoadInstalledPackagesAsync(_viewLoadCts.Token);
            }
            else if (title == "Available Updates" || title == "Dashboard")
            {
                InitializeUpdatesGrid();
                if (_allPackages.Count > 0)
                    PopulateGrid(_allPackages);
                SetActionEnabled(_allPackages.Any(
                    p => p.CurrentStatus.StartsWith("⬆") || p.CurrentStatus.Contains("Failed")));
            }
        }

        private void SwitchToLogsView()
        {
            if (_settingsPanel != null)
            {
                mainContentPanel.Controls.Remove(_settingsPanel);
                _settingsPanel.Dispose();
                _settingsPanel = null;
            }
            searchBox.Visible         = false;
            packagesGrid.Visible      = false;
            lblProgressLabel.Visible  = false;
            updateProgressBar.Visible = false;
            overallProgress.Visible   = false;
            if (_btnAction != null) _btnAction.Visible = false;

            lblLogOutput.Location = new Point(12, 80);
            logOutput.Anchor      = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            logOutput.Location    = new Point(12, 105);
            logOutput.Size        = new Size(
                mainContentPanel.ClientSize.Width  - 24,
                mainContentPanel.ClientSize.Height - 105 - 12);
        }

        private void SwitchToGridView()
        {
            if (_settingsPanel != null)
            {
                mainContentPanel.Controls.Remove(_settingsPanel);
                _settingsPanel.Dispose();
                _settingsPanel = null;
            }
            searchBox.Visible         = true;
            packagesGrid.Visible      = true;
            lblProgressLabel.Visible  = true;
            updateProgressBar.Visible = true;
            overallProgress.Visible   = true;
            if (_btnAction != null) _btnAction.Visible = true;

            lblLogOutput.Location = new Point(12, 454);
            logOutput.Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            logOutput.Location    = new Point(12, 472);
            logOutput.Size        = new Size(mainContentPanel.ClientSize.Width - 24, 104);
        }

        private void SwitchToSettingsView()
        {
            searchBox.Visible         = false;
            packagesGrid.Visible      = false;
            lblProgressLabel.Visible  = false;
            updateProgressBar.Visible = false;
            overallProgress.Visible   = false;
            if (_btnAction != null) _btnAction.Visible = false;

            lblLogOutput.Location = new Point(12, 454);
            logOutput.Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            logOutput.Location    = new Point(12, 472);
            logOutput.Size        = new Size(mainContentPanel.ClientSize.Width - 24, 104);

            if (_settingsPanel == null) BuildSettingsPanel();
            _settingsPanel.Size    = new Size(mainContentPanel.ClientSize.Width - 24, 360);
            _settingsPanel.Visible = true;
        }

        private void BuildSettingsPanel()
        {
            _settingsPanel = new Panel
            {
                Location  = new Point(12, 80),
                BackColor = Color.Transparent,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            int y = 0;

            _settingsPanel.Controls.Add(new Label
            {
                Text      = "Upgrade Options",
                Font      = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = ThemeColors.PrimaryText,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(0, y)
            });
            y += 28;

            _settingsPanel.Controls.Add(new Label
            {
                BackColor = Color.FromArgb(50, 50, 50),
                Location  = new Point(0, y),
                Size      = new Size(540, 1)
            });
            y += 12;

            var chkSilent = BuildCheckBox(
                "Silent mode  (--silent) — suppress installer UI during upgrades",
                AppSettings.SilentMode, y);
            chkSilent.CheckedChanged += (s, e) => AppSettings.SilentMode = chkSilent.Checked;
            _settingsPanel.Controls.Add(chkSilent);
            y += 34;

            var chkForce = BuildCheckBox(
                "Force install  (--force) — override version locks and pre-checks",
                AppSettings.ForceInstall, y);
            chkForce.CheckedChanged += (s, e) => AppSettings.ForceInstall = chkForce.Checked;
            _settingsPanel.Controls.Add(chkForce);
            y += 34;

            var chkAgreements = BuildCheckBox(
                "Accept agreements automatically  (--accept-package-agreements  --accept-source-agreements)",
                AppSettings.AcceptAgreements, y);
            chkAgreements.CheckedChanged += (s, e) => AppSettings.AcceptAgreements = chkAgreements.Checked;
            _settingsPanel.Controls.Add(chkAgreements);
            y += 50;

            _settingsPanel.Controls.Add(new Label
            {
                Text      = "Maintenance",
                Font      = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = ThemeColors.PrimaryText,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(0, y)
            });
            y += 28;

            _settingsPanel.Controls.Add(new Label
            {
                BackColor = Color.FromArgb(50, 50, 50),
                Location  = new Point(0, y),
                Size      = new Size(540, 1)
            });
            y += 12;

            var btnResetCache = BuildSettingsButton("🔄  Reset Winget Cache", new Point(0, y));
            btnResetCache.Click += async (s, e) =>
            {
                btnResetCache.Enabled = false;
                AddLogEntry("Resetting winget sources…", ThemeColors.WarningOrange);
                bool ok = await _service.ResetSourcesAsync(CancellationToken.None, new Progress<string>(AppendRawLog));
                if (!IsDisposed)
                {
                    AddLogEntry(ok ? "✓ Sources reset successfully." : "✗ Source reset failed.",
                                ok ? ThemeColors.SuccessGreen : ThemeColors.ErrorRed);
                    btnResetCache.Enabled = true;
                }
            };
            _settingsPanel.Controls.Add(btnResetCache);

            var btnClearLog = BuildSettingsButton("🧹  Clear Log History", new Point(232, y));
            btnClearLog.Click += (s, e) =>
            {
                lock (_logFileLock)
                {
                    if (File.Exists(_logFilePath)) File.Delete(_logFilePath);
                }
                // ReadOnly is briefly lifted — same pattern as TrimLogBuffer.
                logOutput.ReadOnly = false;
                logOutput.Clear();
                logOutput.ReadOnly = true;
                AddLogEntry("Log history cleared.", ThemeColors.InfoBlue);
            };
            _settingsPanel.Controls.Add(btnClearLog);

            mainContentPanel.Controls.Add(_settingsPanel);
        }

        private static Guna2CheckBox BuildCheckBox(string text, bool initialValue, int y)
        {
            var chk = new Guna2CheckBox
            {
                Text      = text,
                ForeColor = ThemeColors.SecondaryText,
                Font      = new Font("Segoe UI", 9.5F),
                Checked   = initialValue,
                AutoSize  = true,
                Location  = new Point(0, y)
            };
            chk.CheckedState.FillColor     = ThemeColors.ElectricBlue;
            chk.CheckedState.BorderColor   = ThemeColors.ElectricBlue;
            chk.UncheckedState.BorderColor = Color.FromArgb(80, 80, 80);
            return chk;
        }

        private static Guna2Button BuildSettingsButton(string text, Point location)
        {
            var btn = new Guna2Button
            {
                Text            = text,
                FillColor       = ThemeColors.DarkCharcoal,
                ForeColor       = ThemeColors.SecondaryText,
                BorderColor     = Color.FromArgb(60, 60, 60),
                BorderThickness = 1,
                BorderRadius    = ThemeConstants.CornerMedium,
                Font            = new Font("Segoe UI", 9F),
                Size            = new Size(220, 34),
                Location        = location
            };
            btn.DisabledState.FillColor = Color.FromArgb(48, 48, 48);
            btn.DisabledState.ForeColor = Color.FromArgb(90, 90, 90);
            return btn;
        }

        private void HighlightNavButton(Guna2Button active)
        {
            foreach (var btn in new[] { navDashboard, navInstalled, navUpdates, navSettings, navLogs })
            {
                btn.FillColor = Color.Transparent;
                btn.ForeColor = btn == navLogs ? Color.FromArgb(80, 140, 255) : ThemeColors.SecondaryText;
            }
            if (active != navLogs)
            {
                active.FillColor = ThemeColors.HoverBlue;
                active.ForeColor = ThemeColors.ElectricBlue;
            }
        }

        // ── Sidebar ───────────────────────────────────────────────────────────

        private void CollapseSidebar()
        {
            _sidebarCollapsed  = true;
            sidebarPanel.Width = SIDEBAR_COLLAPSED;

            foreach (var btn in new[] { navDashboard, navInstalled, navUpdates, navSettings, navLogs })
            {
                int sp = btn.Text.IndexOf(' ');
                btn.Text = sp > 0 ? btn.Text.Substring(0, sp) : btn.Text;
                btn.Size = new Size(SIDEBAR_COLLAPSED - ThemeConstants.Spacing8,
                                    ThemeConstants.DataGridRowHeight);
            }
        }

        private void ExpandSidebar()
        {
            _sidebarCollapsed  = false;
            sidebarPanel.Width = SIDEBAR_EXPANDED;

            var labels = new[]
            {
                ("📊  Dashboard", navDashboard),
                ("📦  Installed", navInstalled),
                ("⬆️  Updates",   navUpdates),
                ("⚙️  Settings",  navSettings),
                ("🐛  Errors Report", navLogs)
            };

            foreach (var (text, btn) in labels)
            {
                btn.Text = text;
                btn.Size = new Size(SIDEBAR_EXPANDED - ThemeConstants.Spacing16,
                                    ThemeConstants.DataGridRowHeight);
            }
        }

        // ── Log & progress ────────────────────────────────────────────────────

        private void TrimLogBuffer()
        {
            if (logOutput.IsDisposed || !logOutput.IsHandleCreated) return;
            if (logOutput.Lines.Length <= LOG_MAX_LINES) return;

            int linesToRemove = logOutput.Lines.Length - LOG_KEEP_LINES;
            int charIndex     = logOutput.GetFirstCharIndexFromLine(linesToRemove);
            if (charIndex <= 0) return;

            // ReadOnly is briefly lifted to delete old lines while preserving RTF colour data.
            logOutput.ReadOnly = false;
            logOutput.Select(0, charIndex);
            logOutput.SelectedText = string.Empty;
            logOutput.ReadOnly = true;

            logOutput.SelectionStart = logOutput.TextLength;
            logOutput.ScrollToCaret();
        }

        private static void WriteToLogFile(string line)
        {
            try
            {
                lock (_logFileLock)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath));

                    var info = new FileInfo(_logFilePath);
                    if (info.Exists && info.Length > 2 * 1024 * 1024)
                    {
                        string bak = _logFilePath + ".bak";
                        if (File.Exists(bak)) File.Delete(bak);
                        File.Move(_logFilePath, bak);
                    }

                    File.AppendAllText(_logFilePath, line + Environment.NewLine, System.Text.Encoding.UTF8);
                }
            }
            catch { }
        }

        public void AddLogEntry(string message, Color color)
        {
            if (logOutput.InvokeRequired) { logOutput.BeginInvoke(new Action(() => AddLogEntry(message, color))); return; }

            string formatted = $"[{DateTime.Now:HH:mm:ss}] {message}";
            logOutput.SelectionStart  = logOutput.TextLength;
            logOutput.SelectionLength = 0;
            logOutput.SelectionColor  = color;
            logOutput.AppendText(formatted + Environment.NewLine);
            logOutput.SelectionStart = logOutput.TextLength;
            logOutput.ScrollToCaret();
            TrimLogBuffer();
            WriteToLogFile(formatted);
        }

        public void UpdateProgress(int value)
        {
            if (updateProgressBar.InvokeRequired) { updateProgressBar.Invoke(new Action(() => UpdateProgress(value))); return; }
            int v = Math.Max(0, Math.Min(value, 100));
            updateProgressBar.Value = v;
            overallProgress.Value   = v;
            overallProgress.Text    = $"{v}%";
        }
    }
}
