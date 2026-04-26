using System.Text.Json;
using DesktopIcons.Core.Models;

namespace DesktopIcons.Core.Storage;

public static class LayoutStore
{
    public sealed record StoredLayout(string Name, LayoutFile? Layout);
    public sealed record FingerprintGroup(string Fingerprint, string? MonitorSetup, List<StoredLayout> Layouts);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string LayoutsRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DesktopIcons",
            "layouts");

    private static string FingerprintDir(string fingerprint) =>
        Path.Combine(LayoutsRoot, fingerprint);

    private static string LayoutPath(string fingerprint, string name) =>
        Path.Combine(FingerprintDir(fingerprint), name + ".json");

    public static string? ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "layout name cannot be empty";
        if (name.Length > 64)
            return "layout name is too long (max 64 chars)";
        if (name.StartsWith('.'))
            return "layout name cannot start with '.'";

        foreach (var c in name)
        {
            if (c < 32)
                return "layout name contains a control character";
            if ("<>:\"/\\|?*".IndexOf(c) >= 0)
                return $"layout name contains illegal character: '{c}'";
        }

        var bare = name.Split('.')[0].ToUpperInvariant();
        var reserved = new[]
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };
        if (Array.IndexOf(reserved, bare) >= 0)
            return $"layout name '{name}' is reserved by Windows";

        return null;
    }

    public static string Save(string fingerprint, string name, LayoutFile layout)
    {
        var dir = FingerprintDir(fingerprint);
        Directory.CreateDirectory(dir);
        var path = LayoutPath(fingerprint, name);
        File.WriteAllText(path, JsonSerializer.Serialize(layout, JsonOpts));
        return path;
    }

    public static LayoutFile? Load(string fingerprint, string name)
    {
        var path = LayoutPath(fingerprint, name);
        if (!File.Exists(path)) return null;
        return JsonSerializer.Deserialize<LayoutFile>(File.ReadAllText(path), JsonOpts);
    }

    public static bool Delete(string fingerprint, string name)
    {
        var path = LayoutPath(fingerprint, name);
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }

    public static List<StoredLayout> List(string fingerprint)
    {
        var dir = FingerprintDir(fingerprint);
        if (!Directory.Exists(dir)) return new List<StoredLayout>();
        return Directory.EnumerateFiles(dir, "*.json")
            .Select(p => new StoredLayout(Path.GetFileNameWithoutExtension(p), TryLoad(p)))
            .OrderBy(l => l.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<FingerprintGroup> ListAll()
    {
        if (!Directory.Exists(LayoutsRoot)) return new List<FingerprintGroup>();

        var groups = new List<FingerprintGroup>();
        foreach (var dir in Directory.EnumerateDirectories(LayoutsRoot)
                     .OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
        {
            var fp = Path.GetFileName(dir);
            var layouts = Directory.EnumerateFiles(dir, "*.json")
                .Select(p => new StoredLayout(Path.GetFileNameWithoutExtension(p), TryLoad(p)))
                .OrderBy(l => l.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var setup = layouts
                .Select(l => l.Layout?.MonitorSetup)
                .FirstOrDefault(s => !string.IsNullOrEmpty(s));

            groups.Add(new FingerprintGroup(fp, setup, layouts));
        }
        return groups;
    }

    private static LayoutFile? TryLoad(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<LayoutFile>(File.ReadAllText(path), JsonOpts);
        }
        catch
        {
            return null;
        }
    }
}
