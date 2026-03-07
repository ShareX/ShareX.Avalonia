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
using XerahS.RegionCapture.Models;

namespace XerahS.RegionCapture.Services;

internal readonly record struct AvaloniaScreenLayout(
    string DeviceName,
    PixelRect Bounds,
    PixelRect WorkingArea,
    double ScaleFactor,
    bool IsPrimary);

internal static class WaylandMonitorLayoutNormalizer
{
    public static IReadOnlyList<MonitorInfo> Normalize(IReadOnlyList<AvaloniaScreenLayout> screens)
    {
        if (screens.Count == 0)
        {
            return [];
        }

        var normalizedScreens = screens
            .Select(screen => new NormalizedScreen(screen, GetSafeScaleFactor(screen.ScaleFactor)))
            .ToArray();

        var columns = normalizedScreens
            .GroupBy(screen => screen.Layout.Bounds.X)
            .OrderBy(group => group.Key)
            .ToArray();

        var physicalXByLogicalColumn = new Dictionary<double, double>(columns.Length);
        double currentPhysicalX = 0;

        foreach (var column in columns)
        {
            physicalXByLogicalColumn[column.Key] = currentPhysicalX;
            currentPhysicalX += column.Max(screen => screen.PhysicalWidth);
        }

        double minLogicalY = normalizedScreens.Min(screen => screen.Layout.Bounds.Y);

        var monitors = new List<MonitorInfo>(normalizedScreens.Length);
        foreach (var screen in normalizedScreens)
        {
            double physicalX = physicalXByLogicalColumn[screen.Layout.Bounds.X];
            double physicalY = ScaleToPhysical(screen.Layout.Bounds.Y - minLogicalY, screen.ScaleFactor);

            var physicalBounds = new PixelRect(
                physicalX,
                physicalY,
                screen.PhysicalWidth,
                screen.PhysicalHeight);

            var overlayBounds = screen.Layout.Bounds;
            var overlayWorkArea = screen.Layout.WorkingArea;
            var physicalWorkArea = new PixelRect(
                physicalBounds.X + ScaleToPhysical(overlayWorkArea.X - overlayBounds.X, screen.ScaleFactor),
                physicalBounds.Y + ScaleToPhysical(overlayWorkArea.Y - overlayBounds.Y, screen.ScaleFactor),
                ScaleToPhysical(overlayWorkArea.Width, screen.ScaleFactor),
                ScaleToPhysical(overlayWorkArea.Height, screen.ScaleFactor));

            monitors.Add(new MonitorInfo(
                DeviceName: screen.Layout.DeviceName,
                PhysicalBounds: physicalBounds,
                WorkArea: physicalWorkArea,
                ScaleFactor: screen.ScaleFactor,
                IsPrimary: screen.Layout.IsPrimary,
                OverlayBoundsOverride: overlayBounds,
                OverlayWorkAreaOverride: overlayWorkArea));
        }

        return monitors;
    }

    private static double GetSafeScaleFactor(double scaleFactor)
    {
        return scaleFactor > 0 ? scaleFactor : 1.0;
    }

    private static double ScaleToPhysical(double logicalValue, double scaleFactor)
    {
        return Math.Round(logicalValue * scaleFactor);
    }

    private readonly record struct NormalizedScreen(AvaloniaScreenLayout Layout, double ScaleFactor)
    {
        public double PhysicalWidth => ScaleToPhysical(Layout.Bounds.Width, ScaleFactor);
        public double PhysicalHeight => ScaleToPhysical(Layout.Bounds.Height, ScaleFactor);
    }
}
