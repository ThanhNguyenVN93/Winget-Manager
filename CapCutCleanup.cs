using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace frm_winget_upgrade
{
    // The CapCut installer drops each release into %LOCALAPPDATA%\CapCut\Apps\<version> and never
    // removes the previous ones, so every upgrade leaves ~1 GB behind.
    internal static class CapCutCleanup
    {
        public const string PackageId = "ByteDance.CapCut";

        private static readonly Regex VersionFolder = new Regex(@"^\d+\.\d+\.\d+(\.\d+)?$", RegexOptions.Compiled);

        public sealed class Result
        {
            public string SkipReason;
            public readonly List<(string Name, double MB)> Deleted = new List<(string, double)>();
            public readonly List<(string Name, string Error)> Failed = new List<(string, string)>();
            public double FreedMB => Deleted.Sum(d => d.MB);
        }

        public static bool IsCapCut(string packageId) =>
            string.Equals(packageId, PackageId, StringComparison.OrdinalIgnoreCase);

        public static Result RemoveOldVersions()
        {
            var result = new Result();
            try
            {
                string appsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapCut", "Apps");
                if (!Directory.Exists(appsDir)) { result.SkipReason = "install folder not found"; return result; }

                var folders = new DirectoryInfo(appsDir).GetDirectories()
                                                        .Where(d => VersionFolder.IsMatch(d.Name))
                                                        .OrderBy(d => Version.Parse(d.Name))
                                                        .ToList();
                if (folders.Count <= 1) return result;

                // Only delete when the installed version matches a folder exactly: a stale or
                // unreadable registry value must never cause the live version to be removed.
                string current = GetInstalledVersion();
                if (current == null || !folders.Any(f => f.Name == current))
                {
                    result.SkipReason = $"installed version ({current ?? "unknown"}) does not match any version folder";
                    return result;
                }

                if (IsRunning(appsDir)) { result.SkipReason = "CapCut is running — close it and upgrade again to clean up"; return result; }

                string newest = folders[folders.Count - 1].Name;
                foreach (var folder in folders.Where(f => f.Name != current && f.Name != newest))
                {
                    double mb = GetSizeMB(folder);
                    try
                    {
                        DeleteTree(folder);
                        result.Deleted.Add((folder.Name, mb));
                    }
                    catch (Exception ex) { result.Failed.Add((folder.Name, ex.Message)); }
                }
            }
            catch (Exception ex) { result.SkipReason = ex.Message; }
            return result;
        }

        private static string GetInstalledVersion()
        {
            string ReadValue(string path, string name)
            {
                using (var key = Registry.CurrentUser.OpenSubKey(path))
                    return (key?.GetValue(name) as string)?.Trim();
            }

            string v = ReadValue(@"Software\Bytedance\CapCut", "ver");
            if (string.IsNullOrEmpty(v)) v = ReadValue(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\CapCut", "DisplayVersion");
            return string.IsNullOrEmpty(v) ? null : v;
        }

        private static bool IsRunning(string appsDir)
        {
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    if (p.ProcessName.StartsWith("CapCut", StringComparison.OrdinalIgnoreCase)) return true;
                    string exe = p.MainModule?.FileName;
                    if (exe != null && exe.StartsWith(appsDir + "\\", StringComparison.OrdinalIgnoreCase)) return true;
                }
                catch { }
                finally { p.Dispose(); }
            }
            return false;
        }

        private static double GetSizeMB(DirectoryInfo dir)
        {
            try { return Math.Round(dir.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length) / 1048576.0, 1); }
            catch { return 0; }
        }

        // Directory.Delete fails on read-only files, which CapCut ships some of.
        private static void DeleteTree(DirectoryInfo dir)
        {
            foreach (var f in dir.EnumerateFiles("*", SearchOption.AllDirectories))
                if ((f.Attributes & FileAttributes.ReadOnly) != 0) f.Attributes = FileAttributes.Normal;
            dir.Delete(true);
        }
    }
}
