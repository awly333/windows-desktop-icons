using System.Text.Json;
using System.Text.Json.Serialization;

namespace DesktopIcons.Core.Storage;

public sealed class AppSettings
{
    public bool StartWithWindows { get; set; } = false;
    public bool CloseToTray { get; set; } = true;
    public bool AutoRestoreWhenIconsMove { get; set; } = false;

    [JsonPropertyName("autoRestoreOnDisplayChange")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? LegacyAutoRestoreOnDisplayChange { get; set; }

    public Dictionary<string, string> LastLayoutByFingerprint { get; set; } = new();

    public void Normalize()
    {
        if (!AutoRestoreWhenIconsMove && LegacyAutoRestoreOnDisplayChange == true)
        {
            AutoRestoreWhenIconsMove = true;
        }

        LegacyAutoRestoreOnDisplayChange = null;
    }
}

public static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string SettingsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DesktopIcons",
            "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new AppSettings();
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOpts) ?? new AppSettings();
            settings.Normalize();
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        settings.Normalize();
        var dir = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOpts));
    }
}
