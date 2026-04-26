using DesktopIcons.Core.Desktop;
using DesktopIcons.Core.Interop;
using DesktopIcons.Core.Models;
using DesktopIcons.Core.Storage;

namespace DesktopIcons.App.Services;

public sealed record FingerprintInfo(string Fingerprint, string Setup, string DisplaySummary);

public sealed record RestoreResult(int Moved, int Total, int NotFound, List<string> Missing, bool AutoArrangeOn);

public sealed class LayoutService
{
    public Task<FingerprintInfo> GetCurrentFingerprintAsync() =>
        Task.Run(() =>
        {
            var fp = MonitorFingerprint.Compute();
            var monitors = MonitorEnum.GetAll();
            return new FingerprintInfo(fp.Fingerprint, fp.Setup, BuildDisplaySummary(monitors));
        });

    private static string BuildDisplaySummary(IReadOnlyList<MonitorInfo> monitors)
    {
        if (monitors.Count == 0) return "No display";
        var primary = monitors.FirstOrDefault(m => m.IsPrimary);
        if (primary == default) primary = monitors[0];

        var res = $"{primary.Width} × {primary.Height}";
        return monitors.Count switch
        {
            1 => res,
            2 => $"Two displays · {res}",
            3 => $"Three displays · {res}",
            _ => $"{monitors.Count} displays · {res}"
        };
    }

    public Task<List<LayoutStore.FingerprintGroup>> ListAllAsync() =>
        Task.Run(() => LayoutStore.ListAll());

    public Task<List<LayoutStore.StoredLayout>> ListForFingerprintAsync(string fingerprint) =>
        Task.Run(() => LayoutStore.List(fingerprint));

    public Task<LayoutFile> SaveCurrentAsync(string name) =>
        Task.Run(() =>
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
            var layout = new LayoutFile
            {
                Version = 1,
                Name = name,
                CapturedAt = DateTime.UtcNow,
                MonitorFingerprint = fp.Fingerprint,
                MonitorSetup = fp.Setup,
                Monitors = monitors,
                Icons = icons
            };
            LayoutStore.Save(fp.Fingerprint, name, layout);
            return layout;
        });

    public Task<RestoreResult> RestoreAsync(string fingerprint, string name) =>
        Task.Run(() =>
        {
            var layout = LayoutStore.Load(fingerprint, name)
                ?? throw new InvalidOperationException($"layout '{name}' not found.");

            using var desktop = DesktopListView.Open();
            var autoArrange = desktop.IsAutoArrange;
            var result = IconWriter.Apply(desktop, layout.Icons);
            return new RestoreResult(result.Moved, layout.Icons.Count, result.NotFound, result.MissingLabels.ToList(), autoArrange);
        });

    public Task<bool> DeleteAsync(string fingerprint, string name) =>
        Task.Run(() => LayoutStore.Delete(fingerprint, name));

    public Task RenameAsync(string fingerprint, string oldName, string newName) =>
        Task.Run(() =>
        {
            var layout = LayoutStore.Load(fingerprint, oldName)
                ?? throw new InvalidOperationException($"layout '{oldName}' not found.");
            layout.Name = newName;
            LayoutStore.Save(fingerprint, newName, layout);
            if (!string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
            {
                LayoutStore.Delete(fingerprint, oldName);
            }
        });

    public static string? ValidateName(string name) => LayoutStore.ValidateName(name);
}
