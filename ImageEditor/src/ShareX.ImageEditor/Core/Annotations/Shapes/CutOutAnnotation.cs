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
/// CutOut annotation - cuts out a horizontal or vertical section and joins remaining parts
/// Note: This is a special annotation that triggers actual image modification
/// </summary>
public class CutOutAnnotation : Annotation
{
    public override AnnotationCategory Category => AnnotationCategory.Shapes;
    /// <summary>
    /// Indicates if the cut is vertical (true) or horizontal (false)
    /// </summary>
    public bool IsVertical { get; set; }

    public CutOutAnnotation()
    {
        ToolType = EditorTool.CutOut;
    }



    public override bool HitTest(SKPoint point, float tolerance = 5)
    {
        var rect = GetBounds();

        if (IsVertical)
        {
            // Check if point is near the vertical line
            float x = rect.MidX;
            return Math.Abs(point.X - x) <= tolerance;
        }
        else
        {
            // Check if point is near the horizontal line
            float y = rect.MidY;
            return Math.Abs(point.Y - y) <= tolerance;
        }
    }
}
