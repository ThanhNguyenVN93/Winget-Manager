using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace frm_winget_upgrade
{
    public sealed class WingetPackage
    {
        public string Name             { get; set; } = string.Empty;
        public string Id               { get; set; } = string.Empty;
        public string InstalledVersion { get; set; } = string.Empty;
        public string AvailableVersion { get; set; } = string.Empty;
        public string Source           { get; set; } = string.Empty;

        // Mutable runtime status — kept in sync with the grid cell so that
        // row-visibility filtering never loses live state mid-upgrade.
        public string CurrentStatus { get; set; } = "⬆ Update Available";
    }

    public sealed class WingetService
    {
        private const string Exe = "winget";

        private static readonly Regex AnsiEscape = new Regex(
            @"\x1B(\[[0-9;]*[A-Za-z]|\(B)", RegexOptions.Compiled);

        private static readonly Regex FooterLine = new Regex(
            @"^\s*\d+\s+(package|upgrade)s?\s+(found|available)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Word-boundary guard — prevents matching "Id" inside "Identifier" or "winget".
        private static readonly Regex WordId = new Regex(
            @"(?<![A-Za-z])Id(?![A-Za-z])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Raw GUIDs (local registry artefacts) — no Publisher.PackageName dot-structure.
        private static readonly Regex GuidId = new Regex(
            @"^\{?[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\}?$",
            RegexOptions.Compiled);

        // ── Raw command runner ────────────────────────────────────────────────

        public async Task<string> RunCommandAsync(string arguments)
        {
            return await Task.Run(() =>
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = Exe,
                    Arguments              = arguments,
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    CreateNoWindow         = true,
                    StandardOutputEncoding = new UTF8Encoding(false, false),
                    StandardErrorEncoding  = new UTF8Encoding(false, false)
                };

                var sb = new StringBuilder(8192);
                try
                {
                    using (var proc = Process.Start(psi))
                    {
                        if (proc == null) return "[ERROR] Process.Start returned null.";
                        sb.Append(proc.StandardOutput.ReadToEnd());
                        proc.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    return $"[ERROR] Cannot launch winget: {ex.Message}";
                }
                return sb.ToString();
            });
        }

        // ── Available updates ─────────────────────────────────────────────────

        public async Task<List<WingetPackage>> GetAvailableUpdatesAsync(CancellationToken cancellationToken = default)
        {
            string raw = await RunCommandAsync("upgrade --accept-source-agreements");
            return ParseUpgradeOutput(raw, cancellationToken);
        }

        private List<WingetPackage> ParseUpgradeOutput(string raw, CancellationToken cancellationToken = default)
        {
            var result = new List<WingetPackage>();
            if (string.IsNullOrWhiteSpace(raw)) return result;

            string   clean = AnsiEscape.Replace(raw, string.Empty);
            string[] lines = SplitLines(clean);

            int headerIdx = FindHeaderLine(lines, requireAvailable: true);
            if (headerIdx < 0) return result;

            int sepIdx = FindSeparatorAfterHeader(lines, headerIdx);

            int posName, posId, posVersion, posAvailable, posSource;
            ResolveColumns(lines, headerIdx, sepIdx,
                out posName, out posId, out posVersion, out posAvailable, out posSource);

            if (posName < 0 || posId < 0 || posVersion < 0 || posAvailable < 0) return result;

            int dataStart = sepIdx >= 0 ? sepIdx + 1 : headerIdx + 1;
            for (int i = dataStart; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string line = lines[i];
                if (FooterLine.IsMatch(line)) break;
                if (IsSeparatorLine(line)) continue;
                if (line.Length <= posAvailable) continue;

                string name      = Slice(line, posName,      posId);
                string id        = Slice(line, posId,         posVersion);
                string version   = Slice(line, posVersion,    posAvailable);
                string available = posSource > 0
                    ? Slice(line, posAvailable, posSource)
                    : Slice(line, posAvailable, line.Length);

                if (string.IsNullOrWhiteSpace(id)) continue;
                if (string.Equals(id.Trim(), "Microsoft.Edge", StringComparison.OrdinalIgnoreCase)) continue;

                result.Add(new WingetPackage
                {
                    Name             = name.Trim(),
                    Id               = id.Trim(),
                    InstalledVersion = version.Trim(),
                    AvailableVersion = available.Trim()
                });
            }

            return result;
        }

        // ── Installed packages ────────────────────────────────────────────────

        public async Task<List<WingetPackage>> GetInstalledPackagesAsync(CancellationToken cancellationToken = default)
        {
            string raw = await RunCommandAsync("list --accept-source-agreements");
            return ParseInstalledOutput(raw, cancellationToken);
        }

        private List<WingetPackage> ParseInstalledOutput(string raw, CancellationToken cancellationToken = default)
        {
            var result = new List<WingetPackage>();
            if (string.IsNullOrWhiteSpace(raw)) return result;

            string   clean = AnsiEscape.Replace(raw, string.Empty);
            string[] lines = SplitLines(clean);

            int headerIdx = FindHeaderLine(lines, requireAvailable: false);
            if (headerIdx < 0) return result;

            int sepIdx = FindSeparatorAfterHeader(lines, headerIdx);

            int posName, posId, posVersion, posAvailable, posSource;
            ResolveColumns(lines, headerIdx, sepIdx,
                out posName, out posId, out posVersion, out posAvailable, out posSource);

            if (posName < 0 || posId < 0 || posVersion < 0) return result;

            int dataStart = sepIdx >= 0 ? sepIdx + 1 : headerIdx + 1;
            for (int i = dataStart; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string line = lines[i];
                if (FooterLine.IsMatch(line)) break;
                if (IsSeparatorLine(line)) continue;
                if (line.Length <= posId) continue;

                string name = Slice(line, posName, posId);
                string id   = Slice(line, posId,   posVersion);

                // posSource is the last column; guard against lines shorter than its start
                // (packages with no Source entry end abruptly after the Version value).
                int    versionEnd = (posSource > 0 && posSource < line.Length) ? posSource : line.Length;
                string version    = Slice(line, posVersion, versionEnd);
                string source     = (posSource > 0 && line.Length > posSource)
                    ? Slice(line, posSource, line.Length)
                    : string.Empty;

                string idTrimmed = id.Trim();
                if (string.IsNullOrWhiteSpace(idTrimmed)) continue;

                // A long app name can bleed into the ID column span. Real winget IDs never
                // contain spaces, so take the last space-delimited token when this happens.
                if (idTrimmed.IndexOf(' ') >= 0)
                    idTrimmed = idTrimmed.Split(' ')[idTrimmed.Split(' ').Length - 1];

                if (string.IsNullOrWhiteSpace(idTrimmed)) continue;

                // Local registry artefact — GUID or no dot in ID
                bool isLocalRegistry = GuidId.IsMatch(idTrimmed) ||
                                       idTrimmed.IndexOf('.') < 0 ||
                                       string.IsNullOrWhiteSpace(source.Trim());

                result.Add(new WingetPackage
                {
                    Name             = name.Trim(),
                    Id               = idTrimmed,
                    InstalledVersion = version.Trim(),
                    AvailableVersion = string.Empty,
                    Source           = isLocalRegistry ? "Local Registry" : source.Trim(),
                    CurrentStatus    = isLocalRegistry ? "Local Registry" : "Installed"
                });
            }

            return result;
        }

        // ── Shared column-detection helpers ───────────────────────────────────

        private static string[] SplitLines(string text) =>
            text.Replace("\r\n", "\n").Replace("\r", "\n")
                .Split(new[] { '\n' }, StringSplitOptions.None);

        private static int FindHeaderLine(string[] lines, bool requireAvailable)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                string l = lines[i];
                bool hasName    = l.IndexOf("Name",    StringComparison.OrdinalIgnoreCase) >= 0;
                bool hasVersion = l.IndexOf("Version", StringComparison.OrdinalIgnoreCase) >= 0;
                bool hasId      = WordId.IsMatch(l);
                bool hasAvail   = l.IndexOf("Available", StringComparison.OrdinalIgnoreCase) >= 0;

                if (hasName && hasVersion && hasId && (!requireAvailable || hasAvail))
                    return i;
            }
            return -1;
        }

        private static int FindSeparatorAfterHeader(string[] lines, int headerIdx)
        {
            for (int i = headerIdx + 1; i < lines.Length; i++)
            {
                if (IsSeparatorLine(lines[i])) return i;

                string l = lines[i];
                if (l.IndexOf("Version", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    WordId.IsMatch(l))
                    break;
            }
            return -1;
        }

        private static void ResolveColumns(string[] lines, int headerIdx, int sepIdx,
            out int posName, out int posId, out int posVersion,
            out int posAvailable, out int posSource)
        {
            if (sepIdx >= 0)
            {
                var spans = GetSeparatorSpans(lines[sepIdx]);
                if (spans.Count >= 3)
                {
                    posName      = spans[0].Start;
                    posId        = spans[1].Start;
                    posVersion   = spans[2].Start;
                    posAvailable = spans.Count > 3 ? spans[3].Start : -1;
                    posSource    = spans.Count > 4 ? spans[4].Start : -1;
                    return;
                }
            }
            GetHeaderPositions(lines[headerIdx],
                out posName, out posId, out posVersion, out posAvailable, out posSource);
        }

        private static List<(int Start, int End)> GetSeparatorSpans(string line)
        {
            var spans = new List<(int Start, int End)>();
            int i = 0;
            while (i < line.Length)
            {
                while (i < line.Length && !IsSeparatorChar(line[i])) i++;
                if (i >= line.Length) break;
                int start = i;
                while (i < line.Length && IsSeparatorChar(line[i])) i++;
                spans.Add((start, i));
            }
            return spans;
        }

        private static bool IsSeparatorChar(char c) =>
            c == '-' || c == '─' || c == '━' || c == '╌' || c == '╍';

        private static void GetHeaderPositions(string header,
            out int posName, out int posId, out int posVersion,
            out int posAvailable, out int posSource)
        {
            posName      = header.IndexOf("Name",      StringComparison.OrdinalIgnoreCase);
            posVersion   = header.IndexOf("Version",   StringComparison.OrdinalIgnoreCase);
            posAvailable = header.IndexOf("Available", StringComparison.OrdinalIgnoreCase);
            posSource    = header.IndexOf("Source",    StringComparison.OrdinalIgnoreCase);

            var m = WordId.Match(header);
            posId = m.Success ? m.Index : -1;
        }

        // Requires ≥10 separator chars to avoid false-positives from winget's progress spinner ("-").
        private static bool IsSeparatorLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            int dashCount = 0;
            foreach (char c in line)
            {
                if (c == ' ' || c == '\t') continue;
                if (!IsSeparatorChar(c)) return false;
                dashCount++;
            }
            return dashCount >= 10;
        }

        private static string Slice(string s, int start, int end)
        {
            if (start < 0 || start >= s.Length) return string.Empty;
            int len = Math.Min(end, s.Length) - start;
            return len > 0 ? s.Substring(start, len) : string.Empty;
        }

        // ── Upgrade ───────────────────────────────────────────────────────────

        public async Task<bool> UpgradePackageAsync(string packageId,
                                                     CancellationToken cancellationToken = default,
                                                     IProgress<string> progress = null)
        {
            var args = new StringBuilder($"upgrade --id \"{packageId}\"");
            if (AppSettings.SilentMode)       args.Append(" --silent");
            if (AppSettings.ForceInstall)     args.Append(" --force");
            if (AppSettings.AcceptAgreements) args.Append(" --accept-package-agreements --accept-source-agreements");
            return await RunWingetOperationAsync(args.ToString(), cancellationToken, progress);
        }

        // ── Uninstall ─────────────────────────────────────────────────────────

        public async Task<bool> UninstallPackageAsync(string packageId,
                                                       CancellationToken cancellationToken = default,
                                                       IProgress<string> progress = null)
        {
            var args = new StringBuilder($"uninstall --id \"{packageId}\"");
            if (AppSettings.SilentMode)       args.Append(" --silent");
            if (AppSettings.ForceInstall)     args.Append(" --force");
            if (AppSettings.AcceptAgreements) args.Append(" --accept-source-agreements");
            return await RunWingetOperationAsync(args.ToString(), cancellationToken, progress);
        }

        // ── Source reset ──────────────────────────────────────────────────────

        public async Task<bool> ResetSourcesAsync(CancellationToken cancellationToken = default,
                                                   IProgress<string> progress = null)
            => await RunWingetOperationAsync("source reset --force --accept-source-agreements", cancellationToken, progress);

        // ── Shared streamed operation runner ──────────────────────────────────

        private async Task<bool> RunWingetOperationAsync(string arguments,
                                                          CancellationToken cancellationToken,
                                                          IProgress<string> progress)
        {
            return await Task.Run(() =>
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = Exe,
                    Arguments              = arguments,
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    CreateNoWindow         = true,
                    StandardOutputEncoding = new UTF8Encoding(false, false)
                };

                try
                {
                    using (var proc = Process.Start(psi))
                    {
                        if (proc == null) return false;

                        bool   textIndicatesFailure = false;
                        string line;

                        while ((line = proc.StandardOutput.ReadLine()) != null)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                try { proc.Kill(); } catch { }
                                return false;
                            }

                            string cleaned = AnsiEscape.Replace(line, string.Empty).Trim();
                            if (string.IsNullOrEmpty(cleaned)) continue;

                            if (cleaned.IndexOf("failed",                      StringComparison.OrdinalIgnoreCase) >= 0 ||
                                cleaned.IndexOf("error",                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                                cleaned.IndexOf("different install technology", StringComparison.OrdinalIgnoreCase) >= 0)
                                textIndicatesFailure = true;

                            progress?.Report(cleaned);
                        }

                        proc.WaitForExit();

                        if (cancellationToken.IsCancellationRequested) return false;

                        // Exit code 3010 = success, reboot required (Windows Installer standard).
                        return !textIndicatesFailure && (proc.ExitCode == 0 || proc.ExitCode == 3010);
                    }
                }
                catch (Exception ex)
                {
                    progress?.Report($"[ERROR] {ex.Message}");
                    return false;
                }
            }, cancellationToken);
        }
    }
}
