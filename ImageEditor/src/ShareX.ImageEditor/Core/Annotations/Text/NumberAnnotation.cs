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
/// Number annotation - auto-incrementing numbered circle markers
/// </summary>
public partial class NumberAnnotation : Annotation
{
    public override AnnotationCategory Category => AnnotationCategory.Text;
    /// <summary>
    /// Number to display (typically auto-incremented)
    /// </summary>
    public int Number { get; set; } = 1;

    /// <summary>
    /// Font size for the number
    /// </summary>
    public float FontSize { get; set; } = 24;

    /// <summary>
    /// Text body color
    /// </summary>
    public string TextColor { get; set; } = "#FFFFFFFF";

    /// <summary>
    /// Circle radius - auto-calculated based on FontSize if not explicitly set
    /// </summary>
    public float Radius
    {
        get => CalculateRadius();
        set { } // Allow setting but use calculated value
    }

    /// <summary>
    /// Calculate radius based on font size to ensure text fits
    /// </summary>
    private float CalculateRadius()
    {
        // Radius should be about 70% of FontSize to properly contain the number
        // with some padding around it
        return Math.Max(12, FontSize * 0.7f);
    }

    public NumberAnnotation()
    {
        ToolType = EditorTool.Step;
    }



    public override bool HitTest(SKPoint point, float tolerance = 5)
    {
        var dx = point.X - StartPoint.X;
        var dy = point.Y - StartPoint.Y;
        var distance = (float)Math.Sqrt(dx * dx + dy * dy);
        return distance <= (Radius + tolerance);
    }

    public override SKRect GetBounds()
    {
        return new SKRect(
            StartPoint.X - Radius,
            StartPoint.Y - Radius,
            StartPoint.X + Radius,
            StartPoint.Y + Radius);
    }
}
