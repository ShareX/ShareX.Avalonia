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

using System.Drawing;
using XerahS.Platform.Abstractions;

namespace XerahS.Platform.Windows
{
    /// <summary>
    /// Windows implementation of IScreenService using Win32 display APIs.
    /// </summary>
    public class WindowsScreenService : IScreenService
    {
        public bool UsePerScreenScalingForRegionCaptureLayout => false;

        public bool UseWindowPositionForRegionCaptureFallback => false;

        public bool UseLogicalCoordinatesForRegionCapture => false;

        public Rectangle GetVirtualScreenBounds()
        {
            return WindowsDisplayEnumeration.GetVirtualScreenBounds();
        }

        public Rectangle GetWorkingArea()
        {
            WindowsDisplayEnumeration.MonitorData[] monitors = WindowsDisplayEnumeration.GetMonitors();
            return CombineRectangles(monitors.Select(m => m.WorkingArea));
        }

        public Rectangle GetActiveScreenBounds()
        {
            Point cursorPos = WindowsDisplayEnumeration.GetCursorPosition();
            return WindowsDisplayEnumeration.GetMonitorFromPoint(cursorPos).Bounds;
        }

        public Rectangle GetActiveScreenWorkingArea()
        {
            Point cursorPos = WindowsDisplayEnumeration.GetCursorPosition();
            return WindowsDisplayEnumeration.GetMonitorFromPoint(cursorPos).WorkingArea;
        }

        public Rectangle GetPrimaryScreenBounds()
        {
            WindowsDisplayEnumeration.MonitorData[] monitors = WindowsDisplayEnumeration.GetMonitors();
            return monitors.FirstOrDefault(m => m.IsPrimary)?.Bounds ?? Rectangle.Empty;
        }

        public Rectangle GetPrimaryScreenWorkingArea()
        {
            WindowsDisplayEnumeration.MonitorData[] monitors = WindowsDisplayEnumeration.GetMonitors();
            return monitors.FirstOrDefault(m => m.IsPrimary)?.WorkingArea ?? Rectangle.Empty;
        }

        public ScreenInfo[] GetAllScreens()
        {
            WindowsDisplayEnumeration.MonitorData[] monitors = WindowsDisplayEnumeration.GetMonitors();
            return monitors.Select(ToScreenInfo).ToArray();
        }

        public ScreenInfo GetScreenFromPoint(Point point)
        {
            WindowsDisplayEnumeration.MonitorData monitor = WindowsDisplayEnumeration.GetMonitorFromPoint(point);
            return ToScreenInfo(monitor);
        }

        public ScreenInfo GetScreenFromRectangle(Rectangle rectangle)
        {
            WindowsDisplayEnumeration.MonitorData monitor = WindowsDisplayEnumeration.GetMonitorFromRectangle(rectangle);
            return ToScreenInfo(monitor);
        }

        private static ScreenInfo ToScreenInfo(WindowsDisplayEnumeration.MonitorData monitor)
        {
            return new ScreenInfo
            {
                Bounds = monitor.Bounds,
                WorkingArea = monitor.WorkingArea,
                IsPrimary = monitor.IsPrimary,
                DeviceName = monitor.DeviceName,
                BitsPerPixel = monitor.BitsPerPixel,
                ScaleFactor = monitor.ScaleFactor
            };
        }

        private static Rectangle CombineRectangles(IEnumerable<Rectangle> rectangles)
        {
            if (!rectangles.Any())
                return Rectangle.Empty;

            int left = rectangles.Min(r => r.Left);
            int top = rectangles.Min(r => r.Top);
            int right = rectangles.Max(r => r.Right);
            int bottom = rectangles.Max(r => r.Bottom);

            return new Rectangle(left, top, right - left, bottom - top);
        }
    }
}
