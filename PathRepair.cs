using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

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
                // Read and write the raw registry value: Environment.Get/SetEnvironmentVariable
                // expands %VARS% on read and writes REG_SZ, permanently flattening entries such as
                // %USERPROFILE%\... in the user's PATH.
                using (var key = Registry.CurrentUser.OpenSubKey("Environment", writable: true))
                {
                    if (key == null) return fixes;
                    string raw = key.GetValue("Path", null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
                    if (string.IsNullOrEmpty(raw)) return fixes;
                    var kind = key.GetValueKind("Path");

                    var entries = raw.Split(';').ToList();
                    for (int i = 0; i < entries.Count; i++)
                    {
                        string entry    = entries[i];
                        string expanded = Environment.ExpandEnvironmentVariables(entry);
                        if (string.IsNullOrWhiteSpace(entry) || Directory.Exists(expanded)) continue;

                        string repaired = FindReplacement(expanded);
                        if (repaired == null) continue;

                        entries[i] = repaired;
                        fixes.Add((entry, repaired));
                    }

                    if (fixes.Count == 0) return fixes;

                    // Drop duplicates the repair may have created, keeping first occurrence.
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    string updated = string.Join(";", entries.Where(e => string.IsNullOrWhiteSpace(e) || seen.Add(e)));
                    key.SetValue("Path", updated, kind == RegistryValueKind.ExpandString
                        ? RegistryValueKind.ExpandString : RegistryValueKind.String);
                }

                BroadcastEnvironmentChange();

                // Make the running process (and anything it launches) see it immediately.
                string proc = Environment.GetEnvironmentVariable("Path") ?? string.Empty;
                foreach (var (old, @new) in fixes)
                    proc = proc.Replace(Environment.ExpandEnvironmentVariables(old), @new);
                Environment.SetEnvironmentVariable("Path", proc);
            }
            catch { }
            return fixes;
        }

        // entry: <...>\WinGet\Packages\<PackageDir>\<version-folder>[\sub\path]
        // The replacement keeps the same sub-path under the newest version folder, so an
        // unrelated folder that merely shares the leaf name (e.g. "bin") is never picked.
        private static string FindReplacement(string entry)
        {
            int idx = entry.IndexOf(PackagesMarker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;

            string[] parts = entry.Substring(idx + PackagesMarker.Length)
                                  .Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return null;

            string packageDir = entry.Substring(0, idx + PackagesMarker.Length) + parts[0];
            if (!Directory.Exists(packageDir)) return null;

            string subPath = string.Join("\\", parts.Skip(2));

            try
            {
                return Directory.EnumerateDirectories(packageDir)
                                .Select(versionDir => subPath.Length == 0 ? versionDir : Path.Combine(versionDir, subPath))
                                .Where(Directory.Exists)
                                .OrderByDescending(d => Directory.GetLastWriteTimeUtc(d))
                                .FirstOrDefault();
            }
            catch { return null; }
        }

        private static void BroadcastEnvironmentChange()
        {
            try
            {
                SendMessageTimeout(new IntPtr(0xFFFF), 0x001A, IntPtr.Zero, "Environment",
                                   0x0002, 1000, out _);
            }
            catch { }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, IntPtr wParam, string lParam,
                                                        uint flags, uint timeout, out IntPtr result);
    }
}
