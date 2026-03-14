using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Nodes;

namespace BaumLaunch.Services;

public static class UpdateService
{
    private const string ApiUrl = "https://api.github.com/repos/Bruiserbaum/BaumLaunch/releases/latest";

    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
    };

    static UpdateService()
    {
        _http.DefaultRequestHeaders.Add("User-Agent", "BaumLaunch-AutoUpdater");
    }

    public static Version CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 1, 0);

    /// <summary>
    /// Queries GitHub for the latest release. Returns (version, downloadUrl) if a newer
    /// version exists, or null if already up to date or the check fails.
    /// </summary>
    public static async Task<(Version Version, string Url)?> CheckAsync()
    {
        try
        {
            string json = await _http.GetStringAsync(ApiUrl);
            var doc = JsonNode.Parse(json)!;

            string tag = doc["tag_name"]!.GetValue<string>().TrimStart('v');
            if (!Version.TryParse(tag, out var latest)) return null;
            if (latest <= CurrentVersion) return null;

            var assets = doc["assets"]!.AsArray();
            foreach (var asset in assets)
            {
                string name = asset!["name"]!.GetValue<string>();
                if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    string url = asset["browser_download_url"]!.GetValue<string>();
                    return (latest, url);
                }
            }
        }
        catch { /* network offline, rate-limited, etc. — silently skip */ }

        return null;
    }

    /// <summary>
    /// Downloads the installer to a temp folder, then spawns a hidden batch script that:
    /// 1. Waits for this process to exit
    /// 2. Runs the installer silently
    /// 3. Relaunches the newly installed exe
    /// Then calls Application.Exit() so the installer can replace our files.
    /// </summary>
    public static async Task DownloadAndInstallAsync(Version version, string downloadUrl)
    {
        try
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "BaumLaunch");
            Directory.CreateDirectory(tempDir);

            string installerPath = Path.Combine(tempDir, $"BaumLaunch-Setup-{version}.exe");

            // Stream download so we don't hold the whole file in RAM
            using var response = await _http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            await using (var src  = await response.Content.ReadAsStreamAsync())
            await using (var dest = File.Create(installerPath))
                await src.CopyToAsync(dest);

            // Installed exe location (per-user Inno Setup default)
            string exePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "BaumLaunch", "BaumLaunch.exe");

            // Write a minimal batch launcher that runs after we exit
            string scriptPath = Path.Combine(tempDir, "baum-update.bat");
            File.WriteAllText(scriptPath,
                $"""
                @echo off
                timeout /t 3 /nobreak >nul
                "{installerPath}" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-
                timeout /t 6 /nobreak >nul
                if exist "{exePath}" start "" "{exePath}"
                del "{installerPath}"
                del "%~f0"
                """);

            // Launch the script hidden then exit
            Process.Start(new ProcessStartInfo("cmd.exe", $"/c \"{scriptPath}\"")
            {
                CreateNoWindow  = true,
                WindowStyle     = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
            });

            // Brief pause so cmd.exe can start before we vanish
            await Task.Delay(600);
            Application.Exit();
        }
        catch { /* download failed — will retry on next scheduled check */ }
    }
}
