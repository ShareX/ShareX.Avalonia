#region License Information (GPL v3)

/*
    ShareX.ImageEditor - The UI-agnostic Editor library for ShareX
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

using SkiaSharp;

namespace ShareX.ImageEditor.Core.Annotations;

/// <summary>
/// Arrow annotation (line with arrowhead)
/// </summary>
public partial class ArrowAnnotation : Annotation
{
    public override AnnotationCategory Category => AnnotationCategory.Shapes;
    /// <summary>
    /// Arrow head width is proportional to stroke width for visual balance.
    /// ISSUE-006 fix: Centralized magic number constant.
    /// </summary>
    public const double ArrowHeadWidthMultiplier = 3.0;

    public ArrowAnnotation()
    {
        ToolType = EditorTool.Arrow;
    }

    /// <summary>
    /// Single source of truth for arrow geometry points.
    /// Both <see cref="Render"/> (SKCanvas) and <c>CreateArrowGeometry</c> (Avalonia)
    /// consume this method so the shape can never diverge between rendering backends.
    /// Returns null when the arrow has zero length.
    /// </summary>
    public static ArrowPoints? ComputeArrowPoints(
        float startX, float startY,
        float endX, float endY,
        double headSize)
    {
        var dx = endX - startX;
        var dy = endY - startY;
        var length = Math.Sqrt(dx * dx + dy * dy);

        if (length <= 0) return null;

        var ux = dx / length;
        var uy = dy / length;
        var perpX = -uy;
        var perpY = ux;

        var headHeight = headSize * 2.5;
        var headWidthBase = headSize * 1.5;
        var arrowAngle = Math.PI / 5.14; // 35 degrees

        var baseX = endX - headHeight * ux;
        var baseY = endY - headHeight * uy;

        var wingWidth = headWidthBase * Math.Tan(arrowAngle);
        var shaftWidth = headWidthBase * 0.25;

        return new ArrowPoints(
            ShaftEndLeft: new SKPoint((float)(baseX + perpX * shaftWidth), (float)(baseY + perpY * shaftWidth)),
            ShaftEndRight: new SKPoint((float)(baseX - perpX * shaftWidth), (float)(baseY - perpY * shaftWidth)),
            WingLeft: new SKPoint((float)(baseX + perpX * wingWidth), (float)(baseY + perpY * wingWidth)),
            WingRight: new SKPoint((float)(baseX - perpX * wingWidth), (float)(baseY - perpY * wingWidth))
        );
    }

    /// <summary>
    /// Computed arrow geometry points returned by <see cref="ComputeArrowPoints"/>.
    /// </summary>
    public record struct ArrowPoints(
        SKPoint ShaftEndLeft,
        SKPoint ShaftEndRight,
        SKPoint WingLeft,
        SKPoint WingRight);

    public override bool HitTest(SKPoint point, float tolerance = 5)
    {
        // Reuse line hit test logic
        var dx = EndPoint.X - StartPoint.X;
        var dy = EndPoint.Y - StartPoint.Y;
        var lineLength = (float)Math.Sqrt(dx * dx + dy * dy);
        if (lineLength < 0.001f) return false;

        var t = Math.Max(0, Math.Min(1,
            ((point.X - StartPoint.X) * (EndPoint.X - StartPoint.X) +
             (point.Y - StartPoint.Y) * (EndPoint.Y - StartPoint.Y)) / (lineLength * lineLength)));

        var projection = new SKPoint(
            StartPoint.X + (float)t * (EndPoint.X - StartPoint.X),
            StartPoint.Y + (float)t * (EndPoint.Y - StartPoint.Y));

        var pdx = point.X - projection.X;
        var pdy = point.Y - projection.Y;
        var distance = (float)Math.Sqrt(pdx * pdx + pdy * pdy);
        return distance <= tolerance;
    }
}