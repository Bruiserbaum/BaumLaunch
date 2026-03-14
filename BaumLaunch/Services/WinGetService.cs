using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace BaumLaunch.Services;

public sealed record WinGetEntry(string Id, string InstalledVersion, string AvailableVersion);

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

    public static async Task<List<WinGetEntry>> GetInstalledAsync(CancellationToken ct = default)
    {
        var output = await RunWinGetAsync("list --source winget --accept-source-agreements", ct);
        return ParseTable(output);
    }

    public static async Task<List<WinGetEntry>> GetUpgradableAsync(CancellationToken ct = default)
    {
        var output = await RunWinGetAsync("upgrade --source winget --accept-source-agreements", ct);
        return ParseTable(output);
    }

    public static async Task<bool> InstallOrUpgradeAsync(
        string id, bool isInstalled, Action<string>? onOutput = null, CancellationToken ct = default)
    {
        string verb = isInstalled ? "upgrade" : "install";
        string args = $"{verb} --id {id} --exact --silent --accept-package-agreements --accept-source-agreements";

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

        // Remove ANSI escape codes
        output = Regex.Replace(output, @"\x1B\[[0-9;]*[mK]", "");
        // Remove non-printable chars except newline/CR/tab
        output = Regex.Replace(output, @"[^\x09\x0A\x0D\x20-\x7E]", "");

        var lines = output.Split('\n');

        // Find header line (contains "Id" and "Version" and "Name")
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

        var header = lines[headerIdx];
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

            // Determine column end boundaries
            int idEnd    = verCol > idCol ? verCol : line.Length;
            int verEnd   = availCol > verCol ? availCol : (srcCol > verCol ? srcCol : line.Length);
            int availEnd = srcCol > availCol && availCol >= 0 ? srcCol : line.Length;

            string id        = SafeSubstring(line, idCol, idEnd).Trim();
            string version   = SafeSubstring(line, verCol, verEnd).Trim();
            string available = availCol >= 0 ? SafeSubstring(line, availCol, availEnd).Trim() : "";

            // Skip entries without a valid winget ID (must contain a dot)
            if (string.IsNullOrWhiteSpace(id) || !id.Contains('.')) continue;
            // Skip header repeat lines
            if (id.Equals("Id", StringComparison.OrdinalIgnoreCase)) continue;
            // Skip lines that are just summary text
            if (id.StartsWith("upgrades", StringComparison.OrdinalIgnoreCase)) continue;
            if (version.Contains("packages", StringComparison.OrdinalIgnoreCase)) continue;

            results.Add(new WinGetEntry(id, version, available));
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
