using Microsoft.Win32;

namespace BaumLaunch.Services;

/// <summary>
/// Reads the Windows Add/Remove Programs (ARP) registry to detect installed apps
/// without spawning any child processes.
/// </summary>
public static class RegistryService
{
    private static readonly string[] UninstallPaths =
    {
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
    };

    /// <summary>
    /// Returns all ARP-registered apps as a case-insensitive dictionary of
    /// DisplayName → DisplayVersion.  Reads both HKLM and HKCU.
    /// </summary>
    public static Dictionary<string, string> GetInstalledApps()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in UninstallPaths)
        foreach (var hive in new[] { Registry.LocalMachine, Registry.CurrentUser })
        {
            try
            {
                using var key = hive.OpenSubKey(path, false);
                if (key == null) continue;

                foreach (var subName in key.GetSubKeyNames())
                {
                    try
                    {
                        using var sub = key.OpenSubKey(subName, false);
                        if (sub == null) continue;

                        var name    = sub.GetValue("DisplayName")    as string;
                        var version = sub.GetValue("DisplayVersion") as string;

                        // Skip system components and Windows updates
                        var sysComp = sub.GetValue("SystemComponent");
                        if (sysComp is int sc && sc == 1) continue;

                        if (!string.IsNullOrWhiteSpace(name))
                            result.TryAdd(name.Trim(), version?.Trim() ?? "");
                    }
                    catch { /* skip unreadable entries */ }
                }
            }
            catch { /* skip inaccessible hives */ }
        }

        return result;
    }
}
