using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace frm_winget_upgrade
{
    // winget "portable" zip packages (e.g. yt-dlp.FFmpeg) live in a version-named subfolder,
    // so after an upgrade the user PATH can still point at the old, now-deleted folder.
    // This repoints such stale entries at the matching folder inside the same package dir.
    internal static class PathRepair
    {
        private const string PackagesMarker = @"\WinGet\Packages\";

        // Returns the (old, new) pairs that were repaired.
        public static List<(string Old, string New)> RepairUserPath()
        {
            var fixes = new List<(string, string)>();
            try
            {
                string raw = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);
                if (string.IsNullOrEmpty(raw)) return fixes;

                var entries = raw.Split(';').ToList();
                bool changed = false;

                for (int i = 0; i < entries.Count; i++)
                {
                    string entry = entries[i];
                    if (string.IsNullOrWhiteSpace(entry) || Directory.Exists(entry)) continue;

                    string repaired = FindReplacement(entry);
                    if (repaired == null) continue;

                    entries[i] = repaired;
                    fixes.Add((entry, repaired));
                    changed = true;
                }

                if (!changed) return fixes;

                // Drop duplicates the repair may have created, keeping first occurrence.
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string updated = string.Join(";", entries.Where(e => string.IsNullOrWhiteSpace(e) || seen.Add(e)));

                Environment.SetEnvironmentVariable("Path", updated, EnvironmentVariableTarget.User);

                // Make the running process (and anything it launches) see it immediately.
                string proc = Environment.GetEnvironmentVariable("Path") ?? string.Empty;
                foreach (var (old, @new) in fixes)
                    proc = proc.Replace(old, @new);
                Environment.SetEnvironmentVariable("Path", proc);
            }
            catch { }
            return fixes;
        }

        // entry: <...>\WinGet\Packages\<PackageDir>\<version-folder>\...\<leaf>
        private static string FindReplacement(string entry)
        {
            int idx = entry.IndexOf(PackagesMarker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;

            string afterMarker = entry.Substring(idx + PackagesMarker.Length);
            int slash = afterMarker.IndexOf('\\');
            if (slash <= 0) return null;

            string packageDir = entry.Substring(0, idx + PackagesMarker.Length + slash);
            if (!Directory.Exists(packageDir)) return null;

            string leaf = Path.GetFileName(entry.TrimEnd('\\'));
            if (string.IsNullOrEmpty(leaf)) return null;

            try
            {
                return Directory.EnumerateDirectories(packageDir, leaf, SearchOption.AllDirectories)
                                .OrderByDescending(d => Directory.GetLastWriteTimeUtc(d))
                                .FirstOrDefault();
            }
            catch { return null; }
        }
    }
}
