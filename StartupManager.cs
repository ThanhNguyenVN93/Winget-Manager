using System.Diagnostics;
using Microsoft.Win32;

namespace frm_winget_upgrade
{
    // Registers the app to launch at logon in tray mode (see --tray in Program.cs).
    // Scheduled Task is tried first because "/rl highest" launches elevated without a UAC
    // prompt on every boot; the Run key is a fallback for when schtasks.exe can't be used
    // (e.g. restricted environment) and will trigger Form1's elevation prompt each logon.
    public static class StartupManager
    {
        private const string TaskName     = "WingetManagerAutostart";
        private const string RunValueName = "WingetManager";
        private const string RunKeyPath   = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static bool IsEnabled() => TaskExists() || RunKeyExists();

        public static void Enable()
        {
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            if (!TryCreateTask(exePath)) WriteRunKey(exePath);
        }

        public static void Disable()
        {
            TryDeleteTask();
            DeleteRunKey();
        }

        private static bool TryCreateTask(string exePath)
        {
            try
            {
                string arguments =
                    $"/create /tn \"{TaskName}\" /tr \"\\\"{exePath}\\\" --tray\" " +
                    "/sc onlogon /rl highest /f";
                return RunSchtasks(arguments) == 0;
            }
            catch { return false; }
        }

        private static void TryDeleteTask()
        {
            try { RunSchtasks($"/delete /tn \"{TaskName}\" /f"); }
            catch { }
        }

        private static bool TaskExists()
        {
            try { return RunSchtasks($"/query /tn \"{TaskName}\"") == 0; }
            catch { return false; }
        }

        private static int RunSchtasks(string arguments)
        {
            var psi = new ProcessStartInfo("schtasks.exe", arguments)
            {
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true
            };
            using (var p = Process.Start(psi))
            {
                p.WaitForExit();
                return p.ExitCode;
            }
        }

        private static void WriteRunKey(string exePath)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true))
                    key?.SetValue(RunValueName, $"\"{exePath}\" --tray");
            }
            catch { }
        }

        private static void DeleteRunKey()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true))
                    key?.DeleteValue(RunValueName, throwOnMissingValue: false);
            }
            catch { }
        }

        private static bool RunKeyExists()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath))
                    return key?.GetValue(RunValueName) != null;
            }
            catch { return false; }
        }
    }
}
