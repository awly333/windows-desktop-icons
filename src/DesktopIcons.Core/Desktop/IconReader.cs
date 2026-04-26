using System.Runtime.InteropServices;
using DesktopIcons.Core.Interop;
using DesktopIcons.Core.Models;

namespace DesktopIcons.Core.Desktop;

public static class IconReader
{
    private const int TextBufferChars = 260;
    private const uint SendTimeoutMs = 2000;

    public static List<IconInfo> ReadAll(DesktopListView desktop)
    {
        var count = GetItemCount(desktop.Hwnd);
        var icons = new List<IconInfo>(count);

        using var pointBuf = new RemoteMemory(desktop.ProcessHandle, Marshal.SizeOf<POINT>());
        using var textBuf = new RemoteMemory(desktop.ProcessHandle, TextBufferChars * sizeof(char));
        using var itemBuf = new RemoteMemory(desktop.ProcessHandle, Marshal.SizeOf<LVITEMW>());

        for (int i = 0; i < count; i++)
        {
            var label = ReadLabel(desktop, i, itemBuf, textBuf);
            var (x, y) = ReadPosition(desktop.Hwnd, i, pointBuf);
            icons.Add(new IconInfo { Label = label, X = x, Y = y });
        }

        return icons;
    }

    private static int GetItemCount(IntPtr hwnd)
    {
        NativeMethods.SendMessageTimeout(
            hwnd, LvConstants.LVM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero,
            SendMessageFlags.SMTO_ABORTIFHUNG, SendTimeoutMs, out var result);
        return result.ToInt32();
    }

    private static (int X, int Y) ReadPosition(IntPtr hwnd, int index, RemoteMemory pointBuf)
    {
        NativeMethods.SendMessageTimeout(
            hwnd, LvConstants.LVM_GETITEMPOSITION, (IntPtr)index, pointBuf.Address,
            SendMessageFlags.SMTO_ABORTIFHUNG, SendTimeoutMs, out _);
        var pt = pointBuf.Read<POINT>();
        return (pt.X, pt.Y);
    }

    private static string ReadLabel(DesktopListView desktop, int index, RemoteMemory itemBuf, RemoteMemory textBuf)
    {
        var item = new LVITEMW
        {
            mask = LvConstants.LVIF_TEXT,
            iItem = index,
            iSubItem = 0,
            pszText = textBuf.Address,
            cchTextMax = TextBufferChars
        };
        itemBuf.Write(in item);

        NativeMethods.SendMessageTimeout(
            desktop.Hwnd, LvConstants.LVM_GETITEMTEXTW, (IntPtr)index, itemBuf.Address,
            SendMessageFlags.SMTO_ABORTIFHUNG, SendTimeoutMs, out _);

        var bytes = new byte[TextBufferChars * sizeof(char)];
        textBuf.ReadBytes(bytes);
        var raw = System.Text.Encoding.Unicode.GetString(bytes);
        var nul = raw.IndexOf('\0');
        return nul >= 0 ? raw[..nul] : raw;
    }
}
