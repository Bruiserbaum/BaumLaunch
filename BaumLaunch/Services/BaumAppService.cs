using System.Diagnostics;
using System.Text.Json.Nodes;
using BaumLaunch.Models;

namespace BaumLaunch.Services;

/// <summary>
/// Checks GitHub releases and installs/updates Baum apps (BaumDash, BaumAdminTool, etc.).
/// </summary>
public static class BaumAppService
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
    };

    static BaumAppService()
    {
        _http.DefaultRequestHeaders.Add("User-Agent", "BaumLaunch-AppManager");
    }

    /// <summary>Returns the canonical list of managed Baum apps.</summary>
    public static List<BaumAppEntry> GetCatalog() =>
    [
        new BaumAppEntry
        {
            RepoName    = "BaumLaunch",
            DisplayName = "BaumLaunch",
            ExePath     = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "BaumLaunch", "BaumLaunch.exe"),
        },
        new BaumAppEntry
        {
            RepoName    = "BaumDash",
            DisplayName = "BaumDash",
            ExePath     = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "BaumDash", "WinUIAudioMixer.exe"),
        },
        new BaumAppEntry
        {
            RepoName    = "BaumAdminTool",
            DisplayName = "BaumAdminTool",
            ExePath     = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "BaumAdminTool", "BaumAdminTool.exe"),
        },
        new BaumAppEntry
        {
            RepoName    = "BaumKeyGenerator",
            DisplayName = "BaumKeyGenerator",
            ExePath     = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "BaumKeyGenerator", "BaumKeyGenerator.exe"),
        },
    ];

    /// <summary>
    /// Reads the installed version from the target exe's FileVersionInfo.
    /// Returns null if not installed.
    /// </summary>
    public static Version? GetInstalledVersion(BaumAppEntry app)
    {
        if (!File.Exists(app.ExePath)) return null;
        try
        {
            var fvi = FileVersionInfo.GetVersionInfo(app.ExePath);
            if (Version.TryParse(fvi.FileVersion, out var v)) return v;
        }
        catch { /* permission error or corrupt exe */ }
        return null;
    }

    /// <summary>
    /// Queries GitHub for the latest release. Populates LatestVersion, DownloadUrl, and Status.
    /// </summary>
    public static async Task CheckAsync(BaumAppEntry app)
    {
        try
        {
            string url = $"https://api.github.com/repos/{app.RepoOwner}/{app.RepoName}/releases/latest";
            string json = await _http.GetStringAsync(url);
            var doc = JsonNode.Parse(json)!;

            string tag = doc["tag_name"]!.GetValue<string>().TrimStart('v');
            if (!Version.TryParse(tag, out var latest))
            {
                app.Status = BaumAppStatus.Unknown;
                return;
            }

            app.LatestVersion = latest;

            // Find the .exe installer asset
            var assets = doc["assets"]!.AsArray();
            foreach (var asset in assets)
            {
                string name = asset!["name"]!.GetValue<string>();
                if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    app.DownloadUrl = asset["browser_download_url"]!.GetValue<string>();
                    break;
                }
            }

            app.InstalledVersion = GetInstalledVersion(app);

            if (app.InstalledVersion == null)
                app.Status = BaumAppStatus.NotInstalled;
            else if (app.InstalledVersion >= latest)
                app.Status = BaumAppStatus.UpToDate;
            else
                app.Status = BaumAppStatus.UpdateAvailable;
        }
        catch
        {
            app.Status = BaumAppStatus.Unknown;
        }
    }

    /// <summary>
    /// Downloads the installer to a temp folder and runs it silently.
    /// Progress is reported via <paramref name="onProgress"/> (0–100).
    /// </summary>
    public static async Task InstallOrUpdateAsync(
        BaumAppEntry app,
        Action<int> onProgress,
        Action onComplete)
    {
        if (string.IsNullOrEmpty(app.DownloadUrl))
        {
            app.Status = BaumAppStatus.Failed;
            onComplete();
            return;
        }

        try
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "BaumApps");
            Directory.CreateDirectory(tempDir);

            string fileName = $"{app.RepoName}-Setup-{app.LatestVersion}.exe";
            string installerPath = Path.Combine(tempDir, fileName);

            app.Status = BaumAppStatus.Downloading;
            app.DownloadProgress = 0;
            onProgress(0);

            // Stream download with progress
            using var response = await _http.GetAsync(app.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long? total = response.Content.Headers.ContentLength;
            await using var src  = await response.Content.ReadAsStreamAsync();
            await using var dest = File.Create(installerPath);

            var buffer = new byte[81920];
            long downloaded = 0;
            int read;
            while ((read = await src.ReadAsync(buffer)) > 0)
            {
                await dest.WriteAsync(buffer.AsMemory(0, read));
                downloaded += read;
                if (total.HasValue)
                {
                    int pct = (int)(downloaded * 100 / total.Value);
                    app.DownloadProgress = pct;
                    onProgress(pct);
                }
            }

            app.Status = BaumAppStatus.Installing;
            app.DownloadProgress = 100;
            onProgress(100);

            // Run the Inno Setup installer silently
            var psi = new ProcessStartInfo(installerPath,
                "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-")
            {
                UseShellExecute = false,
                CreateNoWindow  = true,
            };

            bool isSelf = string.Equals(app.RepoName, "BaumLaunch", StringComparison.OrdinalIgnoreCase);

            if (isSelf)
            {
                // Self-update: write a batch relay and exit
                string exePath = app.ExePath;
                string scriptPath = Path.Combine(tempDir, "baum-self-update.bat");
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

                Process.Start(new ProcessStartInfo("cmd.exe", $"/c \"{scriptPath}\"")
                {
                    CreateNoWindow  = true,
                    WindowStyle     = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                });

                await Task.Delay(600);
                Application.Exit();
                return;
            }
            else
            {
                var proc = Process.Start(psi)!;
                await proc.WaitForExitAsync();
            }

            // Re-read installed version after install
            app.InstalledVersion = GetInstalledVersion(app);
            app.Status = (app.InstalledVersion != null) ? BaumAppStatus.Updated : BaumAppStatus.Failed;
            app.DownloadProgress = -1;
            onComplete();
        }
        catch
        {
            app.Status = BaumAppStatus.Failed;
            app.DownloadProgress = -1;
            onComplete();
        }
    }
}
