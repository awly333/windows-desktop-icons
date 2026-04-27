using DesktopIcons.Core.Storage;

namespace DesktopIcons.App.Services;

public sealed class AutoRestoreService : IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan SuccessCooldown = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan RetryCooldown = TimeSpan.FromSeconds(20);

    private readonly LayoutService _layoutService;
    private readonly Func<AppSettings> _getSettings;
    private readonly System.Threading.Timer _timer;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _enabled;
    private bool _disposed;
    private DateTime _nextCheckUtc = DateTime.MinValue;

    public AutoRestoreService(LayoutService layoutService, Func<AppSettings> getSettings)
    {
        _layoutService = layoutService;
        _getSettings = getSettings;
        _timer = new System.Threading.Timer(OnTimerTick, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public void SetEnabled(bool enabled)
    {
        ThrowIfDisposed();
        if (_enabled == enabled) return;

        _enabled = enabled;
        _nextCheckUtc = DateTime.UtcNow;
        _timer.Change(enabled ? PollInterval : Timeout.InfiniteTimeSpan, enabled ? PollInterval : Timeout.InfiniteTimeSpan);
    }

    private void OnTimerTick(object? state)
    {
        if (!_enabled || _disposed) return;
        if (DateTime.UtcNow < _nextCheckUtc) return;
        if (!_gate.Wait(0)) return;

        _ = CheckAndRestoreAsync();
    }

    private async Task CheckAndRestoreAsync()
    {
        try
        {
            var settings = _getSettings();
            var fp = await _layoutService.GetCurrentFingerprintAsync();
            if (!settings.LastLayoutByFingerprint.TryGetValue(fp.Fingerprint, out var name) ||
                string.IsNullOrWhiteSpace(name))
            {
                _nextCheckUtc = DateTime.UtcNow + RetryCooldown;
                return;
            }

            var drift = await _layoutService.DetectDriftAsync(fp.Fingerprint, name);
            if (!drift.LayoutExists || drift.AutoArrangeOn)
            {
                _nextCheckUtc = DateTime.UtcNow + RetryCooldown;
                return;
            }

            if (!drift.HasDrift)
            {
                _nextCheckUtc = DateTime.UtcNow + PollInterval;
                return;
            }

            await _layoutService.RestoreAsync(fp.Fingerprint, name);
            _nextCheckUtc = DateTime.UtcNow + SuccessCooldown;
        }
        catch
        {
            _nextCheckUtc = DateTime.UtcNow + RetryCooldown;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Dispose();
        _gate.Dispose();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
