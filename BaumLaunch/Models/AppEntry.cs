namespace BaumLaunch.Models;

public enum AppStatus { Unknown, NotInstalled, UpToDate, UpdateAvailable, Installing, Updated, Failed, NotManaged }

public sealed class AppEntry
{
    public string    WinGetId         { get; set; } = "";
    public string    DisplayName      { get; set; } = "";
    public string    Category         { get; set; } = "";
    public string?   InstalledVersion { get; set; }   // null = not installed
    public string?   AvailableVersion { get; set; }   // null = checked and up to date, or not yet checked
    public AppStatus Status           { get; set; } = AppStatus.Unknown;
    public bool      IsSelected       { get; set; } = false;  // for profile/batch ops
    /// <summary>True when the installed copy was installed via WinGet (Source = "winget").</summary>
    public bool      IsWinGetManaged  { get; set; } = false;
    /// <summary>
    /// Optional substring to match against ARP display names for packages whose ARP name differs
    /// from the WinGet package ID or catalog DisplayName (e.g. .NET runtimes, VC++ redists).
    /// </summary>
    public string?   ArpNameHint      { get; set; }

    public bool IsInstalled => InstalledVersion != null;
    public bool HasUpdate   => Status == AppStatus.UpdateAvailable;
}
