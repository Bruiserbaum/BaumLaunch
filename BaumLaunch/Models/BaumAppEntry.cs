namespace BaumLaunch.Models;

public enum BaumAppStatus { Unknown, NotInstalled, UpToDate, UpdateAvailable, Downloading, Installing, Updated, Failed }

public sealed class BaumAppEntry
{
    public string         RepoOwner        { get; set; } = "Bruiserbaum";
    public string         RepoName         { get; set; } = "";
    public string         DisplayName      { get; set; } = "";
    /// <summary>Expected exe path after install (per-user Inno default).</summary>
    public string         ExePath          { get; set; } = "";
    public Version?       InstalledVersion { get; set; }
    public Version?       LatestVersion    { get; set; }
    public string?        DownloadUrl      { get; set; }
    public BaumAppStatus  Status           { get; set; } = BaumAppStatus.Unknown;
    /// <summary>0–100 while downloading, -1 when unknown.</summary>
    public int            DownloadProgress { get; set; } = -1;

    public bool IsInstalled => InstalledVersion != null;
    public bool HasUpdate   => Status == BaumAppStatus.UpdateAvailable;
}
