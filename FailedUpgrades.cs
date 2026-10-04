using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace frm_winget_upgrade
{
    // Remembers packages whose upgrade failed so a rescan keeps listing them. winget omits a
    // package from `upgrade` output when its source errors (e.g. 0x8a150044 "REST API endpoint
    // is not found"), which would otherwise make a failed package silently vanish.
    internal static class FailedUpgrades
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WingetManager", "failed_upgrades.txt");

        private static readonly object Gate = new object();
        private static readonly Dictionary<string, WingetPackage> Items = Load();

        public static IReadOnlyList<WingetPackage> All()
        {
            lock (Gate) return Items.Values.ToList();
        }

        public static void Add(WingetPackage pkg)
        {
            if (pkg == null || string.IsNullOrWhiteSpace(pkg.Id)) return;
            lock (Gate)
            {
                Items[pkg.Id] = pkg;
                Save();
            }
        }

        public static void Remove(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            lock (Gate)
            {
                if (Items.Remove(id)) Save();
            }
        }

        private static Dictionary<string, WingetPackage> Load()
        {
            var map = new Dictionary<string, WingetPackage>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (File.Exists(FilePath))
                    foreach (var line in File.ReadAllLines(FilePath))
                    {
                        var f = line.Split('\t');
                        if (f.Length < 5 || string.IsNullOrWhiteSpace(f[0])) continue;
                        map[f[0]] = new WingetPackage
                        {
                            Id = f[0], Name = f[1], InstalledVersion = f[2],
                            AvailableVersion = f[3], Source = f[4]
                        };
                    }
            }
            catch { }
            return map;
        }

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllLines(FilePath, Items.Values.Select(p =>
                    string.Join("\t", p.Id, p.Name, p.InstalledVersion, p.AvailableVersion, p.Source)));
            }
            catch { }
        }
    }
}
