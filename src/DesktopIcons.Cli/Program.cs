using System.Text.Json;
using DesktopIcons.Core;
using DesktopIcons.Core.Desktop;
using DesktopIcons.Core.Interop;
using DesktopIcons.Core.Models;
using DesktopIcons.Core.Storage;

namespace DesktopIcons.Cli;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static int Main(string[] args)
    {
        Dpi.EnsurePerMonitorV2();

        if (args.Length < 1)
        {
            PrintUsage();
            return 2;
        }

        var verb = args[0].ToLowerInvariant();

        try
        {
            return verb switch
            {
                "dump"    => RequireArg(args, 1, verb) ?? Dump(args[1]),
                "apply"   => RequireArg(args, 1, verb) ?? Apply(args[1]),
                "save"    => RequireArg(args, 1, verb) ?? Save(args[1]),
                "restore" => RequireArg(args, 1, verb) ?? Restore(args[1]),
                "list"    => List(args),
                "delete"  => RequireArg(args, 1, verb) ?? Delete(args[1]),
                _         => UnknownVerb(verb)
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }

    private static int? RequireArg(string[] args, int index, string verb)
    {
        if (args.Length <= index)
        {
            Console.Error.WriteLine($"error: '{verb}' requires an argument");
            PrintUsage();
            return 2;
        }
        return null;
    }

    private static LayoutFile CaptureLayout(string? name = null)
    {
        using var desktop = DesktopListView.Open();
        var icons = IconReader.ReadAll(desktop);
        var fp = MonitorFingerprint.Compute();
        var monitors = MonitorEnum.GetAll()
            .Select(m => new MonitorRect
            {
                X = m.Left, Y = m.Top, W = m.Width, H = m.Height, Primary = m.IsPrimary,
                WorkX = m.WorkLeft, WorkY = m.WorkTop, WorkW = m.WorkWidth, WorkH = m.WorkHeight
            })
            .ToList();
        return new LayoutFile
        {
            Version = 1,
            Name = name,
            CapturedAt = DateTime.UtcNow,
            MonitorFingerprint = fp.Fingerprint,
            MonitorSetup = fp.Setup,
            Monitors = monitors,
            Icons = icons
        };
    }

    private static int ApplyLayout(LayoutFile layout)
    {
        if (layout.Icons.Count == 0)
        {
            Console.Error.WriteLine("error: layout has no icons");
            return 1;
        }

        using var desktop = DesktopListView.Open();

        if (desktop.IsAutoArrange)
        {
            Console.Error.WriteLine(
                "warning: 'Auto arrange icons' is ON. Positions will be ignored by Explorer. " +
                "Right-click desktop → View → uncheck 'Auto arrange icons', then retry.");
        }

        var current = MonitorFingerprint.Compute();
        if (!string.IsNullOrEmpty(layout.MonitorFingerprint) &&
            layout.MonitorFingerprint != current.Fingerprint)
        {
            Console.Error.WriteLine(
                "warning: monitor configuration changed since this layout was captured.");
            Console.Error.WriteLine($"  saved:   {layout.MonitorFingerprint}  ({layout.MonitorSetup ?? "unknown"})");
            Console.Error.WriteLine($"  current: {current.Fingerprint}  ({current.Setup})");
            Console.Error.WriteLine("  applying anyway — icons targeting off-screen coords may end up clipped.");
        }

        var result = IconWriter.Apply(desktop, layout.Icons);
        Console.WriteLine($"moved {result.Moved} / {layout.Icons.Count} icons.");
        if (result.NotFound > 0)
        {
            Console.WriteLine($"not found on desktop ({result.NotFound}):");
            foreach (var label in result.MissingLabels)
            {
                Console.WriteLine($"  - {label}");
            }
        }

        return 0;
    }

    private static int Dump(string outPath)
    {
        var layout = CaptureLayout();
        File.WriteAllText(outPath, JsonSerializer.Serialize(layout, JsonOpts));

        Console.WriteLine($"dumped {layout.Icons.Count} icons → {outPath}");
        Console.WriteLine($"  fingerprint: {layout.MonitorFingerprint}  ({layout.MonitorSetup})");
        foreach (var icon in layout.Icons)
        {
            Console.WriteLine($"  [{icon.X,5},{icon.Y,5}]  {icon.Label}");
        }
        return 0;
    }

    private static int Apply(string inPath)
    {
        if (!File.Exists(inPath))
        {
            Console.Error.WriteLine($"error: file not found: {inPath}");
            return 1;
        }

        var layout = JsonSerializer.Deserialize<LayoutFile>(File.ReadAllText(inPath), JsonOpts);
        if (layout is null)
        {
            Console.Error.WriteLine("error: layout file is empty or invalid");
            return 1;
        }
        return ApplyLayout(layout);
    }

    private static int Save(string name)
    {
        var error = LayoutStore.ValidateName(name);
        if (error is not null)
        {
            Console.Error.WriteLine($"error: {error}");
            return 2;
        }

        var layout = CaptureLayout(name);
        var path = LayoutStore.Save(layout.MonitorFingerprint!, name, layout);

        Console.WriteLine($"saved '{name}' ({layout.Icons.Count} icons) → {path}");
        Console.WriteLine($"  fingerprint: {layout.MonitorFingerprint}  ({layout.MonitorSetup})");
        return 0;
    }

    private static int Restore(string name)
    {
        var error = LayoutStore.ValidateName(name);
        if (error is not null)
        {
            Console.Error.WriteLine($"error: {error}");
            return 2;
        }

        var current = MonitorFingerprint.Compute();
        var layout = LayoutStore.Load(current.Fingerprint, name);
        if (layout is null)
        {
            Console.Error.WriteLine(
                $"error: no layout named '{name}' for current fingerprint {current.Fingerprint}.");
            Console.Error.WriteLine(
                "  use `list` to see available layouts, or `list --all` for other configurations.");
            return 1;
        }
        return ApplyLayout(layout);
    }

    private static int List(string[] args)
    {
        if (args.Length >= 2 && args[1].Equals("--all", StringComparison.OrdinalIgnoreCase))
        {
            return ListAll();
        }

        var current = MonitorFingerprint.Compute();
        var layouts = LayoutStore.List(current.Fingerprint);
        if (layouts.Count == 0)
        {
            Console.WriteLine($"no layouts for {current.Fingerprint} ({current.Setup}).");
            Console.WriteLine(
                "use `save <name>` to create one, or `list --all` to see layouts for other monitor configurations.");
            return 0;
        }

        Console.WriteLine($"layouts for {current.Fingerprint} ({current.Setup}):");
        PrintLayouts(layouts);
        return 0;
    }

    private static int ListAll()
    {
        var groups = LayoutStore.ListAll();
        if (groups.Count == 0)
        {
            Console.WriteLine("no layouts stored. use `save <name>` to create one.");
            Console.WriteLine($"  storage root: {LayoutStore.LayoutsRoot}");
            return 0;
        }

        var current = MonitorFingerprint.Compute();
        foreach (var group in groups)
        {
            var marker = group.Fingerprint == current.Fingerprint ? " [current]" : "";
            var setup = group.MonitorSetup ?? "setup unknown";
            Console.WriteLine();
            Console.WriteLine($"{group.Fingerprint}{marker}  ({setup})");
            PrintLayouts(group.Layouts);
        }
        return 0;
    }

    private static void PrintLayouts(List<LayoutStore.StoredLayout> layouts)
    {
        if (layouts.Count == 0)
        {
            Console.WriteLine("  (empty)");
            return;
        }
        var nameWidth = layouts.Max(l => l.Name.Length);
        foreach (var entry in layouts)
        {
            var capturedAt = entry.Layout?.CapturedAt ?? default;
            var time = capturedAt == default
                ? "(unknown)"
                : capturedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            var count = entry.Layout?.Icons.Count ?? 0;
            Console.WriteLine($"  {entry.Name.PadRight(nameWidth)}  captured {time}  ({count} icons)");
        }
    }

    private static int Delete(string name)
    {
        var error = LayoutStore.ValidateName(name);
        if (error is not null)
        {
            Console.Error.WriteLine($"error: {error}");
            return 2;
        }

        var current = MonitorFingerprint.Compute();
        if (LayoutStore.Delete(current.Fingerprint, name))
        {
            Console.WriteLine($"deleted '{name}' from {current.Fingerprint}.");
            return 0;
        }

        Console.Error.WriteLine(
            $"error: no layout named '{name}' for current fingerprint {current.Fingerprint}.");
        return 1;
    }

    private static int UnknownVerb(string verb)
    {
        Console.Error.WriteLine($"error: unknown verb '{verb}'");
        PrintUsage();
        return 2;
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("usage:");
        Console.Error.WriteLine("  DesktopIcons.Cli save    <name>     save current desktop as a named layout");
        Console.Error.WriteLine("  DesktopIcons.Cli restore <name>     apply a named layout");
        Console.Error.WriteLine("  DesktopIcons.Cli list    [--all]    list layouts (--all: across all monitor configs)");
        Console.Error.WriteLine("  DesktopIcons.Cli delete  <name>     delete a named layout");
        Console.Error.WriteLine("  DesktopIcons.Cli dump    <path>     dump current desktop to a specific json file");
        Console.Error.WriteLine("  DesktopIcons.Cli apply   <path>     apply from a specific json file");
    }
}
