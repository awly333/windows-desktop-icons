using DesktopIcons.Core.Storage;
using Microsoft.Win32;

namespace DesktopIcons.App.Services;

public sealed class AutoRestoreService : IDisposable
{
    private const int DebounceMs = 500;

    private readonly LayoutService _layoutService;
    private readonly Func<AppSettings> _getSettings;
    private readonly System.Threading.Timer _timer;
    private bool _enabled;
    private bool _hooked;

    public AutoRestoreService(LayoutService layoutService, Func<AppSettings> getSettings)
    {
        _layoutService = layoutService;
        _getSettings = getSettings;
        _timer = new System.Threading.Timer(OnDebounceFire, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void SetEnabled(bool enabled)
    {
        if (_enabled == enabled) return;
        _enabled = enabled;

        if (enabled && !_hooked)
        {
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            _hooked = true;
        }
        else if (!enabled && _hooked)
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            _hooked = false;
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        _timer.Change(DebounceMs, Timeout.Infinite);
    }

    private async void OnDebounceFire(object? state)
    {
        if (!_enabled) return;
        try
        {
            var fp = await _layoutService.GetCurrentFingerprintAsync();
            var settings = _getSettings();
            if (!settings.LastLayoutByFingerprint.TryGetValue(fp.Fingerprint, out var name))
                return;
            await _layoutService.RestoreAsync(fp.Fingerprint, name);
        }
        catch
        {
            // best-effort; auto-restore must never crash the app
        }
    }

    public void Dispose()
    {
        if (_hooked)
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            _hooked = false;
        }
        _timer.Dispose();
    }
}
