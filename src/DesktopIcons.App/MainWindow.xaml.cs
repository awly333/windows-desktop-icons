using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using DesktopIcons.App.Dialogs;
using DesktopIcons.App.Services;
using DesktopIcons.App.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Graphics;
using WinRT.Interop;

namespace DesktopIcons.App;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; } = new();
    public SettingsViewModel Settings { get; } = new();
    public ICommand ToggleTrayCommand { get; }
    public ICommand ToggleStartWithWindowsCommand { get; }
    public ICommand ToggleCloseToTrayCommand { get; }
    public ICommand ToggleAutoRestoreCommand { get; }
    public ICommand QuitCommand { get; }

    private AppWindow? _appWindow;
    private IntPtr _hwnd;
    private IntPtr _hTrayIcon;
    private bool _isExiting;
    private readonly AutoRestoreService _autoRestore;

    public MainWindow()
    {
        ToggleTrayCommand = new RelayCommand(ToggleMainWindow);
        ToggleStartWithWindowsCommand = new RelayCommand(() => Settings.StartWithWindows = !Settings.StartWithWindows);
        ToggleCloseToTrayCommand = new RelayCommand(() => Settings.CloseToTray = !Settings.CloseToTray);
        ToggleAutoRestoreCommand = new RelayCommand(() => Settings.AutoRestoreWhenIconsMove = !Settings.AutoRestoreWhenIconsMove);
        QuitCommand = new RelayCommand(QuitApp);
        InitializeComponent();
        Title = "Desktop Icons";

        ConfigureWindow();

        ViewModel.LayoutRestored += OnLayoutRestored;
        _autoRestore = new AutoRestoreService(ViewModel.Service, () => Settings.Snapshot);
        _autoRestore.SetEnabled(Settings.AutoRestoreWhenIconsMove);
        Settings.PropertyChanged += OnSettingsChanged;

        _ = ViewModel.InitializeAsync();
    }

    private void OnLayoutRestored(object? sender, (string Fingerprint, string Name) e)
    {
        Settings.RecordLastLayout(e.Fingerprint, e.Name);
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.AutoRestoreWhenIconsMove))
        {
            _autoRestore.SetEnabled(Settings.AutoRestoreWhenIconsMove);
        }
    }

    private void ConfigureWindow()
    {
        _hwnd = WindowNative.GetWindowHandle(this);
        var id = Win32Interop.GetWindowIdFromWindow(_hwnd);
        _appWindow = AppWindow.GetFromWindowId(id);

        var scale = GetDpiScale(_hwnd);
        _appWindow.Resize(new SizeInt32((int)(560 * scale), (int)(640 * scale)));

        ApplyIcons();

        _appWindow.Closing += AppWindow_Closing;
    }

    private void ApplyIcons()
    {
        var icoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        if (!File.Exists(icoPath)) return;

        _appWindow?.SetIcon(icoPath);

        // H.NotifyIcon's ImageSource→SoftwareBitmap→HICON conversion is unreliable
        // in unpackaged WinUI 3 (sometimes blank, sometimes stale). Bypass it: load
        // an HICON via Win32 LoadImage and push it directly into the inner TrayIcon.
        _hTrayIcon = LoadImage(IntPtr.Zero, icoPath, IMAGE_ICON, 0, 0,
                               LR_LOADFROMFILE | LR_DEFAULTSIZE);
        if (_hTrayIcon == IntPtr.Zero) return;

        TrayIcon.Loaded += OnTrayIconLoaded;
    }

    private void OnTrayIconLoaded(object sender, RoutedEventArgs e)
    {
        if (_hTrayIcon == IntPtr.Zero) return;
        try
        {
            if (TrayIcon.TrayIcon is { IsCreated: false })
            {
                TrayIcon.ForceCreate(enablesEfficiencyMode: false);
            }
            TrayIcon.TrayIcon?.UpdateIcon(_hTrayIcon);
        }
        catch { }
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_isExiting) return;
        if (Settings.CloseToTray)
        {
            args.Cancel = true;
            sender.Hide();
        }
    }

    private void ToggleMainWindow()
    {
        if (_appWindow is null) return;
        if (_appWindow.IsVisible)
        {
            _appWindow.Hide();
        }
        else
        {
            _appWindow.Show();
            SetForegroundWindow(_hwnd);
        }
    }

    public void HideToTray() => _appWindow?.Hide();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadImage(IntPtr hInst, string name, uint type,
                                           int cx, int cy, uint fuLoad);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x00000010;
    private const uint LR_DEFAULTSIZE = 0x00000040;

    private static double GetDpiScale(IntPtr hwnd)
    {
        var dpi = NativeDpi.GetDpiForWindow(hwnd);
        return dpi <= 0 ? 1.0 : dpi / 96.0;
    }

    private async void NewLayout_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new NameInputDialog("Save desktop layout", "Give it a name. You can restore to this layout later.")
        {
            XamlRoot = Content.XamlRoot
        };
        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return;

        var name = dialog.EnteredName;
        if (NameExists(name) && !await ConfirmOverwriteAsync(name)) return;
        await ViewModel.SaveNewCommand.ExecuteAsync(name);
    }

    private void Card_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is LayoutItemViewModel item)
        {
            var wasExpanded = item.IsExpanded;
            foreach (var l in ViewModel.Layouts) l.IsExpanded = false;
            item.IsExpanded = !wasExpanded;
            ViewModel.SelectedLayout = item.IsExpanded ? item : null;
        }
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is LayoutItemViewModel item)
        {
            var confirm = new ConfirmDialog(
                title: "Restore layout",
                message: $"Desktop icons will move to match “{item.Name}”. The current layout will be overwritten.",
                confirmText: "Restore",
                cancelText: "Cancel",
                destructive: true)
            {
                XamlRoot = Content.XamlRoot
            };
            var r = await confirm.ShowAsync();
            if (r == ContentDialogResult.Primary)
            {
                await ViewModel.RestoreCommand.ExecuteAsync(item);
            }
        }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is LayoutItemViewModel item)
        {
            var confirm = new ConfirmDialog(
                title: "Delete layout",
                message: $"Delete “{item.Name}”? This cannot be undone.",
                confirmText: "Delete",
                cancelText: "Cancel",
                destructive: true)
            {
                XamlRoot = Content.XamlRoot
            };
            var r = await confirm.ShowAsync();
            if (r == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteCommand.ExecuteAsync(item);
                if (!NameExists(item.Name))
                {
                    Settings.RemoveRecordedLayout(item.Fingerprint, item.Name);
                }
            }
        }
    }

    private async void Rename_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is LayoutItemViewModel item)
        {
            var dialog = new NameInputDialog("Rename layout", "Enter a new name.", item.Name)
            {
                XamlRoot = Content.XamlRoot
            };
            var r = await dialog.ShowAsync();
            if (r != ContentDialogResult.Primary) return;

            var newName = dialog.EnteredName;
            var oldName = item.Name;
            var isOtherDuplicate = ViewModel.Layouts.Any(
                l => !ReferenceEquals(l, item) &&
                     string.Equals(l.Name, newName, StringComparison.OrdinalIgnoreCase));
            if (isOtherDuplicate && !await ConfirmOverwriteAsync(newName)) return;
            await ViewModel.RenameCommand.ExecuteAsync((item, newName));
            if (!NameExists(oldName) && NameExists(newName))
            {
                Settings.RenameRecordedLayout(item.Fingerprint, oldName, newName);
            }
        }
    }

    private void QuitApp()
    {
        _isExiting = true;
        _autoRestore.Dispose();
        TrayIcon?.Dispose();
        if (_hTrayIcon != IntPtr.Zero)
        {
            DestroyIcon(_hTrayIcon);
            _hTrayIcon = IntPtr.Zero;
        }
        Application.Current.Exit();
    }

    private bool NameExists(string name) =>
        ViewModel.Layouts.Any(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));

    private async Task<bool> ConfirmOverwriteAsync(string name)
    {
        var confirm = new ConfirmDialog(
            title: "Overwrite layout",
            message: $"A layout named \"{name}\" already exists. Overwrite it?",
            confirmText: "Overwrite",
            cancelText: "Cancel",
            destructive: true)
        {
            XamlRoot = Content.XamlRoot
        };
        return await confirm.ShowAsync() == ContentDialogResult.Primary;
    }
}

internal static class NativeDpi
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern uint GetDpiForWindow(IntPtr hwnd);
}
