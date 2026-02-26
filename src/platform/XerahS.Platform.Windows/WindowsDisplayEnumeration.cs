#region License Information (GPL v3)

/*
    XerahS - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2026 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace XerahS.Platform.Windows
{
    [SupportedOSPlatform("windows")]
    internal static class WindowsDisplayEnumeration
    {
        private const uint MONITORINFOF_PRIMARY = 1;
        private const int BITSPIXEL = 12;
        private const int PLANES = 14;

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromRect(ref RECT lprc, uint dwFlags);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateDC(string pwszDriver, string pwszDevice, string? pszPort, IntPtr pdm);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int index);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFOEX
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public Rectangle ToRectangle()
            {
                return new Rectangle(Left, Top, Right - Left, Bottom - Top);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        internal sealed class MonitorData
        {
            public IntPtr Handle { get; set; }

            public Rectangle Bounds { get; set; }

            public Rectangle WorkingArea { get; set; }

            public bool IsPrimary { get; set; }

            public string DeviceName { get; set; } = string.Empty;

            public int BitsPerPixel { get; set; }

            public double ScaleFactor { get; set; }
        }

        public static Point GetCursorPosition()
        {
            return NativeMethods.GetCursorPos();
        }

        public static Rectangle GetVirtualScreenBounds()
        {
            int left = NativeMethods.GetSystemMetrics(SystemMetric.SM_XVIRTUALSCREEN);
            int top = NativeMethods.GetSystemMetrics(SystemMetric.SM_YVIRTUALSCREEN);
            int width = NativeMethods.GetSystemMetrics(SystemMetric.SM_CXVIRTUALSCREEN);
            int height = NativeMethods.GetSystemMetrics(SystemMetric.SM_CYVIRTUALSCREEN);

            if (width <= 0 || height <= 0)
                return Rectangle.Empty;

            return new Rectangle(left, top, width, height);
        }

        public static MonitorData[] GetMonitors()
        {
            List<MonitorData> monitors = new();
            MonitorEnumProc callback = (hMonitor, _, _, _) =>
            {
                if (TryCreateMonitorData(hMonitor, out MonitorData monitor))
                {
                    monitors.Add(monitor);
                }

                return true;
            };

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);

            if (monitors.Count == 0)
            {
                monitors.Add(CreateFallbackMonitor());
            }

            return monitors.ToArray();
        }

        public static MonitorData GetMonitorFromPoint(Point point)
        {
            POINT nativePoint = new() { X = point.X, Y = point.Y };
            IntPtr hMonitor = MonitorFromPoint(nativePoint, NativeMethods.MONITOR_DEFAULTTONEAREST);

            if (TryCreateMonitorData(hMonitor, out MonitorData monitor))
                return monitor;

            return GetFallbackFromEnumeration();
        }

        public static MonitorData GetMonitorFromRectangle(Rectangle rectangle)
        {
            RECT nativeRect = new()
            {
                Left = rectangle.Left,
                Top = rectangle.Top,
                Right = rectangle.Right,
                Bottom = rectangle.Bottom
            };

            IntPtr hMonitor = MonitorFromRect(ref nativeRect, NativeMethods.MONITOR_DEFAULTTONEAREST);
            if (TryCreateMonitorData(hMonitor, out MonitorData monitor))
                return monitor;

            return GetFallbackFromEnumeration();
        }

        private static MonitorData GetFallbackFromEnumeration()
        {
            MonitorData[] monitors = GetMonitors();
            if (monitors.Length > 0)
                return monitors[0];

            return CreateFallbackMonitor();
        }

        private static MonitorData CreateFallbackMonitor()
        {
            Rectangle virtualBounds = GetVirtualScreenBounds();
            if (virtualBounds == Rectangle.Empty)
            {
                virtualBounds = new Rectangle(0, 0, NativeMethods.GetSystemMetrics(SystemMetric.SM_CXSCREEN), NativeMethods.GetSystemMetrics(SystemMetric.SM_CYSCREEN));
            }

            return new MonitorData
            {
                Handle = IntPtr.Zero,
                Bounds = virtualBounds,
                WorkingArea = virtualBounds,
                IsPrimary = true,
                DeviceName = "DISPLAY",
                BitsPerPixel = 32,
                ScaleFactor = 1.0
            };
        }

        private static bool TryCreateMonitorData(IntPtr hMonitor, out MonitorData monitor)
        {
            monitor = CreateFallbackMonitor();
            if (hMonitor == IntPtr.Zero)
                return false;

            MONITORINFOEX monitorInfo = new() { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
            if (!GetMonitorInfo(hMonitor, ref monitorInfo))
                return false;

            monitor = new MonitorData
            {
                Handle = hMonitor,
                Bounds = monitorInfo.rcMonitor.ToRectangle(),
                WorkingArea = monitorInfo.rcWork.ToRectangle(),
                IsPrimary = (monitorInfo.dwFlags & MONITORINFOF_PRIMARY) != 0,
                DeviceName = monitorInfo.szDevice,
                BitsPerPixel = GetBitsPerPixel(monitorInfo.szDevice),
                ScaleFactor = NativeMethods.GetMonitorScaleFactor(hMonitor)
            };

            return true;
        }

        private static int GetBitsPerPixel(string deviceName)
        {
            IntPtr hdc = CreateDC("DISPLAY", deviceName, null, IntPtr.Zero);
            if (hdc == IntPtr.Zero)
                return 32;

            try
            {
                int bitsPerPixel = GetDeviceCaps(hdc, BITSPIXEL);
                int planes = GetDeviceCaps(hdc, PLANES);
                int value = bitsPerPixel * planes;

                return value > 0 ? value : 32;
            }
            finally
            {
                DeleteDC(hdc);
            }
        }
    }
}
