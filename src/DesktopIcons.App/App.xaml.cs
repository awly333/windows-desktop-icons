using DesktopIcons.App.Services;
using DesktopIcons.Core;
using Microsoft.UI.Xaml;
using System.Text;

namespace DesktopIcons.App;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        Dpi.EnsurePerMonitorV2();
        InitializeComponent();
        UnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            var window = new MainWindow();
            _window = window;
            window.Activate();
            if (AutoStartService.LaunchedMinimized())
            {
                window.HideToTray();
            }
        }
        catch (Exception ex)
        {
            StartupLog.Write("OnLaunched", ex);
            throw;
        }
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        StartupLog.Write("Application.UnhandledException", e.Exception);
    }

    private void OnCurrentDomainUnhandledException(object? sender, System.UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            StartupLog.Write("AppDomain.CurrentDomain.UnhandledException", ex);
        }
        else
        {
            StartupLog.WriteRaw("AppDomain.CurrentDomain.UnhandledException", e.ExceptionObject?.ToString() ?? "<null>");
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        StartupLog.Write("TaskScheduler.UnobservedTaskException", e.Exception);
    }
}

internal static class StartupLog
{
    private static readonly string LogPath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DesktopIcons",
            "logs",
            "startup.log");

    public static void Write(string source, Exception ex)
    {
        WriteRaw(source, ex.ToString());
    }

    public static void WriteRaw(string source, string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var sb = new StringBuilder();
            sb.Append('[')
              .Append(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"))
              .Append("] ")
              .Append(source)
              .AppendLine();
            sb.AppendLine(message);
            sb.AppendLine(new string('-', 80));

            File.AppendAllText(LogPath, sb.ToString(), Encoding.UTF8);
        }
        catch
        {
            // Best-effort logging only.
        }
    }
}
