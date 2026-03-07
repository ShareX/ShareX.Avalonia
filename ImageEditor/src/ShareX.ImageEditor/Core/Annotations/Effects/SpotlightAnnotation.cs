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
/// Spotlight annotation - darkens entire image except highlighted area
/// </summary>
public partial class SpotlightAnnotation : Annotation
{
    public override AnnotationCategory Category => AnnotationCategory.Effects;

    /// <summary>
    /// Darkening overlay opacity (0-255)
    /// </summary>
    public byte DarkenOpacity { get; set; } = 180;

    /// <summary>
    /// Size of the canvas (needed for full overlay)
    /// </summary>
    public SKSize CanvasSize { get; set; }

    public SpotlightAnnotation()
    {
        ToolType = EditorTool.Spotlight;
    }




    public override bool HitTest(SKPoint point, float tolerance = 5)
    {
        var bounds = GetBounds();
        var inflated = SKRect.Inflate(bounds, tolerance, tolerance);
        return inflated.Contains(point);
    }
}
