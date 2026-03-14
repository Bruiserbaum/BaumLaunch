using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace BaumLaunch.Services;

/// <param name="Name">Display name from the Name column (used for fallback matching of non-WinGet installs).</param>
public sealed record WinGetEntry(string Id, string InstalledVersion, string AvailableVersion, string Source = "", string Name = "");

public static class WinGetService
{
    private static async Task<string> RunWinGetAsync(string args, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo("winget", args)
            {
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding  = Encoding.UTF8,
            };
            using var proc = Process.Start(psi) ?? throw new Exception("Failed to start winget");
            string output = await proc.StandardOutput.ReadToEndAsync(ct);
            await proc.WaitForExitAsync(ct);
            return output;
        }
        catch { return ""; }
    }

    /// <summary>
    /// Returns apps whose Source column == "winget" (i.e. installed/managed by WinGet).
    /// Uses plain "winget list" (local, no network) and filters by Source in code so we never
    /// depend on a --source winget index download that can silently time-out and return nothing.
    /// </summary>
    public static async Task<List<WinGetEntry>> GetWinGetManagedAsync(CancellationToken ct = default)
    {
        var output = await RunWinGetAsync("list --accept-source-agreements --disable-interactivity", ct);
        return ParseTable(output)
            .Where(e => e.Source.Equals("winget", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Returns all installed apps (WinGet + ARP/system installs).
    /// Used to detect apps installed outside WinGet that WinGet can correlate to a package ID.
    /// </summary>
    public static async Task<List<WinGetEntry>> GetInstalledAsync(CancellationToken ct = default)
    {
        var output = await RunWinGetAsync("list --accept-source-agreements", ct);
        return ParseTable(output);
    }

    /// <summary>
    /// Checks whether a specific package is installed (by any means, not just WinGet).
    /// More reliable than the full list for per-app detection because it asks about one ID exactly.
    /// </summary>
    public static async Task<WinGetEntry?> CheckSingleAsync(string id, CancellationToken ct = default)
    {
        var output = await RunWinGetAsync(
            $"list --id {id} --exact --accept-source-agreements --disable-interactivity", ct);
        return ParseTable(output).FirstOrDefault();
    }

    public static async Task<List<WinGetEntry>> GetUpgradableAsync(CancellationToken ct = default)
    {
        var output = await RunWinGetAsync("upgrade --source winget --accept-source-agreements", ct);
        return ParseTable(output);
    }

    public static async Task<bool> InstallOrUpgradeAsync(
        string id, bool isInstalled, Action<string>? onOutput = null,
        bool force = false, CancellationToken ct = default)
    {
        string verb  = isInstalled ? "upgrade" : "install";
        // --disable-interactivity suppresses winget's own prompts without passing /S to the installer
        // (--silent fails on some packages that don't support silent-mode installation)
        // --force (optional) makes winget reinstall even if already at latest version — used when
        // a prior uninstall failed so we need winget to take ownership of an existing ARP install.
        string force_ = force ? " --force" : "";
        string args  = $"{verb} --id {id} --exact --disable-interactivity --accept-package-agreements --accept-source-agreements{force_}";

        try
        {
            var psi = new ProcessStartInfo("winget", args)
            {
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true,
                StandardOutputEncoding = Encoding.UTF8,
            };
            using var proc = Process.Start(psi) ?? throw new Exception("Failed to start winget");

            proc.OutputDataReceived += (_, e) => { if (e.Data != null) onOutput?.Invoke(e.Data); };
            proc.BeginOutputReadLine();

            await proc.WaitForExitAsync(ct);
            // 0 = success; 3010 = success (reboot required); -1978335189 = already up-to-date
            return proc.ExitCode == 0 || proc.ExitCode == 3010 || proc.ExitCode == -1978335189;
        }
        catch { return false; }
    }

    public static async Task<bool> UninstallAsync(
        string id, Action<string>? onOutput = null, CancellationToken ct = default)
    {
        string args = $"uninstall --id {id} --exact --silent --accept-source-agreements";
        try
        {
            var psi = new ProcessStartInfo("winget", args)
            {
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true,
                StandardOutputEncoding = Encoding.UTF8,
            };
            using var proc = Process.Start(psi) ?? throw new Exception("Failed to start winget");
            proc.OutputDataReceived += (_, e) => { if (e.Data != null) onOutput?.Invoke(e.Data); };
            proc.BeginOutputReadLine();
            await proc.WaitForExitAsync(ct);
            return proc.ExitCode == 0;
        }
        catch { return false; }
    }

    private static List<WinGetEntry> ParseTable(string output)
    {
        var results = new List<WinGetEntry>();
        if (string.IsNullOrWhiteSpace(output)) return results;

        // Remove ANSI escape codes and non-printable chars
        output = Regex.Replace(output, @"\x1B\[[0-9;]*[mK]", "");
        output = Regex.Replace(output, @"[^\x09\x0A\x0D\x20-\x7E]", "");

        var lines = output.Split('\n');

        // Find header line (must contain Id, Version, Name columns)
        int headerIdx = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            var l = lines[i];
            if (l.Contains("Id") && l.Contains("Version") && l.Contains("Name"))
            {
                headerIdx = i;
                break;
            }
        }
        if (headerIdx < 0) return results;

        var header   = lines[headerIdx];
        int idCol    = FindColIdx(header, "Id");
        int verCol   = FindColIdx(header, "Version");
        int availCol = FindColIdx(header, "Available");
        int srcCol   = FindColIdx(header, "Source");

        if (idCol < 0 || verCol < 0) return results;

        // Skip separator line (dashes)
        int dataStart = headerIdx + 1;
        if (dataStart < lines.Length && lines[dataStart].TrimStart().StartsWith('-'))
            dataStart++;

        for (int i = dataStart; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.TrimStart().StartsWith('-')) continue;

            // Column boundaries
            int idEnd    = verCol > idCol ? verCol : line.Length;
            int verEnd   = availCol > verCol ? availCol : (srcCol > verCol ? srcCol : line.Length);
            int availEnd = srcCol > availCol && availCol >= 0 ? srcCol : line.Length;

            // Name lives from position 0 to the start of the Id column
            string name      = idCol > 0 ? SafeSubstring(line, 0, idCol).Trim() : "";
            string id        = SafeSubstring(line, idCol, idEnd).Trim();
            string version   = SafeSubstring(line, verCol, verEnd).Trim();
            string available = availCol >= 0 ? SafeSubstring(line, availCol, availEnd).Trim() : "";
            string source    = srcCol  >= 0 ? SafeSubstring(line, srcCol, line.Length).Trim() : "";

            // Skip junk/summary lines
            if (string.IsNullOrWhiteSpace(id)) continue;
            if (id.Equals("Id", StringComparison.OrdinalIgnoreCase)) continue;
            if (id.StartsWith("upgrades", StringComparison.OrdinalIgnoreCase)) continue;
            if (version.Contains("packages", StringComparison.OrdinalIgnoreCase)) continue;
            if (string.IsNullOrWhiteSpace(version) && string.IsNullOrWhiteSpace(name)) continue;

            // Accept entry if:
            //  • ID looks like a WinGet package ID (contains '.') — used for ID-based lookup
            //  • OR it has a meaningful display name + version — kept for name-based fallback matching
            bool hasWinGetId = id.Contains('.');
            bool hasNameInfo  = !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(version);
            if (!hasWinGetId && !hasNameInfo) continue;

            results.Add(new WinGetEntry(id, version, available, source, name));
        }

        return results;
    }

    private static int FindColIdx(string header, string colName)
    {
        return header.IndexOf(colName, StringComparison.OrdinalIgnoreCase);
    }

    private static string SafeSubstring(string s, int start, int end)
    {
        if (start < 0 || start >= s.Length) return "";
        if (end > s.Length) end = s.Length;
        if (end <= start) return "";
        return s[start..end];
    }
}
