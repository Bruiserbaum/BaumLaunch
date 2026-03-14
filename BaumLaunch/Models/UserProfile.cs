using System.Text.Json;

namespace BaumLaunch.Models;

public sealed class UserProfile
{
    public string       Name        { get; set; } = "My Setup";
    public DateTime     Created     { get; set; } = DateTime.UtcNow;
    public List<string> SelectedIds { get; set; } = new(); // WinGet IDs the user wants

    public static UserProfile FromEntries(IEnumerable<AppEntry> entries) => new()
    {
        Created     = DateTime.UtcNow,
        SelectedIds = entries.Where(e => e.IsSelected).Select(e => e.WinGetId).ToList(),
    };

    public void ApplyTo(IEnumerable<AppEntry> entries)
    {
        foreach (var e in entries)
            e.IsSelected = SelectedIds.Contains(e.WinGetId);
    }

    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

    public static UserProfile? FromJson(string json)
    {
        try { return JsonSerializer.Deserialize<UserProfile>(json); }
        catch { return null; }
    }
}
