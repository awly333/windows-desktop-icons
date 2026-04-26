using DesktopIcons.App.Services;
using DesktopIcons.Core;
using Microsoft.UI.Xaml;

namespace DesktopIcons.App;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        Dpi.EnsurePerMonitorV2();
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var window = new MainWindow();
        _window = window;
        window.Activate();
        if (AutoStartService.LaunchedMinimized())
        {
            window.HideToTray();
        }
    }
}
