using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace frm_winget_upgrade
{
    public sealed class UpdateInfo
    {
        public Version Version     { get; set; }
        public string  TagName     { get; set; }
        public string  DownloadUrl { get; set; }
        public string  HtmlUrl     { get; set; }
    }

    public sealed class UpdateService
    {
        private const string LatestReleaseApiUrl =
            "https://api.github.com/repos/ThanhNguyenVN93/Winget-Manager/releases/latest";

        // GitHub rejects requests without a User-Agent header.
        private const string UserAgent = "WingetManager-UpdateChecker";

        public async Task<UpdateInfo> CheckForUpdateAsync()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            string json;
            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                client.Headers[HttpRequestHeader.Accept]     = "application/vnd.github+json";
                json = await client.DownloadStringTaskAsync(LatestReleaseApiUrl);
            }

            var data = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);

            string tagName = data.TryGetValue("tag_name", out var tag) ? tag as string : null;
            if (string.IsNullOrWhiteSpace(tagName)) return null;
            if (!Version.TryParse(tagName.TrimStart('v', 'V'), out Version remoteVersion)) return null;

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            if (NormalizeVersion(remoteVersion) <= NormalizeVersion(currentVersion)) return null;

            string downloadUrl = FindExeAssetUrl(data);
            if (string.IsNullOrWhiteSpace(downloadUrl)) return null;

            return new UpdateInfo
            {
                Version     = remoteVersion,
                TagName     = tagName,
                DownloadUrl = downloadUrl,
                HtmlUrl     = data.TryGetValue("html_url", out var url) ? url as string : null
            };
        }

        private static string FindExeAssetUrl(Dictionary<string, object> release)
        {
            if (!release.TryGetValue("assets", out var assetsObj) || !(assetsObj is ArrayList assets))
                return null;

            foreach (var item in assets)
            {
                if (!(item is Dictionary<string, object> asset)) continue;
                string name = asset.TryGetValue("name", out var n) ? n as string : null;
                if (name != null && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    return asset.TryGetValue("browser_download_url", out var u) ? u as string : null;
            }
            return null;
        }

        // AssemblyVersion often omits Build/Revision (e.g. "1.0.0" → Revision = -1),
        // which Version treats as lower than an explicit 0 — normalize before comparing.
        private static Version NormalizeVersion(Version v) =>
            new Version(v.Major, v.Minor, Math.Max(v.Build, 0), Math.Max(v.Revision, 0));

        public async Task DownloadAndApplyUpdateAsync(UpdateInfo info, IProgress<int> progress = null)
        {
            string currentExePath = Application.ExecutablePath;
            string tempExePath    = Path.Combine(Path.GetTempPath(), $"WingetManager_{info.TagName}.exe");

            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                if (progress != null)
                    client.DownloadProgressChanged += (s, e) => progress.Report(e.ProgressPercentage);
                await client.DownloadFileTaskAsync(info.DownloadUrl, tempExePath);
            }

            string scriptPath = Path.Combine(Path.GetTempPath(), "WingetManager_update.bat");
            int    pid        = Process.GetCurrentProcess().Id;

            // Waits for this process to exit (a running EXE can't be overwritten in place),
            // then swaps the file and relaunches — ping is used as a console-free sleep.
            File.WriteAllText(scriptPath,
                "@echo off\r\n" +
                ":wait\r\n" +
                $"tasklist /fi \"PID eq {pid}\" | find \"{pid}\" >nul\r\n" +
                "if not errorlevel 1 (\r\n" +
                "    ping 127.0.0.1 -n 2 >nul\r\n" +
                "    goto wait\r\n" +
                ")\r\n" +
                $"move /y \"{tempExePath}\" \"{currentExePath}\"\r\n" +
                $"start \"\" \"{currentExePath}\"\r\n" +
                "del \"%~f0\"\r\n");

            Process.Start(new ProcessStartInfo
            {
                FileName        = scriptPath,
                UseShellExecute = true,
                WindowStyle     = ProcessWindowStyle.Hidden,
                CreateNoWindow  = true
            });

            Application.Exit();
        }
    }
}
