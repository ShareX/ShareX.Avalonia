#region License Information (GPL v3)

/*
    ShareX.Avalonia - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2025 ShareX Team

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
using Avalonia.Media;

namespace ShareX.Avalonia.Annotations.Models;

/// <summary>
/// Spotlight annotation - darkens entire image except highlighted area
/// </summary>
public class SpotlightAnnotation : Annotation
{
    /// <summary>
    /// Darkening overlay opacity (0-255)
    /// </summary>
    public byte DarkenOpacity { get; set; } = 180;

    /// <summary>
    /// Size of the canvas (needed for full overlay)
    /// </summary>
    public Size CanvasSize { get; set; }

    public SpotlightAnnotation()
    {
        ToolType = EditorTool.Spotlight;
    }

    public override void Render(DrawingContext context)
    {
        if (CanvasSize.Width <= 0 || CanvasSize.Height <= 0) return;

        // Create dark overlay brush
        var overlayBrush = new SolidColorBrush(Color.FromArgb(DarkenOpacity, 0, 0, 0));
        
        // Full canvas rectangle
        var fullRect = new Rect(0, 0, CanvasSize.Width, CanvasSize.Height);
        
        // Spotlight ellipse rectangle
        var spotlightRect = new Rect(StartPoint, EndPoint);
        
        // Create geometry with hole using PathGeometry
        var pathGeometry = new PathGeometry();
        var pathFigure = new PathFigure { StartPoint = new Point(0, 0), IsClosed = true };
        
        // Outer rectangle (full canvas)
        pathFigure.Segments.Add(new LineSegment { Point = new Point(CanvasSize.Width, 0) });
        pathFigure.Segments.Add(new LineSegment { Point = new Point(CanvasSize.Width, CanvasSize.Height) });
        pathFigure.Segments.Add(new LineSegment { Point = new Point(0, CanvasSize.Height) });
        pathFigure.Segments.Add(new LineSegment { Point = new Point(0, 0) });
        
        pathGeometry.Figures.Add(pathFigure);
        
        // Draw overlay
        context.DrawGeometry(overlayBrush, null, pathGeometry);
        
        // Draw spotlight ellipse border (optional - for visibility)
        var pen = new Pen(new SolidColorBrush(Colors.Yellow), 2);
        var ellipseGeometry = new EllipseGeometry(spotlightRect);
        context.DrawGeometry(null, pen, ellipseGeometry);
    }

    public override bool HitTest(Point point, double tolerance = 5)
    {
        var rect = new Rect(StartPoint, EndPoint);
        return rect.Inflate(tolerance).Contains(point);
    }
}
