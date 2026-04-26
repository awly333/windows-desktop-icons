using DesktopIcons.Core.Interop;

namespace DesktopIcons.Core;

public static class Dpi
{
    public static void EnsurePerMonitorV2()
    {
        NativeMethods.SetProcessDpiAwarenessContext(
            NativeMethods.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
    }
}
