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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using XerahS.RegionCapture.Models;
using PixelRect = XerahS.RegionCapture.Models.PixelRect;

namespace XerahS.RegionCapture.Services;

/// <summary>
/// Service for enumerating physical monitors using Avalonia's cross-platform API.
/// On Windows, enhanced with native DPI detection for sub-pixel precision.
/// </summary>
public static class MonitorEnumerationService
{
    /// <summary>
    /// Gets information about all connected monitors.
    /// </summary>
    public static IReadOnlyList<MonitorInfo> GetAllMonitors()
    {
#if WINDOWS
        return GetWindowsMonitors();
#else
        return GetAvaloniaMonitors();
#endif
    }

    private static IReadOnlyList<MonitorInfo> GetAvaloniaMonitors()
    {
        var screens = Application.Current?.ApplicationLifetime switch
        {
            IClassicDesktopStyleApplicationLifetime desktop
                => desktop.MainWindow?.Screens,
            _ => null
        };

        if (screens is null)
            return [];

        var allScreens = screens.All;
        bool isWayland = IsWaylandSession();
        var logicalScreens = new List<AvaloniaScreenLayout>(allScreens.Count);

        for (int i = 0; i < allScreens.Count; i++)
        {
            var screen = allScreens[i];
            var scaleFactor = screen.Scaling > 0 ? screen.Scaling : 1.0;

            // Wayland and macOS: Avalonia reports bounds in logical coordinates (compositor space / points).
            // X11: Avalonia reports bounds in device (physical) pixels; convert to logical so the
            // normalizer produces correct OverlayBounds (logical) and PhysicalBounds for mixed DPI.
            PixelRect bounds;
            PixelRect workArea;
            bool useLogicalAsIs = isWayland || OperatingSystem.IsMacOS();
            if (useLogicalAsIs)
            {
                bounds = new PixelRect(
                    screen.Bounds.X,
                    screen.Bounds.Y,
                    screen.Bounds.Width,
                    screen.Bounds.Height);
                workArea = new PixelRect(
                    screen.WorkingArea.X,
                    screen.WorkingArea.Y,
                    screen.WorkingArea.Width,
                    screen.WorkingArea.Height);
            }
            else
            {
                bounds = PhysicalToLogicalRect(ToPixelRect(screen.Bounds), scaleFactor);
                workArea = PhysicalToLogicalRect(ToPixelRect(screen.WorkingArea), scaleFactor);
            }

            logicalScreens.Add(new AvaloniaScreenLayout(
                DeviceName: $"Display {i + 1}",
                ScaleFactor: scaleFactor,
                Bounds: bounds,
                WorkingArea: workArea,
                IsPrimary: screen.IsPrimary));
        }

        // Always use the normalizer on non-Windows so overlay windows get logical bounds
        // (OverlayBoundsOverride) and capture uses physical bounds. This avoids one monitor's
        // overlay appearing squashed in mixed-DPI configurations (X11, macOS, Wayland).
        // Aligns with: Avalonia Screen.Bounds in device pixels (X11); Wayland/macOS logical;
        // AVALONIA_SCREEN_SCALE_FACTORS and mixed-DPI issues (#14755, #17834).
        return WaylandMonitorLayoutNormalizer.Normalize(logicalScreens);
    }

    private static PixelRect ToPixelRect(Avalonia.PixelRect r)
    {
        return new PixelRect(r.X, r.Y, r.Width, r.Height);
    }

    /// <summary>
    /// Converts a rectangle from physical (device) pixels to logical coordinates by dividing by scale.
    /// Used on X11 where Avalonia reports physical bounds; we need logical for overlay sizing.
    /// </summary>
    private static PixelRect PhysicalToLogicalRect(PixelRect physical, double scaleFactor)
    {
        if (scaleFactor <= 0) scaleFactor = 1.0;
        return new PixelRect(
            physical.X / scaleFactor,
            physical.Y / scaleFactor,
            Math.Max(1, physical.Width / scaleFactor),
            Math.Max(1, physical.Height / scaleFactor));
    }

    private static bool IsWaylandSession()
    {
        if (!OperatingSystem.IsLinux())
        {
            return false;
        }

        return string.Equals(
            Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"),
            "wayland",
            StringComparison.OrdinalIgnoreCase);
    }

#if WINDOWS
    private static IReadOnlyList<MonitorInfo> GetWindowsMonitors()
    {
        return Platform.Windows.NativeMonitorService.EnumerateMonitors();
    }
#endif
}
