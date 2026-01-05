#region License Information (GPL v3)

/*
    ShareX.Ava - The Avalonia UI implementation of ShareX
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
using Avalonia.Controls;
using Avalonia.Media;
using ShareX.Editor.Annotations;

namespace ShareX.Ava.UI.Controls;

/// <summary>
/// Custom control for rendering spotlight annotations with proper darkening effect.
/// This is an Avalonia adapter that translates SKCanvas rendering to Avalonia DrawingContext.
/// </summary>
public class SpotlightControl : Control
{
    private SpotlightAnnotation? _annotation;

    public static readonly StyledProperty<SpotlightAnnotation?> AnnotationProperty =
        AvaloniaProperty.Register<SpotlightControl, SpotlightAnnotation?>(nameof(Annotation));

    public SpotlightAnnotation? Annotation
    {
        get => GetValue(AnnotationProperty);
        set => SetValue(AnnotationProperty, value);
    }

    static SpotlightControl()
    {
        AffectsRender<SpotlightControl>(AnnotationProperty);
    }

    public SpotlightControl()
    {
        // Make this control take up the full canvas space
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var annotation = Annotation;
        if (annotation == null) return;
        
        // Avalonia adapter: Translate SkiaSharp rendering to Avalonia DrawingContext
        // The annotation now uses SKCanvas, so we need to replicate its logic here for Avalonia
        
        var canvasSize = annotation.CanvasSize;
        if (canvasSize.Width <= 0 || canvasSize.Height <= 0) return;

        var spotlightRect = annotation.GetBounds();

        // Create dark overlay using path with EvenOdd fill rule
        var pathGeometry = new PathGeometry { FillRule = FillRule.EvenOdd };
        
        // Outer figure: full canvas
        var outerFigure = new PathFigure { StartPoint = new Point(0, 0), IsClosed = true };
        outerFigure.Segments.Add(new LineSegment { Point = new Point(canvasSize.Width, 0) });
        outerFigure.Segments.Add(new LineSegment { Point = new Point(canvasSize.Width, canvasSize.Height) });
        outerFigure.Segments.Add(new LineSegment { Point = new Point(0, canvasSize.Height) });
        pathGeometry.Figures.Add(outerFigure);
        
        // Inner figure: spotlight rectangle (hole)
        var innerFigure = new PathFigure 
        { 
            StartPoint = new Point(spotlightRect.Left, spotlightRect.Top), 
            IsClosed = true 
        };
        innerFigure.Segments.Add(new LineSegment { Point = new Point(spotlightRect.Right, spotlightRect.Top) });
        innerFigure.Segments.Add(new LineSegment { Point = new Point(spotlightRect.Right, spotlightRect.Bottom) });
        innerFigure.Segments.Add(new LineSegment { Point = new Point(spotlightRect.Left, spotlightRect.Bottom) });
        pathGeometry.Figures.Add(innerFigure);
        
        // Draw the overlay (darkens everything except the rectangle)
        var overlayBrush = new SolidColorBrush(Color.FromArgb(annotation.DarkenOpacity, 0, 0, 0));
        context.DrawGeometry(overlayBrush, null, pathGeometry);
    }
}
