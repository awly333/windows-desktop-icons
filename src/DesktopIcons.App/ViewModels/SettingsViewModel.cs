using CommunityToolkit.Mvvm.ComponentModel;
using DesktopIcons.App.Services;
using DesktopIcons.Core.Storage;

namespace DesktopIcons.App.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly AppSettings _settings;
    private bool _suspendSave;

    public SettingsViewModel()
    {
        _settings = SettingsStore.Load();
        _suspendSave = true;
        StartWithWindows = AutoStartService.IsEnabled();
        CloseToTray = _settings.CloseToTray;
        AutoRestoreWhenIconsMove = _settings.AutoRestoreWhenIconsMove;
        _suspendSave = false;

        if (_settings.StartWithWindows != StartWithWindows)
        {
            _settings.StartWithWindows = StartWithWindows;
            try { SettingsStore.Save(_settings); } catch { }
        }
    }

    public AppSettings Snapshot => _settings;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _closeToTray;

    [ObservableProperty]
    private bool _autoRestoreWhenIconsMove;

    partial void OnStartWithWindowsChanged(bool value)
    {
        _settings.StartWithWindows = value;
        AutoStartService.SetEnabled(value);
        Persist();
    }

    partial void OnCloseToTrayChanged(bool value)
    {
        _settings.CloseToTray = value;
        Persist();
    }

    partial void OnAutoRestoreWhenIconsMoveChanged(bool value)
    {
        _settings.AutoRestoreWhenIconsMove = value;
        Persist();
    }

    public void RecordLastLayout(string fingerprint, string name)
    {
        if (string.IsNullOrEmpty(fingerprint) || string.IsNullOrEmpty(name)) return;
        _settings.LastLayoutByFingerprint[fingerprint] = name;
        Persist();
    }

    public void RenameRecordedLayout(string fingerprint, string oldName, string newName)
    {
        if (string.IsNullOrEmpty(fingerprint) || string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName)) return;
        if (_settings.LastLayoutByFingerprint.TryGetValue(fingerprint, out var current) &&
            string.Equals(current, oldName, StringComparison.OrdinalIgnoreCase))
        {
            _settings.LastLayoutByFingerprint[fingerprint] = newName;
            Persist();
        }
    }

    public void RemoveRecordedLayout(string fingerprint, string name)
    {
        if (string.IsNullOrEmpty(fingerprint) || string.IsNullOrEmpty(name)) return;
        if (_settings.LastLayoutByFingerprint.TryGetValue(fingerprint, out var current) &&
            string.Equals(current, name, StringComparison.OrdinalIgnoreCase))
        {
            _settings.LastLayoutByFingerprint.Remove(fingerprint);
            Persist();
        }
    }

    private void Persist()
    {
        if (_suspendSave) return;
        try { SettingsStore.Save(_settings); } catch { }
    }
}
