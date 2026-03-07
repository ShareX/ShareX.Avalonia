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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ShareX.ImageEditor.Core.Annotations;

public partial class ArrowAnnotation
{
    /// <summary>
    /// Creates the Avalonia visual for this annotation
    /// </summary>
    public Control CreateVisual()
    {
        var brush = new SolidColorBrush(Color.Parse(StrokeColor));
        var path = new Avalonia.Controls.Shapes.Path
        {
            Stroke = brush,
            StrokeThickness = StrokeWidth,
            StrokeLineCap = PenLineCap.Round,
            StrokeJoin = PenLineJoin.Round,
            Fill = brush,
            Tag = this
        };

        // Populate the arrow geometry using the annotation's points
        path.Data = CreateArrowGeometry(
            new Point(StartPoint.X, StartPoint.Y),
            new Point(EndPoint.X, EndPoint.Y),
            StrokeWidth * ArrowHeadWidthMultiplier);

        if (ShadowEnabled)
        {
            path.Effect = new Avalonia.Media.DropShadowEffect
            {
                OffsetX = 3,
                OffsetY = 3,
                BlurRadius = 4,
                Color = Avalonia.Media.Color.FromArgb(128, 0, 0, 0)
            };
        }

        return path;
    }

    /// <summary>
    /// Creates arrow geometry for the Avalonia Path.
    /// Consumes <see cref="ComputeArrowPoints"/> to guarantee identical shape with <see cref="Render"/>.
    /// </summary>
    public Geometry CreateArrowGeometry(Point start, Point end, double headSize)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            var pts = ComputeArrowPoints(
                (float)start.X, (float)start.Y,
                (float)end.X, (float)end.Y,
                headSize);

            if (pts is { } p)
            {
                ctx.BeginFigure(start, true);
                ctx.LineTo(new Point(p.ShaftEndLeft.X, p.ShaftEndLeft.Y));
                ctx.LineTo(new Point(p.WingLeft.X, p.WingLeft.Y));
                ctx.LineTo(end);
                ctx.LineTo(new Point(p.WingRight.X, p.WingRight.Y));
                ctx.LineTo(new Point(p.ShaftEndRight.X, p.ShaftEndRight.Y));
                ctx.EndFigure(true);
            }
            else
            {
                var radius = 2.0;
                ctx.BeginFigure(new Point(start.X - radius, start.Y), true);
                ctx.ArcTo(new Point(start.X + radius, start.Y), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
                ctx.ArcTo(new Point(start.X - radius, start.Y), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
                ctx.EndFigure(true);
            }
        }

        return geometry;
    }
}
