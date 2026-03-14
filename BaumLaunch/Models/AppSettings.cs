using System.Text.Json;
using Microsoft.Win32;

namespace BaumLaunch.Models;

public sealed class AppSettings
{
    public bool StartWithWindows { get; set; } = false;
    /// <summary>How often to auto-check for winget package updates, in hours. 0 = manual only.</summary>
    public int  UpdateCheckHours { get; set; } = 6;
    /// <summary>Run a package status check immediately on startup.</summary>
    public bool CheckOnStartup   { get; set; } = true;

    // ── Persistence ──────────────────────────────────────────────────────────

    private static string FilePath =>
        Path.Combine(AppContext.BaseDirectory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
                if (loaded != null) return loaded;
            }
        }
        catch { }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(FilePath,
                JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    // ── Start with Windows ────────────────────────────────────────────────────

    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "BaumLaunch";

    public static bool GetStartWithWindows()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        return key?.GetValue(RunValueName) != null;
    }

    public static void SetStartWithWindows(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        if (key == null) return;
        if (enable)
            key.SetValue(RunValueName, $"\"{Application.ExecutablePath}\"");
        else
            key.DeleteValue(RunValueName, false);
    }
}
