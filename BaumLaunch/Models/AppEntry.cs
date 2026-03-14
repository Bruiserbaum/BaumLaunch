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
    public bool      IsSelected       { get; set; } = true;  // for profile/batch ops
    /// <summary>True when the installed copy was installed via WinGet (Source = "winget").</summary>
    public bool      IsWinGetManaged  { get; set; } = false;

    public bool IsInstalled => InstalledVersion != null;
    public bool HasUpdate   => Status == AppStatus.UpdateAvailable;
}
