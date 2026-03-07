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
using System.Text.Json.Serialization;

namespace ShareX.ImageEditor.Core.Annotations;

/// <summary>
/// Base class for all annotation types
/// </summary>
[JsonDerivedType(typeof(ArrowAnnotation), typeDiscriminator: "Arrow")]
[JsonDerivedType(typeof(BlurAnnotation), typeDiscriminator: "Blur")]
[JsonDerivedType(typeof(CropAnnotation), typeDiscriminator: "Crop")]
[JsonDerivedType(typeof(EllipseAnnotation), typeDiscriminator: "Ellipse")]
[JsonDerivedType(typeof(FreehandAnnotation), typeDiscriminator: "Freehand")]
[JsonDerivedType(typeof(HighlightAnnotation), typeDiscriminator: "Highlight")]
[JsonDerivedType(typeof(ImageAnnotation), typeDiscriminator: "Image")]
[JsonDerivedType(typeof(LineAnnotation), typeDiscriminator: "Line")]
[JsonDerivedType(typeof(MagnifyAnnotation), typeDiscriminator: "Magnify")]
[JsonDerivedType(typeof(NumberAnnotation), typeDiscriminator: "Number")]
[JsonDerivedType(typeof(PixelateAnnotation), typeDiscriminator: "Pixelate")]
[JsonDerivedType(typeof(RectangleAnnotation), typeDiscriminator: "Rectangle")]
[JsonDerivedType(typeof(SmartEraserAnnotation), typeDiscriminator: "SmartEraser")]
[JsonDerivedType(typeof(SpeechBalloonAnnotation), typeDiscriminator: "SpeechBalloon")]
[JsonDerivedType(typeof(SpotlightAnnotation), typeDiscriminator: "Spotlight")]
[JsonDerivedType(typeof(TextAnnotation), typeDiscriminator: "Text")]
public abstract class Annotation
{
    /// <summary>
    /// Functional category of this annotation type (Shapes, Effects, or Text).
    /// </summary>
    public abstract AnnotationCategory Category { get; }

    /// <summary>
    /// Unique identifier for this annotation
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Tool type that created this annotation
    /// </summary>
    public EditorTool ToolType { get; set; }

    /// <summary>
    /// Stroke/border color (hex color string)
    /// </summary>
    public string StrokeColor { get; set; } = "#ef4444";

    /// <summary>
    /// Stroke width in pixels
    /// </summary>
    public float StrokeWidth { get; set; } = 4;

    /// <summary>
    /// Fill color (hex color string, transparent by default for stroke-only shapes)
    /// </summary>
    public string FillColor { get; set; } = "#00000000";

    /// <summary>
    /// Whether shadow is enabled for this annotation
    /// </summary>
    public bool ShadowEnabled { get; set; }

    /// <summary>
    /// Starting point (top-left for rectangles, start for lines/arrows)
    /// </summary>
    public SKPoint StartPoint { get; set; }

    /// <summary>
    /// Ending point (bottom-right for rectangles, end for lines/arrows)
    /// </summary>
    public SKPoint EndPoint { get; set; }

    /// <summary>
    /// Whether this annotation is currently selected
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// Z-order for rendering (higher = on top)
    /// </summary>
    public int ZIndex { get; set; }

    /// <summary>
    /// Rotation angle in degrees (clockwise). Used for rotating annotations.
    /// </summary>
    public float RotationAngle { get; set; }



    /// <summary>
    /// Hit test to determine if a point intersects this annotation
    /// </summary>
    /// <param name="point">Point to test</param>
    /// <param name="tolerance">Hit test tolerance in pixels</param>
    /// <returns>True if the point hits this annotation</returns>
    public abstract bool HitTest(SKPoint point, float tolerance = 10);

    /// <summary>
    /// Get the bounding rectangle for this annotation
    /// </summary>
    public virtual SKRect GetBounds()
    {
        return new SKRect(
            Math.Min(StartPoint.X, EndPoint.X),
            Math.Min(StartPoint.Y, EndPoint.Y),
            Math.Max(StartPoint.X, EndPoint.X),
            Math.Max(StartPoint.Y, EndPoint.Y));
    }

    /// <summary>
    /// Creates a deep clone of this annotation for undo/redo history.
    /// Derived classes should override to handle reference-type properties.
    /// </summary>
    /// <remarks>
    /// XIP0039 Guardrail 3: Clones preserve the original Id so that
    /// <see cref="EditorHistory"/> mementos can restore selection by Id after undo/redo.
    /// Clones live only inside history snapshots and are never added to a live canvas
    /// alongside their source, so Id uniqueness within the active canvas is maintained.
    /// </remarks>
    public virtual Annotation Clone()
    {
        // MemberwiseClone handles value types (SKPoint, float, int, etc.) correctly
        // Derived classes override to deep copy reference types (lists, bitmaps)
        var clone = (Annotation)MemberwiseClone();
        // Id is preserved (not regenerated) so history can match SelectedAnnotationId on restore.
        return clone;
    }

    /// <summary>
    /// Parse hex color string to SKColor
    /// </summary>
    protected SKColor ParseColor(string hexColor)
    {
        return SKColor.Parse(hexColor);
    }


}
