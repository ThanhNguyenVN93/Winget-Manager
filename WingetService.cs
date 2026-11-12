using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        // Not installed yet — offered via `winget install` instead of `winget upgrade`.
        public bool IsNewInstall { get; set; }

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
        private const int HeartbeatInterval          = 120_000;
        private const int CommandTimeoutMs           = 300_000;
        private const int RecheckTimeoutMs           = 120_000;

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

        public async Task<string> RunCommandAsync(string arguments, int timeoutMs = CommandTimeoutMs)
        {
            return await Task.Run(() =>
            {
                var sb = new StringBuilder(8192);
                try
                {
                    RunProcess(arguments, line => { lock (sb) sb.AppendLine(line); }, CancellationToken.None, timeoutMs);
                }
                catch (TimeoutException)
                {
                    return $"[ERROR] winget did not respond within {timeoutMs / 60000} min (winget {arguments}).";
                }
                catch (Exception ex)
                {
                    return $"[ERROR] Cannot launch winget: {ex.Message}";
                }
                lock (sb) return sb.ToString();
            });
        }

        // Streams are read asynchronously and the wait is on the process itself, not on stream EOF:
        // an installer winget spawns (e.g. Visual Studio's) can inherit the pipe and hold it open
        // long after winget has exited, which used to block ReadLine() forever.
        private static int RunProcess(string arguments, Action<string> onLine,
                                      CancellationToken cancellationToken, int timeoutMs)
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

            var  proc     = new Process { StartInfo = psi };
            var  outDone  = new ManualResetEventSlim();
            var  errDone  = new ManualResetEventSlim();
            bool finished = false;

            // Not disposed on purpose: a leaked pipe can still raise these after we return.
            proc.OutputDataReceived += (s, e) => { if (e.Data == null) outDone.Set(); else if (!finished) onLine(e.Data); };
            proc.ErrorDataReceived  += (s, e) => { if (e.Data == null) errDone.Set(); else if (!finished) onLine(e.Data); };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            var elapsed = Stopwatch.StartNew();
            while (!proc.WaitForExit(250))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    finished = true;
                    KillProcessTree(proc.Id);
                    throw new OperationCanceledException(cancellationToken);
                }
                if (timeoutMs > 0 && elapsed.ElapsedMilliseconds > timeoutMs)
                {
                    finished = true;
                    KillProcessTree(proc.Id);
                    throw new TimeoutException();
                }
            }

            bool drained = outDone.Wait(2000) & errDone.Wait(2000);
            finished = true;
            int exitCode = proc.ExitCode;
            if (drained) proc.Dispose();
            return exitCode;
        }

        // Kills winget and the installer it launched, so a cancelled upgrade leaves nothing behind.
        private static void KillProcessTree(int pid)
        {
            try
            {
                using (var kill = Process.Start(new ProcessStartInfo("taskkill.exe", $"/PID {pid} /T /F")
                       { UseShellExecute = false, CreateNoWindow = true }))
                    kill?.WaitForExit(5000);
            }
            catch { }
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
            ThrowIfWingetFailed(raw);
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

            var installedIds = await RecheckMissedUpdatesAsync(updates, seenIds, progress, cancellationToken);

            // Keep previously-failed upgrades visible: winget drops them from the scan when
            // their source errors. Only if still installed at the same version and nothing else
            // under that Id is installed at another version (a newer copy supersedes it, and a
            // leftover registry entry of the old one must not keep it listed).
            foreach (var failed in FailedUpgrades.All())
            {
                if (seenIds.Contains(failed.Id)) continue;
                if (!installedIds.TryGetValue(failed.Id, out var versions) ||
                    versions.Count != 1 || !versions.Contains(failed.InstalledVersion))
                {
                    FailedUpgrades.Remove(failed.Id);
                    continue;
                }
                failed.CurrentStatus = "✗ Failed";
                updates.Add(failed);
                seenIds.Add(failed.Id);
            }

            if (!seenIds.Contains(VcRedistAioId) && !installedIds.ContainsKey(VcRedistAioId))
            {
                var aio = await GetVcRedistAioAsync();
                if (aio != null) updates.Add(aio);
            }

            updates.RemoveAll(p => ExcludedPackages.IsExcluded(p.Id));

            foreach (var pkg in updates)
                pkg.Track = VersionLockedId.IsMatch(pkg.Id) ? "Version-locked" : string.Empty;

            return updates;
        }

        private const string VcRedistAioId = "abbodi1406.vcredist";

        // One install covers every VC++ runtime (2005–2026, x86/x64); once installed it is tracked
        // by the regular upgrade scan, so it is only offered here while missing.
        private async Task<WingetPackage> GetVcRedistAioAsync()
        {
            string raw   = await RunCommandAsync($"show --id \"{VcRedistAioId}\" --exact --accept-source-agreements");
            var    match = Regex.Match(raw, @"^Version:\s*(\S+)", RegexOptions.Multiline);
            if (!match.Success) return null;

            return new WingetPackage
            {
                Name             = "Visual C++ Redistributable AIO",
                Id               = VcRedistAioId,
                InstalledVersion = "Not installed",
                AvailableVersion = match.Groups[1].Value,
                Source           = "winget",
                CurrentStatus    = "⬆ Install Available",
                IsNewInstall     = true
            };
        }

        // The bulk "upgrade" scan silently drops a package when winget's own name-matching
        // finds several candidates for it (e.g. multiple "qBittorrent" listings in the source) —
        // it doesn't error, it just omits the row. Re-querying that exact Id alone sidesteps
        // the ambiguity and recovers the update if one actually exists. This also covers
        // packages winget only knows locally through their ARP registry key (no dotted winget
        // Id, Source blank) — e.g. qBittorrent installed by its own installer rather than
        // winget — where the bulk scan's internal catalog match can fail silently the same way;
        // those have no queryable Id, so they're re-queried by display name instead.
        // Returns Id → every installed version winget lists under that Id.
        private async Task<Dictionary<string, HashSet<string>>> RecheckMissedUpdatesAsync(List<WingetPackage> updates, HashSet<string> seenIds,
                                                       IProgress<string> progress, CancellationToken cancellationToken)
        {
            string rawInstalled = await RunCommandAsync("list --accept-source-agreements");
            ThrowIfWingetFailed(rawInstalled);
            var    installed    = ParseInstalledOutput(rawInstalled, cancellationToken);

            var installedIds = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in installed)
            {
                if (!installedIds.TryGetValue(p.Id, out var set))
                    installedIds[p.Id] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                set.Add(p.InstalledVersion);
            }

            var toRecheck = installed
                .Where(p => !seenIds.Contains(p.Id) && !ExcludedPackages.IsExcluded(p.Id) &&
                            !string.IsNullOrWhiteSpace(p.Name) &&
                            !IsEdgeComponent(p.Name, p.Id) &&
                            (AppSettings.IncludeBetaVersions || !IsPreReleaseChannel(p.Name, p.Id)))
                .ToList();

            if (toRecheck.Count == 0) return installedIds;

            progress?.Report($"Deep-checking {toRecheck.Count} package(s) individually — this can take a bit…");

            using (var gate = new SemaphoreSlim(FallbackRecheckConcurrency))
            {
                var tasks = toRecheck.Select(async pkg =>
                {
                    await gate.WaitAsync(cancellationToken);
                    try
                    {
                        string query = pkg.Source != "Local Registry"
                            ? (IsTruncated(pkg.Id)
                                ? $"list --id \"{ToQuery(pkg.Id)}\" --accept-source-agreements"
                                : $"list --id \"{pkg.Id}\" --exact --accept-source-agreements")
                            : $"list \"{ToQuery(pkg.Name)}\" --accept-source-agreements";
                        string raw = await RunCommandAsync(query, RecheckTimeoutMs);
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
            return installedIds;
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
            ResolveColumns(lines, headerIdx, sepIdx, requireAvailable: true,
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
            ThrowIfWingetFailed(raw);
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
            ResolveColumns(lines, headerIdx, sepIdx, requireAvailable: false,
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

        private static readonly Regex WingetFailure = new Regex(
            @"Failed when searching source|An unexpected error occurred|0x8[0-9A-Fa-f]{7}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // A missing/broken winget or an offline source produces no table at all — without this
        // the empty parse result was reported as "All packages are up to date".
        private static void ThrowIfWingetFailed(string raw)
        {
            if (raw.StartsWith("[ERROR]")) throw new InvalidOperationException(raw.Substring(7).Trim());

            string[] lines = SplitLines(AnsiEscape.Replace(raw, string.Empty));
            if (FindHeaderLine(lines, requireAvailable: false) >= 0) return;

            var failures = lines.Where(l => WingetFailure.IsMatch(l)).Select(l => l.Trim()).ToList();
            if (failures.Count == 0) return;

            // msstore is often broken on its own (0x8a150044, followed by a generic error line)
            // while the winget source answered fine — that alone is not a failed scan.
            bool onlyMsStore = failures.Any(f => f.IndexOf("msstore", StringComparison.OrdinalIgnoreCase) >= 0) &&
                               !failures.Any(f => f.StartsWith("Failed when searching source", StringComparison.OrdinalIgnoreCase) &&
                                                  f.IndexOf("msstore", StringComparison.OrdinalIgnoreCase) < 0);
            if (!onlyMsStore) throw new InvalidOperationException($"winget: {failures[0]}");
        }

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

            // Localized winget: no English labels, but the table still has a dashed rule under its header.
            for (int i = 1; i < lines.Length; i++)
                if (IsSeparatorLine(lines[i]) && !string.IsNullOrWhiteSpace(lines[i - 1]))
                    return i - 1;
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

        private static void ResolveColumns(string[] lines, int headerIdx, int sepIdx, bool requireAvailable,
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
            if (posName >= 0 && posId >= 0 && posVersion >= 0) return;

            // Localized labels: column order is fixed (Name, Id, Version, [Available], Source).
            var starts = GetColumnStartsFromData(lines, headerIdx, sepIdx);
            posName = posId = posVersion = posAvailable = posSource = -1;
            if (starts.Count < 3) return;
            posName    = starts[0];
            posId      = starts[1];
            posVersion = starts[2];
            if (starts.Count >= 5)  { posAvailable = starts[3]; posSource = starts[4]; }
            else if (starts.Count == 4)
            {
                if (requireAvailable) posAvailable = starts[3];
                else                  posSource    = starts[3];
            }
        }

        // A header word starts a column only if every data row has a blank just before it —
        // this keeps multi-word labels (e.g. "Phiên bản") from being split into two columns.
        private static List<int> GetColumnStartsFromData(string[] lines, int headerIdx, int sepIdx)
        {
            string header = lines[headerIdx];
            var data = new List<string>();
            for (int i = (sepIdx >= 0 ? sepIdx : headerIdx) + 1; i < lines.Length; i++)
            {
                if (FooterLine.IsMatch(lines[i])) break;
                if (!string.IsNullOrWhiteSpace(lines[i]) && !IsSeparatorLine(lines[i])) data.Add(lines[i]);
            }

            var starts = new List<int>();
            for (int i = 0; i < header.Length; i++)
            {
                if (header[i] == ' ' || (i > 0 && header[i - 1] != ' ')) continue;
                if (i == 0 || data.All(l => i - 1 >= l.Length || l[i - 1] == ' ')) starts.Add(i);
            }
            return starts;
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

        // winget won't touch a user-scope package from an elevated process, and Winget Manager
        // always runs elevated — surface that instead of leaving a bare "failed".
        private static readonly Regex UserScopeElevatedError = new Regex(
            "user scope cannot be .* when running with administrator", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Set when an installer returned 3010 (success, restart needed); cleared by the caller
        // at the start of a session via ResetRebootFlag().
        public bool RebootRequired { get; private set; }
        public void ResetRebootFlag() => RebootRequired = false;

        // Running Store/MSIX apps (e.g. Windows Terminal) are staged by winget, which reports
        // success but keeps the old version until the app is closed — so the next scan still lists
        // them. Tracked here to show "close the app" instead of a seemingly endless re-upgrade.
        private static readonly Regex AppRestartNeeded = new Regex(
            "Restart the application to complete the upgrade", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private bool _appRestartNeeded;
        public HashSet<string> PendingAppRestartIds { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // ── Upgrade ───────────────────────────────────────────────────────────

        public async Task<bool> UpgradePackageAsync(string packageId,
                                                     string source,
                                                     string installedVersion,
                                                     CancellationToken cancellationToken = default,
                                                     IProgress<string> progress = null,
                                                     bool isNewInstall = false)
        {
            var args = new StringBuilder(isNewInstall
                ? $"install --id \"{packageId}\" --exact --source winget"
                : $"upgrade --id \"{ToQuery(packageId)}\"");
            if (!isNewInstall && IsMsStoreSource(source)) args.Append(" --source msstore");
            else if (!isNewInstall && IsRegistryOnlyId(packageId))
            {
                // Registry-only entry (ARP\...): with no source winget also queries msstore, which
                // errors out (0x8a150044) on machines where that source is broken. Pinning it to
                // winget matches the package, and --include-unknown covers its undetectable version.
                args.Append(" --source winget --include-unknown");
            }
            if (AppSettings.SilentMode)       args.Append(" --silent");
            if (AppSettings.ForceInstall)     args.Append(" --force");
            if (AppSettings.AcceptAgreements) args.Append(" --accept-package-agreements --accept-source-agreements");

            _appRestartNeeded = false;
            var (ok, ambiguous) = await RunWingetOperationAsync(args.ToString(), cancellationToken, progress);
            if (!ok && ambiguous && !string.IsNullOrWhiteSpace(installedVersion))
            {
                progress?.Report($"Multiple installed versions matched — retrying with --version \"{installedVersion}\"…");
                args.Append($" --version \"{installedVersion}\"");
                (ok, _) = await RunWingetOperationAsync(args.ToString(), cancellationToken, progress);
            }

            if (ok && _appRestartNeeded && ReleaseStalePackageHosts(packageId, progress) > 0)
            {
                _appRestartNeeded = false;
                (ok, _) = await RunWingetOperationAsync(args.ToString(), cancellationToken, progress);
            }

            if (ok && _appRestartNeeded) PendingAppRestartIds.Add(packageId);
            else if (ok) PendingAppRestartIds.Remove(packageId);
            return ok;
        }

        // A Store/MSIX package refuses to update while a COM surrogate (dllhost.exe, e.g. the
        // "Open in Terminal" shell extension) still has the old version's DLLs loaded — there is
        // no app window to close. Only dllhost processes holding files of this very package
        // (MSIX folder name = winget Id, e.g. Microsoft.WindowsTerminal) are stopped — matching
        // any side-by-side package would also hit shared frameworks such as VCLibs x86/x64.
        // Explorer respawns the surrogate on demand.
        private static int ReleaseStalePackageHosts(string packageId, IProgress<string> progress)
        {
            const string WindowsApps = @"C:\Program Files\WindowsApps";
            var staleDirs = new List<string>();
            try
            {
                staleDirs = Directory.GetDirectories(WindowsApps)
                    .Where(d => string.Equals(Path.GetFileName(d).Split('_')[0], packageId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            catch { return 0; }
            if (staleDirs.Count == 0) return 0;

            int stopped = 0;
            foreach (var host in Process.GetProcessesByName("dllhost"))
            {
                using (host)
                {
                    try
                    {
                        string held = host.Modules.Cast<ProcessModule>()
                            .Select(m => m.FileName)
                            .FirstOrDefault(f => staleDirs.Any(d => f.StartsWith(d + "\\", StringComparison.OrdinalIgnoreCase)));
                        if (held == null) continue;

                        progress?.Report($"Stopping dllhost.exe (PID {host.Id}) holding {Path.GetFileName(held)} so the update can apply…");
                        host.Kill();
                        host.WaitForExit(3000);
                        stopped++;
                    }
                    catch { }
                }
            }
            return stopped;
        }

        // winget cuts over-long table cells with "…". --id without --exact is a substring match,
        // so the untruncated prefix still finds the package; quotes would break the argument.
        private static bool IsTruncated(string value) => value?.EndsWith("…") == true;

        private static string ToQuery(string value) =>
            (value ?? string.Empty).TrimEnd('…').Replace("\"", string.Empty).Trim();

        private static bool IsRegistryOnlyId(string id) =>
            id != null && id.StartsWith("ARP\\", StringComparison.OrdinalIgnoreCase);

        private static bool IsMsStoreSource(string source) =>
            string.Equals(source, "msstore", StringComparison.OrdinalIgnoreCase);

        // ── Uninstall ─────────────────────────────────────────────────────────

        public async Task<bool> UninstallPackageAsync(string packageId,
                                                       string installedVersion,
                                                       CancellationToken cancellationToken = default,
                                                       IProgress<string> progress = null)
        {
            var args = new StringBuilder($"uninstall --id \"{ToQuery(packageId)}\"");
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

        // ── Version pins ──────────────────────────────────────────────────────

        // Raw `winget pin list` output (empty list text when nothing is pinned).
        public Task<string> GetPinListAsync() => RunCommandAsync("pin list --accept-source-agreements");

        // Removes winget's version pin so the package can be upgraded again. Returns false
        // when the package has no pin or winget refuses (the reason is streamed to progress).
        public async Task<bool> RemovePinAsync(string packageId,
                                                CancellationToken cancellationToken = default,
                                                IProgress<string> progress = null)
        {
            var (ok, _) = await RunWingetOperationAsync(
                $"pin remove --id \"{packageId}\" --accept-source-agreements", cancellationToken, progress);
            return ok;
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
                var  gate                 = new object();
                bool textIndicatesFailure = false;
                bool ambiguousVersions    = false;
                bool rebootRequired       = false;

                Action<string> onLine = line =>
                {
                    string cleaned = AnsiEscape.Replace(line, string.Empty).Trim();
                    if (string.IsNullOrEmpty(cleaned) || IsSpinnerFragment(cleaned)) return;

                    lock (gate)
                    {
                        // Some installers (e.g. PawnIO) return 3010 = success, reboot required, and
                        // winget words it "Installer failed with exit code: 3010" — not a real failure.
                        if (cleaned.IndexOf("exit code: 3010", StringComparison.OrdinalIgnoreCase) >= 0)
                            rebootRequired = true;
                        else if (cleaned.IndexOf("failed",                      StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 cleaned.IndexOf("error",                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 cleaned.IndexOf("different install technology", StringComparison.OrdinalIgnoreCase) >= 0)
                            textIndicatesFailure = true;

                        if (MultipleVersionsError.IsMatch(cleaned)) ambiguousVersions = true;
                        if (AppRestartNeeded.IsMatch(cleaned))      _appRestartNeeded = true;
                    }

                    if (UserScopeElevatedError.IsMatch(cleaned))
                        progress?.Report("Hint: this package is installed for the current user only. " +
                                         "winget refuses to change it from an Administrator process — " +
                                         "run the same winget command from a normal (non-admin) terminal.");

                    progress?.Report(cleaned);
                };

                // Big installers (Visual Studio) run for many minutes without printing anything.
                var started = Stopwatch.StartNew();
                using (new Timer(_ => progress?.Report(
                           $"Still running… {(int)started.Elapsed.TotalMinutes} min elapsed (installer is working silently)."),
                           null, HeartbeatInterval, HeartbeatInterval))
                {
                    int exitCode;
                    try
                    {
                        exitCode = RunProcess(arguments, onLine, cancellationToken, timeoutMs: 0);
                    }
                    catch (OperationCanceledException)
                    {
                        progress?.Report("Cancelled — winget and its installer were stopped.");
                        return (false, false);
                    }
                    catch (Exception ex)
                    {
                        progress?.Report($"[ERROR] {ex.Message}");
                        return (false, false);
                    }

                    lock (gate)
                    {
                        // Exit code 3010 = success, reboot required (Windows Installer standard).
                        bool success = !textIndicatesFailure &&
                                       (rebootRequired || exitCode == 0 || exitCode == 3010);
                        if (success && (rebootRequired || exitCode == 3010))
                        {
                            RebootRequired = true;
                            progress?.Report("Installed — a restart is required to finish.");
                        }
                        return (success, ambiguousVersions);
                    }
                }
            });
        }

        // With \r treated as a line break, winget's progress spinner arrives as lone "-", "\", "|", "/" lines.
        private static bool IsSpinnerFragment(string s) => s.Length == 1 && @"-\|/".IndexOf(s[0]) >= 0;
    }
}
