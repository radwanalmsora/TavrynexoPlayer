using System.IO;
using System.Text.Json;

namespace TavrynexoPlayer.Services;

public sealed class AppSettings
{
    public int Volume { get; set; } = 80;
    public bool Muted { get; set; }
    public bool Shuffle { get; set; }
    public int RepeatMode { get; set; }          // 0 off, 1 all, 2 one
    public bool ShowPlaylist { get; set; }
    public List<string> Recent { get; set; } = new();
    public Dictionary<string, long> Positions { get; set; } = new();
}

public static class SettingsService
{
    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TavrynexoPlayer");
    private static readonly string FilePath = Path.Combine(Dir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch { /* corrupted settings: start fresh */ }
        return new AppSettings();
    }

    public static void Save(AppSettings s)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* ignore */ }
    }
}
