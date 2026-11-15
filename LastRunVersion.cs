using System;
using System.IO;
using System.Linq;

namespace frm_winget_upgrade
{
    internal static class LastRunVersion
    {
        private static readonly string DataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WingetManager");

        private static readonly string FilePath = Path.Combine(DataDir, "last_version.txt");

        // Returns null on a fresh install. Older versions never wrote this file, so an
        // existing data folder without it means the app was just updated from one of them.
        public static Version Get()
        {
            try
            {
                if (File.Exists(FilePath))
                    return Version.TryParse(File.ReadAllText(FilePath).Trim(), out var v) ? v : null;
                if (Directory.Exists(DataDir) && Directory.EnumerateFiles(DataDir).Any())
                    return new Version(0, 0, 0, 0);
            }
            catch { }
            return null;
        }

        public static void Set(Version version)
        {
            try
            {
                Directory.CreateDirectory(DataDir);
                File.WriteAllText(FilePath, version.ToString());
            }
            catch { }
        }
    }
}
