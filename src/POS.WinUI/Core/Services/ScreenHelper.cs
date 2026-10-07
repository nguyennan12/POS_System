using System.Runtime.InteropServices;
using System.Windows;

namespace POS.WinUI.Core.Services;

public struct ScreenRect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
    public int Width => Right - Left;
    public int Height => Bottom - Top;
    public bool IsPrimary;
}

public static class ScreenHelper
{
    private const int MONITORINFOF_PRIMARY = 0x00000001;

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public int dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    public static List<ScreenRect> GetAllScreens()
    {
        var screens = new List<ScreenRect>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
        {
            var mi = new MONITORINFOEX();
            mi.cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
            if (GetMonitorInfo(hMonitor, ref mi))
            {
                screens.Add(new ScreenRect
                {
                    Left = mi.rcMonitor.Left,
                    Top = mi.rcMonitor.Top,
                    Right = mi.rcMonitor.Right,
                    Bottom = mi.rcMonitor.Bottom,
                    IsPrimary = (mi.dwFlags & MONITORINFOF_PRIMARY) != 0
                });
            }
            return true;
        }, IntPtr.Zero);

        return screens;
    }
}
