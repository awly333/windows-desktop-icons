using System.Text.Json.Serialization;

namespace DesktopIcons.Core.Models;

public sealed class LayoutFile
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("capturedAt")]
    public DateTime CapturedAt { get; set; }

    [JsonPropertyName("monitorFingerprint")]
    public string? MonitorFingerprint { get; set; }

    [JsonPropertyName("monitorSetup")]
    public string? MonitorSetup { get; set; }

    [JsonPropertyName("monitors")]
    public List<MonitorRect>? Monitors { get; set; }

    [JsonPropertyName("icons")]
    public List<IconInfo> Icons { get; set; } = new();
}

public sealed class MonitorRect
{
    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("w")]
    public int W { get; set; }

    [JsonPropertyName("h")]
    public int H { get; set; }

    [JsonPropertyName("primary")]
    public bool Primary { get; set; }

    [JsonPropertyName("workX")]
    public int? WorkX { get; set; }

    [JsonPropertyName("workY")]
    public int? WorkY { get; set; }

    [JsonPropertyName("workW")]
    public int? WorkW { get; set; }

    [JsonPropertyName("workH")]
    public int? WorkH { get; set; }
}

public sealed class IconInfo
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
