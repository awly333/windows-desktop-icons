using System.Diagnostics;
using Microsoft.Win32;

namespace DesktopIcons.App.Services;

public static class AutoStartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DesktopIcons";
    private const string MinimizedArg = "--minimized";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(ValueName) is string;
        }
        catch
        {
            return false;
        }
    }

    public static void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (key is null) return;
            if (enabled)
            {
                var exe = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exe)) return;
                var command = $"\"{exe}\" {MinimizedArg}";
                key.SetValue(ValueName, command, RegistryValueKind.String);
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // best-effort; surface via UI in a future iteration
        }
    }

    public static bool LaunchedMinimized() =>
        Environment.GetCommandLineArgs()
            .Any(a => string.Equals(a, MinimizedArg, StringComparison.OrdinalIgnoreCase));
}
