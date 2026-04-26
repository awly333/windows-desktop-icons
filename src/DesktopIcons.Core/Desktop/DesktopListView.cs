using System.Runtime.InteropServices;
using System.Text;
using DesktopIcons.Core.Interop;

namespace DesktopIcons.Core.Desktop;

public sealed class DesktopListView : IDisposable
{
    public IntPtr Hwnd { get; }
    public IntPtr ProcessHandle { get; private set; }
    public uint ProcessId { get; }
    public bool IsAutoArrange { get; }

    private DesktopListView(IntPtr hwnd, IntPtr processHandle, uint pid, bool isAutoArrange)
    {
        Hwnd = hwnd;
        ProcessHandle = processHandle;
        ProcessId = pid;
        IsAutoArrange = isAutoArrange;
    }

    public static DesktopListView Open()
    {
        var hwnd = FindDesktopListView()
            ?? throw new InvalidOperationException(
                "Could not locate desktop SysListView32. Desktop icons may be hidden, or the shell is in an unsupported state.");

        if (NativeMethods.GetWindowThreadProcessId(hwnd, out var pid) == 0 || pid == 0)
        {
            throw new InvalidOperationException(
                $"GetWindowThreadProcessId failed: Win32={Marshal.GetLastWin32Error()}");
        }

        var hProc = NativeMethods.OpenProcess(ProcessAccess.ALL_FOR_REMOTE, false, pid);
        if (hProc == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"OpenProcess(pid={pid}) failed: Win32={Marshal.GetLastWin32Error()}. " +
                "If explorer.exe is elevated, run this tool elevated too.");
        }

        var style = NativeMethods.GetWindowLongPtr(hwnd, LvConstants.GWL_STYLE).ToInt64();
        var autoArrange = (style & LvConstants.LVS_AUTOARRANGE) != 0;

        return new DesktopListView(hwnd, hProc, pid, autoArrange);
    }

    private static IntPtr? FindDesktopListView()
    {
        var progman = NativeMethods.FindWindow("Progman", null);
        if (progman != IntPtr.Zero)
        {
            var defView = NativeMethods.FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (defView != IntPtr.Zero)
            {
                var lv = NativeMethods.FindWindowEx(defView, IntPtr.Zero, "SysListView32", null);
                if (lv != IntPtr.Zero) return lv;
            }
        }

        IntPtr found = IntPtr.Zero;
        NativeMethods.EnumWindows((h, _) =>
        {
            var sb = new StringBuilder(32);
            NativeMethods.GetClassName(h, sb, sb.Capacity);
            if (sb.ToString() != "WorkerW") return true;

            var defView = NativeMethods.FindWindowEx(h, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (defView == IntPtr.Zero) return true;

            var lv = NativeMethods.FindWindowEx(defView, IntPtr.Zero, "SysListView32", null);
            if (lv == IntPtr.Zero) return true;

            found = lv;
            return false;
        }, IntPtr.Zero);

        return found == IntPtr.Zero ? null : found;
    }

    public void Dispose()
    {
        if (ProcessHandle != IntPtr.Zero)
        {
            NativeMethods.CloseHandle(ProcessHandle);
            ProcessHandle = IntPtr.Zero;
        }
    }
}
