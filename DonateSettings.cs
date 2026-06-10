using System;
using System.IO;

namespace frm_winget_upgrade
{
    internal static class DonateSettings
    {
        private static readonly string FlagFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WingetManager", "no_donate");

        public static bool DontShowAgain
        {
            get => File.Exists(FlagFile);
            set
            {
                try
                {
                    if (value)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(FlagFile));
                        File.WriteAllText(FlagFile, "1");
                    }
                    else if (File.Exists(FlagFile))
                    {
                        File.Delete(FlagFile);
                    }
                }
                catch { }
            }
        }
    }
}
