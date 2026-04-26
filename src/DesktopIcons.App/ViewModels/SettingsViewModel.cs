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
        AutoRestoreOnDisplayChange = _settings.AutoRestoreOnDisplayChange;
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
    private bool _autoRestoreOnDisplayChange;

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

    partial void OnAutoRestoreOnDisplayChangeChanged(bool value)
    {
        _settings.AutoRestoreOnDisplayChange = value;
        Persist();
    }

    public void RecordLastLayout(string fingerprint, string name)
    {
        if (string.IsNullOrEmpty(fingerprint) || string.IsNullOrEmpty(name)) return;
        _settings.LastLayoutByFingerprint[fingerprint] = name;
        Persist();
    }

    private void Persist()
    {
        if (_suspendSave) return;
        try { SettingsStore.Save(_settings); } catch { }
    }
}
