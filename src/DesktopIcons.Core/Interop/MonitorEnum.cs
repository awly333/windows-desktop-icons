using System.Runtime.InteropServices;

namespace DesktopIcons.Core.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct MONITORINFOEXW
{
    public uint cbSize;
    public RECT rcMonitor;
    public RECT rcWork;
    public uint dwFlags;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string szDevice;

    public const uint MONITORINFOF_PRIMARY = 0x00000001;
}

public readonly record struct MonitorInfo(
    int Width, int Height, int Left, int Top, bool IsPrimary, string Device,
    int WorkLeft, int WorkTop, int WorkWidth, int WorkHeight);

public static class MonitorEnum
{
    private const string User32 = "user32.dll";

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport(User32, EntryPoint = "GetMonitorInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEXW lpmi);

    public static List<MonitorInfo> GetAll()
    {
        var list = new List<MonitorInfo>();
        var mySize = (uint)Marshal.SizeOf<MONITORINFOEXW>();

        bool Callback(IntPtr hMonitor, IntPtr _, ref RECT __, IntPtr ___)
        {
            var info = new MONITORINFOEXW { cbSize = mySize, szDevice = string.Empty };
            if (!GetMonitorInfo(hMonitor, ref info)) return true;

            list.Add(new MonitorInfo(
                Width: info.rcMonitor.Right - info.rcMonitor.Left,
                Height: info.rcMonitor.Bottom - info.rcMonitor.Top,
                Left: info.rcMonitor.Left,
                Top: info.rcMonitor.Top,
                IsPrimary: (info.dwFlags & MONITORINFOEXW.MONITORINFOF_PRIMARY) != 0,
                Device: info.szDevice ?? string.Empty,
                WorkLeft: info.rcWork.Left,
                WorkTop: info.rcWork.Top,
                WorkWidth: info.rcWork.Right - info.rcWork.Left,
                WorkHeight: info.rcWork.Bottom - info.rcWork.Top));
            return true;
        }

        if (!EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, Callback, IntPtr.Zero))
        {
            throw new InvalidOperationException(
                $"EnumDisplayMonitors failed: Win32={Marshal.GetLastWin32Error()}");
        }

        return list;
    }
}
