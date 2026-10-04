using System;
using System.Collections.Generic;
using System.IO;

namespace frm_winget_upgrade
{
    // Persisted as simple key=value lines in %APPDATA%\WingetManager\settings.ini.
    public static class AppSettings
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WingetManager", "settings.ini");

        private static readonly object Gate = new object();

        private static bool _forceInstall        = true;
        private static bool _silentMode          = true;
        private static bool _acceptAgreements    = true;
        private static bool _includeBetaVersions = false;

        static AppSettings() => Load();

        public static bool ForceInstall
        {
            get => _forceInstall;
            set { _forceInstall = value; Save(); }
        }

        public static bool SilentMode
        {
            get => _silentMode;
            set { _silentMode = value; Save(); }
        }

        public static bool AcceptAgreements
        {
            get => _acceptAgreements;
            set { _acceptAgreements = value; Save(); }
        }

        public static bool IncludeBetaVersions
        {
            get => _includeBetaVersions;
            set { _includeBetaVersions = value; Save(); }
        }

        private static void Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return;

                var map = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    if (bool.TryParse(line.Substring(eq + 1).Trim(), out bool v))
                        map[line.Substring(0, eq).Trim()] = v;
                }

                if (map.TryGetValue(nameof(ForceInstall),        out bool a)) _forceInstall        = a;
                if (map.TryGetValue(nameof(SilentMode),          out bool b)) _silentMode          = b;
                if (map.TryGetValue(nameof(AcceptAgreements),    out bool c)) _acceptAgreements    = c;
                if (map.TryGetValue(nameof(IncludeBetaVersions), out bool d)) _includeBetaVersions = d;
            }
            catch { }
        }

        private static void Save()
        {
            lock (Gate)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                    File.WriteAllLines(FilePath, new[]
                    {
                        $"{nameof(ForceInstall)}={_forceInstall}",
                        $"{nameof(SilentMode)}={_silentMode}",
                        $"{nameof(AcceptAgreements)}={_acceptAgreements}",
                        $"{nameof(IncludeBetaVersions)}={_includeBetaVersions}"
                    });
                }
                catch { }
            }
        }
    }
}
