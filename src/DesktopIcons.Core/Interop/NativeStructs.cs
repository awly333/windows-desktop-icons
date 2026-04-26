using System.Runtime.InteropServices;

namespace DesktopIcons.Core.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct LVITEMW
{
    public uint mask;
    public int iItem;
    public int iSubItem;
    public uint state;
    public uint stateMask;
    public IntPtr pszText;
    public int cchTextMax;
    public int iImage;
    public IntPtr lParam;
    public int iIndent;
    public int iGroupId;
    public uint cColumns;
    public IntPtr puColumns;
    public IntPtr piColFmt;
    public int iGroup;
}

internal static class LvConstants
{
    public const uint LVM_FIRST = 0x1000;
    public const uint LVM_GETITEMCOUNT = LVM_FIRST + 4;
    public const uint LVM_GETITEMPOSITION = LVM_FIRST + 16;
    public const uint LVM_SETITEMPOSITION32 = LVM_FIRST + 49;
    public const uint LVM_GETITEMTEXTW = LVM_FIRST + 115;

    public const uint LVIF_TEXT = 0x00000001;

    public const uint WM_SETREDRAW = 0x000B;

    public const int GWL_STYLE = -16;
    public const int LVS_AUTOARRANGE = 0x0100;
}

internal static class ProcessAccess
{
    public const uint VM_OPERATION = 0x0008;
    public const uint VM_READ = 0x0010;
    public const uint VM_WRITE = 0x0020;
    public const uint ALL_FOR_REMOTE = VM_OPERATION | VM_READ | VM_WRITE;
}

internal static class AllocFlags
{
    public const uint MEM_COMMIT = 0x1000;
    public const uint MEM_RESERVE = 0x2000;
    public const uint MEM_RELEASE = 0x8000;
    public const uint PAGE_READWRITE = 0x04;
}

internal static class SendMessageFlags
{
    public const uint SMTO_NORMAL = 0x0000;
    public const uint SMTO_ABORTIFHUNG = 0x0002;
}
