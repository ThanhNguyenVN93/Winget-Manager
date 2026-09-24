using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace frm_winget_upgrade
{
    internal static class ExcludedPackages
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WingetManager", "excluded.txt");

        private static readonly object Gate = new object();
        private static readonly HashSet<string> Ids = Load();

        public static bool IsExcluded(string id)
        {
            lock (Gate) return Ids.Contains(id);
        }

        public static IReadOnlyList<string> All()
        {
            lock (Gate) return Ids.OrderBy(i => i, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static bool Add(string id)
        {
            id = id?.Trim();
            if (string.IsNullOrEmpty(id)) return false;
            lock (Gate)
            {
                if (!Ids.Add(id)) return false;
                Save();
                return true;
            }
        }

        public static void Remove(string id)
        {
            lock (Gate)
            {
                if (Ids.Remove(id)) Save();
            }
        }

        private static HashSet<string> Load()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (File.Exists(FilePath))
                    foreach (var line in File.ReadAllLines(FilePath))
                        if (!string.IsNullOrWhiteSpace(line)) set.Add(line.Trim());
            }
            catch { }
            return set;
        }

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllLines(FilePath, Ids);
            }
            catch { }
        }
    }
}
