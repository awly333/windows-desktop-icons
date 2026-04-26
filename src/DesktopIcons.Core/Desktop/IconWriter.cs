using System.Runtime.InteropServices;
using DesktopIcons.Core.Interop;
using DesktopIcons.Core.Models;

namespace DesktopIcons.Core.Desktop;

public static class IconWriter
{
    public sealed record ApplyResult(int Moved, int NotFound, IReadOnlyList<string> MissingLabels);

    public static ApplyResult Apply(DesktopListView desktop, IReadOnlyList<IconInfo> desired)
    {
        var current = IconReader.ReadAll(desktop);
        var labelToIndex = new Dictionary<string, int>(current.Count);
        for (int i = 0; i < current.Count; i++)
        {
            labelToIndex.TryAdd(current[i].Label, i);
        }

        using var pointBuf = new RemoteMemory(desktop.ProcessHandle, Marshal.SizeOf<POINT>());

        NativeMethods.SendMessage(desktop.Hwnd, LvConstants.WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);

        int moved = 0;
        var missing = new List<string>();

        try
        {
            foreach (var icon in desired)
            {
                if (!labelToIndex.TryGetValue(icon.Label, out var idx))
                {
                    missing.Add(icon.Label);
                    continue;
                }

                pointBuf.Write(new POINT { X = icon.X, Y = icon.Y });
                NativeMethods.SendMessage(
                    desktop.Hwnd,
                    LvConstants.LVM_SETITEMPOSITION32,
                    (IntPtr)idx,
                    pointBuf.Address);
                moved++;
            }
        }
        finally
        {
            NativeMethods.SendMessage(desktop.Hwnd, LvConstants.WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
            NativeMethods.InvalidateRect(desktop.Hwnd, IntPtr.Zero, true);
        }

        return new ApplyResult(moved, missing.Count, missing);
    }
}
