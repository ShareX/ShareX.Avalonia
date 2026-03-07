using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ShareX.ImageEditor.Core.Annotations;

namespace ShareX.ImageEditor.Presentation.Controls
{
    /// <summary>
    /// Custom control for rendering spotlight annotations with proper darkening effect
    /// </summary>
    public class SpotlightControl : Control
    {

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
            if (annotation != null && annotation.CanvasSize.Width > 0 && annotation.CanvasSize.Height > 0)
            {
                // CRITICAL FIX: Always render at full canvas size, regardless of control's actual measured bounds
                var canvasW = (double)annotation.CanvasSize.Width;
                var canvasH = (double)annotation.CanvasSize.Height;
                var fullCanvasBounds = new Rect(0, 0, canvasW, canvasH);

                var annotatedBounds = annotation.GetBounds();
                var spotlightRect = new Rect(annotatedBounds.Left, annotatedBounds.Top, annotatedBounds.Width, annotatedBounds.Height);

                // Create path geometry with EvenOdd fill rule to punch a hole
                var geometry = new PathGeometry { FillRule = FillRule.EvenOdd };

                // Outer rectangle - full canvas (darkened area)
                var outerFigure = new PathFigure { StartPoint = fullCanvasBounds.TopLeft, IsClosed = true };
                outerFigure.Segments?.Add(new LineSegment { Point = fullCanvasBounds.TopRight });
                outerFigure.Segments?.Add(new LineSegment { Point = fullCanvasBounds.BottomRight });
                outerFigure.Segments?.Add(new LineSegment { Point = fullCanvasBounds.BottomLeft });
                geometry.Figures?.Add(outerFigure);

                // Inner rectangle - spotlight area (hole - not darkened)
                var holeFigure = new PathFigure { StartPoint = spotlightRect.TopLeft, IsClosed = true };
                holeFigure.Segments?.Add(new LineSegment { Point = spotlightRect.TopRight });
                holeFigure.Segments?.Add(new LineSegment { Point = spotlightRect.BottomRight });
                holeFigure.Segments?.Add(new LineSegment { Point = spotlightRect.BottomLeft });
                geometry.Figures?.Add(holeFigure);

                // Create brush from DarkenOpacity (byte)
                var color = Color.FromUInt32((uint)((annotation.DarkenOpacity << 24) | 0x000000));
                var brush = new SolidColorBrush(color);

                context.DrawGeometry(brush, null, geometry);
            }
        }
    }
}
