using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace frm_winget_upgrade
{
    public sealed class ReleaseNotes
    {
        public string Notes    { get; set; } = string.Empty;
        public string Url      { get; set; } = string.Empty;
        public string Homepage { get; set; } = string.Empty;
    }

    public sealed class WingetPackage
    {
        public string Name             { get; set; } = string.Empty;
        public string Id               { get; set; } = string.Empty;
        public string InstalledVersion { get; set; } = string.Empty;
        public string AvailableVersion { get; set; } = string.Empty;
        public string Source           { get; set; } = string.Empty;

        // Non-empty when the package Id names a fixed version line (e.g. OpenJS.NodeJS.22) —
        // its updates stay within that line, so it's excluded from "Select All" by default.
        public string Track { get; set; } = string.Empty;

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

        // Id ends in a numeric version segment (OpenJS.NodeJS.22, Python.Python.3.12) —
        // winget already confines that package's upgrades to the same line.
        private static readonly Regex VersionLockedId = new Regex(
            @"\.\d+(\.\d+){0,2}$", RegexOptions.Compiled);

        private const int FallbackRecheckConcurrency = 6;

        // Edge components are serviced by Windows Update, not winget — attempting to upgrade
        // them through winget consistently fails (e.g. "REST API endpoint not found"), so
        // they're filtered out of results entirely rather than offered and then failing.
        private static bool IsEdgeComponent(string name, string id) =>
            name.IndexOf("Microsoft Edge", StringComparison.OrdinalIgnoreCase) >= 0 ||
            id.IndexOf("Microsoft.Edge",   StringComparison.OrdinalIgnoreCase) >= 0 ||
            id.IndexOf("MicrosoftEdge",    StringComparison.OrdinalIgnoreCase) >= 0;

        // Word-bounded so it catches "Chrome Beta" / "Google.Chrome.Beta" without matching
        // substrings inside unrelated words (e.g. "Developer", "Preview Pane").
        private static readonly Regex PreReleaseChannel = new Regex(
            @"(?<![A-Za-z])(beta|dev|canary|nightly|insider|alpha|preview)(?![A-Za-z])",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static bool IsPreReleaseChannel(string name, string id) =>
            PreReleaseChannel.IsMatch(name) || PreReleaseChannel.IsMatch(id);

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

        // ── Release notes ─────────────────────────────────────────────────────

        // Parses the English field labels of `winget show`; a localized winget yields empty notes.
        public async Task<ReleaseNotes> GetReleaseNotesAsync(string id)
        {
            string raw   = await RunCommandAsync($"show --id \"{id}\" --exact --accept-source-agreements");
            var    info  = new ReleaseNotes();
            var    lines = raw.Replace("\r", string.Empty).Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith("Release Notes Url:"))  info.Url      = line.Substring("Release Notes Url:".Length).Trim();
                else if (line.StartsWith("Homepage:"))      info.Homepage = line.Substring("Homepage:".Length).Trim();
                else if (line.StartsWith("Release Notes:"))
                {
                    var sb = new StringBuilder(line.Substring("Release Notes:".Length).Trim());
                    while (i + 1 < lines.Length &&
                           (lines[i + 1].StartsWith(" ") ||
                            (lines[i + 1].Length == 0 && i + 2 < lines.Length && lines[i + 2].StartsWith(" "))))
                    {
                        i++;
                        if (sb.Length > 0) sb.AppendLine();
                        sb.Append(lines[i].Trim());
                    }
                    info.Notes = sb.ToString().Trim();
                }
            }
            return info;
        }

        // ── Available updates ─────────────────────────────────────────────────

        public async Task<List<WingetPackage>> GetAvailableUpdatesAsync(CancellationToken cancellationToken = default,
                                                                          IProgress<string> progress = null)
        {
            string raw     = await RunCommandAsync("upgrade --accept-source-agreements");
            var    updates = ParseUpgradeOutput(raw, cancellationToken);

            // msstore updates are sometimes dropped from the combined multi-source scan above —
            // query the source explicitly and merge in anything not already found.
            string rawMsStore     = await RunCommandAsync("upgrade --source msstore --accept-source-agreements");
            var    msStoreUpdates = ParseUpgradeOutput(rawMsStore, cancellationToken);

            var seenIds = new HashSet<string>(updates.Select(p => p.Id), StringComparer.OrdinalIgnoreCase);
            foreach (var pkg in msStoreUpdates)
            {
                if (seenIds.Contains(pkg.Id)) continue;
                pkg.Source = "msstore";
                updates.Add(pkg);
                seenIds.Add(pkg.Id);
            }

            await RecheckMissedUpdatesAsync(updates, seenIds, progress, cancellationToken);

            updates.RemoveAll(p => ExcludedPackages.IsExcluded(p.Id));

            foreach (var pkg in updates)
                pkg.Track = VersionLockedId.IsMatch(pkg.Id) ? "Version-locked" : string.Empty;

            return updates;
        }

        // The bulk "upgrade" scan silently drops a package when winget's own name-matching
        // finds several candidates for it (e.g. multiple "qBittorrent" listings in the source) —
        // it doesn't error, it just omits the row. Re-querying that exact Id alone sidesteps
        // the ambiguity and recovers the update if one actually exists. This also covers
        // packages winget only knows locally through their ARP registry key (no dotted winget
        // Id, Source blank) — e.g. qBittorrent installed by its own installer rather than
        // winget — where the bulk scan's internal catalog match can fail silently the same way;
        // those have no queryable Id, so they're re-queried by display name instead.
        private async Task RecheckMissedUpdatesAsync(List<WingetPackage> updates, HashSet<string> seenIds,
                                                       IProgress<string> progress, CancellationToken cancellationToken)
        {
            string rawInstalled = await RunCommandAsync("list --accept-source-agreements");
            var    installed    = ParseInstalledOutput(rawInstalled, cancellationToken);

            var toRecheck = installed
                .Where(p => !seenIds.Contains(p.Id) && !ExcludedPackages.IsExcluded(p.Id) &&
                            !string.IsNullOrWhiteSpace(p.Name) &&
                            !IsEdgeComponent(p.Name, p.Id) &&
                            (AppSettings.IncludeBetaVersions || !IsPreReleaseChannel(p.Name, p.Id)))
                .ToList();

            if (toRecheck.Count == 0) return;

            progress?.Report($"Deep-checking {toRecheck.Count} package(s) individually — this can take a bit…");

            using (var gate = new SemaphoreSlim(FallbackRecheckConcurrency))
            {
                var tasks = toRecheck.Select(async pkg =>
                {
                    await gate.WaitAsync(cancellationToken);
                    try
                    {
                        string query = pkg.Source != "Local Registry"
                            ? $"list --id \"{pkg.Id}\" --exact --accept-source-agreements"
                            : $"list \"{pkg.Name}\" --accept-source-agreements";
                        string raw = await RunCommandAsync(query);
                        return ParseUpgradeOutput(raw, cancellationToken);
                    }
                    finally
                    {
                        gate.Release();
                    }
                });

                var foundLists = await Task.WhenAll(tasks);

                foreach (var pkg in foundLists.SelectMany(list => list))
                {
                    if (!seenIds.Add(pkg.Id)) continue;
                    updates.Add(pkg);
                }
            }
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
                string source    = (posSource > 0 && line.Length > posSource)
                    ? Slice(line, posSource, line.Length)
                    : string.Empty;

                if (string.IsNullOrWhiteSpace(id)) continue;
                if (IsEdgeComponent(name.Trim(), id.Trim())) continue;
                if (!AppSettings.IncludeBetaVersions && IsPreReleaseChannel(name.Trim(), id.Trim())) continue;

                result.Add(new WingetPackage
                {
                    Name             = name.Trim(),
                    Id               = id.Trim(),
                    InstalledVersion = version.Trim(),
                    AvailableVersion = available.Trim(),
                    Source           = source.Trim()
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

        // winget's own catalog can list two distinct locally-installed products under the
        // same Id (e.g. "Google Chrome" and "Google Chrome Beta" both as
        // Google.Chrome.Beta.EXE) — it then refuses to act, asking for `--version` to say
        // which installed instance is meant. Pinning to the row's own installed version
        // disambiguates it automatically instead of leaving the user to retry by hand.
        private static readonly Regex MultipleVersionsError = new Regex(
            "Multiple versions of this package are installed", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // ── Upgrade ───────────────────────────────────────────────────────────

        public async Task<bool> UpgradePackageAsync(string packageId,
                                                     string source,
                                                     string installedVersion,
                                                     CancellationToken cancellationToken = default,
                                                     IProgress<string> progress = null)
        {
            var args = new StringBuilder($"upgrade --id \"{packageId}\"");
            if (IsMsStoreSource(source))      args.Append(" --source msstore");
            if (AppSettings.SilentMode)       args.Append(" --silent");
            if (AppSettings.ForceInstall)     args.Append(" --force");
            if (AppSettings.AcceptAgreements) args.Append(" --accept-package-agreements --accept-source-agreements");

            var (ok, ambiguous) = await RunWingetOperationAsync(args.ToString(), cancellationToken, progress);
            if (ok || !ambiguous || string.IsNullOrWhiteSpace(installedVersion)) return ok;

            progress?.Report($"Multiple installed versions matched — retrying with --version \"{installedVersion}\"…");
            args.Append($" --version \"{installedVersion}\"");
            var (retryOk, _) = await RunWingetOperationAsync(args.ToString(), cancellationToken, progress);
            return retryOk;
        }

        private static bool IsMsStoreSource(string source) =>
            string.Equals(source, "msstore", StringComparison.OrdinalIgnoreCase);

        // ── Uninstall ─────────────────────────────────────────────────────────

        public async Task<bool> UninstallPackageAsync(string packageId,
                                                       string installedVersion,
                                                       CancellationToken cancellationToken = default,
                                                       IProgress<string> progress = null)
        {
            var args = new StringBuilder($"uninstall --id \"{packageId}\"");
            if (AppSettings.SilentMode)       args.Append(" --silent");
            if (AppSettings.ForceInstall)     args.Append(" --force");
            if (AppSettings.AcceptAgreements) args.Append(" --accept-source-agreements");

            var (ok, ambiguous) = await RunWingetOperationAsync(args.ToString(), cancellationToken, progress);
            if (ok || !ambiguous || string.IsNullOrWhiteSpace(installedVersion)) return ok;

            progress?.Report($"Multiple installed versions matched — retrying with --version \"{installedVersion}\"…");
            args.Append($" --version \"{installedVersion}\"");
            var (retryOk, _) = await RunWingetOperationAsync(args.ToString(), cancellationToken, progress);
            return retryOk;
        }

        // ── Source reset ──────────────────────────────────────────────────────

        public async Task<bool> ResetSourcesAsync(CancellationToken cancellationToken = default,
                                                   IProgress<string> progress = null)
        {
            var (ok, _) = await RunWingetOperationAsync("source reset --force --accept-source-agreements", cancellationToken, progress);
            return ok;
        }

        // ── Shared streamed operation runner ──────────────────────────────────

        private async Task<(bool Success, bool AmbiguousVersions)> RunWingetOperationAsync(
            string arguments, CancellationToken cancellationToken, IProgress<string> progress)
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
                        if (proc == null) return (false, false);

                        bool   textIndicatesFailure = false;
                        bool   ambiguousVersions     = false;
                        string line;

                        while ((line = proc.StandardOutput.ReadLine()) != null)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                try { proc.Kill(); } catch { }
                                return (false, false);
                            }

                            string cleaned = AnsiEscape.Replace(line, string.Empty).Trim();
                            if (string.IsNullOrEmpty(cleaned)) continue;

                            if (cleaned.IndexOf("failed",                      StringComparison.OrdinalIgnoreCase) >= 0 ||
                                cleaned.IndexOf("error",                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                                cleaned.IndexOf("different install technology", StringComparison.OrdinalIgnoreCase) >= 0)
                                textIndicatesFailure = true;

                            if (MultipleVersionsError.IsMatch(cleaned))
                                ambiguousVersions = true;

                            progress?.Report(cleaned);
                        }

                        proc.WaitForExit();

                        if (cancellationToken.IsCancellationRequested) return (false, false);

                        // Exit code 3010 = success, reboot required (Windows Installer standard).
                        bool success = !textIndicatesFailure && (proc.ExitCode == 0 || proc.ExitCode == 3010);
                        return (success, ambiguousVersions);
                    }
                }
                catch (Exception ex)
                {
                    progress?.Report($"[ERROR] {ex.Message}");
                    return (false, false);
                }
            }, cancellationToken);
        }
    }
}
