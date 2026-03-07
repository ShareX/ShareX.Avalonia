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

using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using ShareX.ImageEditor.Core.Annotations;
using ShareX.ImageEditor.Presentation.Controls;
using ShareX.ImageEditor.Presentation.Rendering;
using SkiaSharp;

namespace ShareX.ImageEditor.Presentation.Rendering;

/// <summary>
/// Indicates how an annotation visual is used by the host.
/// </summary>
public enum AnnotationVisualMode
{
    Persisted,
    Preview
}

/// <summary>
/// Shared factory/synchronizer for annotation visuals used by editor and region-capture hosts.
/// </summary>
public static class AnnotationVisualFactory
{
    /// <summary>
    /// Creates the visual control for the provided annotation.
    /// </summary>
    public static Control? CreateVisualControl(Annotation annotation, AnnotationVisualMode mode = AnnotationVisualMode.Persisted)
    {
        ArgumentNullException.ThrowIfNull(annotation);

        if (mode == AnnotationVisualMode.Preview)
        {
            return annotation switch
            {
                TextAnnotation text => CreateTextPreviewPlaceholder(text),
                SpeechBalloonAnnotation balloon => CreateSpeechBalloonPreviewPlaceholder(balloon),
                SpotlightAnnotation spotlight => CreateSpotlightPreviewPlaceholder(spotlight),
                BlurAnnotation blur => CreateBlurPreviewPlaceholder(blur),
                PixelateAnnotation pixelate => CreatePixelatePreviewPlaceholder(pixelate),
                MagnifyAnnotation magnify => CreateMagnifyPreviewPlaceholder(magnify),
                HighlightAnnotation highlight => CreateHighlightPreviewPlaceholder(highlight),
                _ => CreatePersistedVisualControl(annotation)
            };
        }

        return CreatePersistedVisualControl(annotation);
    }

    /// <summary>
    /// Updates an existing visual control to reflect the annotation's current geometry and position.
    /// </summary>
    public static void UpdateVisualControl(
        Control control,
        Annotation annotation,
        AnnotationVisualMode mode = AnnotationVisualMode.Persisted,
        double canvasWidth = 0,
        double canvasHeight = 0)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(annotation);

        bool ensureMinimumSize = mode == AnnotationVisualMode.Preview;

        switch (annotation)
        {
            case LineAnnotation when control is Line line:
                line.StartPoint = new Avalonia.Point(annotation.StartPoint.X, annotation.StartPoint.Y);
                line.EndPoint = new Avalonia.Point(annotation.EndPoint.X, annotation.EndPoint.Y);
                break;

            case ArrowAnnotation arrow when control is Avalonia.Controls.Shapes.Path arrowPath:
                var start = new Avalonia.Point(annotation.StartPoint.X, annotation.StartPoint.Y);
                var end = new Avalonia.Point(annotation.EndPoint.X, annotation.EndPoint.Y);
                arrowPath.Data = arrow.CreateArrowGeometry(start, end, arrow.StrokeWidth * ArrowAnnotation.ArrowHeadWidthMultiplier);
                break;

            case FreehandAnnotation freehand when control is Avalonia.Controls.Shapes.Path freehandPath:
                freehandPath.Data = freehand.CreateSmoothedGeometry();
                break;

            case NumberAnnotation number when control is Grid numberGrid:
                UpdateNumberGrid(numberGrid, number);
                break;

            case TextAnnotation text when mode == AnnotationVisualMode.Preview && control is Rectangle:
                ApplyBoundsControl(control, text.GetBounds(), ensureMinimumSize: true);
                break;

            case TextAnnotation text when control is OutlinedTextControl textControl:
                var textBounds = text.GetBounds();
                Canvas.SetLeft(textControl, textBounds.Left);
                Canvas.SetTop(textControl, textBounds.Top);

                // Note: The text content, font size, bold/italic, etc. are handled automatically by the control's rendering
                // using the bound Annotation property, but we must apply the transform and invalidate it explicitly here.

                // Apply rotation transform if set
                if (text.RotationAngle != 0)
                {
                    textControl.RenderTransformOrigin = new Avalonia.RelativePoint(0.5, 0.5, Avalonia.RelativeUnit.Relative);
                    textControl.RenderTransform = new RotateTransform(text.RotationAngle);
                }
                else
                {
                    textControl.RenderTransform = null;
                }

                textControl.InvalidateVisual();
                textControl.InvalidateMeasure();
                break;

            case SpeechBalloonAnnotation balloon when mode == AnnotationVisualMode.Preview && control is Rectangle:
                ApplyBoundsControl(control, balloon.GetBounds(), ensureMinimumSize: true);
                break;

            case SpeechBalloonAnnotation balloon when control is SpeechBalloonControl balloonControl:
                balloonControl.Annotation = balloon;
                ApplyBoundsControl(balloonControl, balloon.GetBounds(), ensureMinimumSize);
                balloonControl.InvalidateVisual();
                break;

            case SpotlightAnnotation spotlight when mode == AnnotationVisualMode.Preview && control is Rectangle:
                ApplyBoundsControl(control, spotlight.GetBounds(), ensureMinimumSize: true);
                break;

            case SpotlightAnnotation spotlight when control is SpotlightControl spotlightControl:
                if (canvasWidth > 0 && canvasHeight > 0)
                {
                    spotlight.CanvasSize = new SKSize((float)canvasWidth, (float)canvasHeight);
                }

                spotlightControl.Annotation = spotlight;
                Canvas.SetLeft(spotlightControl, 0);
                Canvas.SetTop(spotlightControl, 0);
                spotlightControl.Width = Math.Max(1, spotlight.CanvasSize.Width);
                spotlightControl.Height = Math.Max(1, spotlight.CanvasSize.Height);
                spotlightControl.InvalidateVisual();
                break;

            case ImageAnnotation imageAnnotation when control is Image imageControl:
                if (imageAnnotation.ImageBitmap != null)
                {
                    imageControl.Source = BitmapConversionHelpers.ToAvaloniBitmap(imageAnnotation.ImageBitmap);
                    imageControl.Width = imageAnnotation.ImageBitmap.Width;
                    imageControl.Height = imageAnnotation.ImageBitmap.Height;
                }

                var imageBounds = imageAnnotation.GetBounds();
                Canvas.SetLeft(imageControl, imageBounds.Left);
                Canvas.SetTop(imageControl, imageBounds.Top);
                break;

            default:
                ApplyBoundsControl(control, annotation.GetBounds(), ensureMinimumSize);
                break;
        }
    }

    private static Control? CreatePersistedVisualControl(Annotation annotation)
    {
        return annotation switch
        {
            RectangleAnnotation rect => rect.CreateVisual(),
            EllipseAnnotation ellipse => ellipse.CreateVisual(),
            LineAnnotation line => line.CreateVisual(),
            ArrowAnnotation arrow => arrow.CreateVisual(),
            TextAnnotation text => text.CreateVisual(),
            SpeechBalloonAnnotation balloon => balloon.CreateVisual(),
            NumberAnnotation number => number.CreateVisual(),
            BlurAnnotation blur => blur.CreateVisual(),
            PixelateAnnotation pixelate => pixelate.CreateVisual(),
            MagnifyAnnotation magnify => magnify.CreateVisual(),
            HighlightAnnotation highlight => highlight.CreateVisual(),
            SpotlightAnnotation spotlight => spotlight.CreateVisual(),
            SmartEraserAnnotation smartEraser => smartEraser.CreateVisual(),
            FreehandAnnotation freehand => freehand.CreateVisual(),
            ImageAnnotation image => CreateImageVisual(image),
            _ => null
        };
    }

    private static Control CreateImageVisual(ImageAnnotation imageAnnotation)
    {
        var image = new Image
        {
            Tag = imageAnnotation
        };

        if (imageAnnotation.ImageBitmap != null)
        {
            image.Source = BitmapConversionHelpers.ToAvaloniBitmap(imageAnnotation.ImageBitmap);
            image.Width = imageAnnotation.ImageBitmap.Width;
            image.Height = imageAnnotation.ImageBitmap.Height;
        }

        return image;
    }

    private static Control CreateTextPreviewPlaceholder(TextAnnotation annotation)
    {
        return new Rectangle
        {
            Stroke = new SolidColorBrush(Color.Parse(annotation.StrokeColor)),
            StrokeThickness = 1,
            StrokeDashArray = new AvaloniaList<double> { 4, 4 },
            Fill = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
            Tag = annotation
        };
    }

    private static Control CreateSpeechBalloonPreviewPlaceholder(SpeechBalloonAnnotation annotation)
    {
        return new Rectangle
        {
            Stroke = new SolidColorBrush(Color.Parse(annotation.StrokeColor)),
            StrokeThickness = annotation.StrokeWidth,
            Fill = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)),
            RadiusX = 10,
            RadiusY = 10,
            Tag = annotation
        };
    }

    private static Control CreateSpotlightPreviewPlaceholder(SpotlightAnnotation annotation)
    {
        return new Rectangle
        {
            Fill = Brushes.Transparent,
            Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
            StrokeThickness = 2,
            StrokeDashArray = new AvaloniaList<double> { 6, 3 },
            Tag = annotation
        };
    }

    private static Control CreateBlurPreviewPlaceholder(BlurAnnotation annotation)
    {
        return new Rectangle
        {
            Fill = new SolidColorBrush(Color.Parse("#200000FF")),
            Stroke = new SolidColorBrush(Color.FromArgb(80, 0, 0, 255)),
            StrokeThickness = 1,
            Tag = annotation
        };
    }

    private static Control CreatePixelatePreviewPlaceholder(PixelateAnnotation annotation)
    {
        return new Rectangle
        {
            Fill = new SolidColorBrush(Color.Parse("#2000FF00")),
            Stroke = new SolidColorBrush(Color.FromArgb(80, 0, 255, 0)),
            StrokeThickness = 1,
            Tag = annotation
        };
    }

    private static Control CreateMagnifyPreviewPlaceholder(MagnifyAnnotation annotation)
    {
        return new Rectangle
        {
            Fill = new SolidColorBrush(Color.FromArgb(30, 211, 211, 211)),
            Stroke = new SolidColorBrush(Color.FromArgb(80, 100, 100, 100)),
            StrokeThickness = 1,
            Tag = annotation
        };
    }

    private static Control CreateHighlightPreviewPlaceholder(HighlightAnnotation annotation)
    {
        Color baseColor = Color.Parse(annotation.StrokeColor);
        Color highlightColor = Color.FromArgb(0x55, baseColor.R, baseColor.G, baseColor.B);
        return new Rectangle
        {
            Fill = new SolidColorBrush(highlightColor),
            Stroke = Brushes.Transparent,
            StrokeThickness = 0,
            Tag = annotation
        };
    }

    private static void ApplyBoundsControl(Control control, SKRect bounds, bool ensureMinimumSize)
    {
        double left = bounds.Left;
        double top = bounds.Top;
        double width = ensureMinimumSize ? Math.Max(1, bounds.Width) : bounds.Width;
        double height = ensureMinimumSize ? Math.Max(1, bounds.Height) : bounds.Height;

        Canvas.SetLeft(control, left);
        Canvas.SetTop(control, top);
        control.Width = width;
        control.Height = height;
    }

    private static void UpdateNumberGrid(Grid grid, NumberAnnotation number)
    {
        double radius = number.Radius;
        Canvas.SetLeft(grid, number.StartPoint.X - radius);
        Canvas.SetTop(grid, number.StartPoint.Y - radius);
        grid.Width = radius * 2;
        grid.Height = radius * 2;

        if (grid.Children.Count > 0 && grid.Children[0] is Avalonia.Controls.Shapes.Ellipse ellipse)
        {
            IBrush fillBrush = string.IsNullOrEmpty(number.FillColor) || number.FillColor == "#00000000"
                ? Brushes.Transparent
                : new SolidColorBrush(Color.Parse(number.FillColor));
            ellipse.Fill = fillBrush;
            ellipse.Stroke = new SolidColorBrush(Color.Parse(number.StrokeColor));
            ellipse.StrokeThickness = number.StrokeWidth;
        }

        if (grid.Children.Count > 1 && grid.Children[1] is TextBlock textBlock)
        {
            textBlock.Text = number.Number.ToString();
            textBlock.FontSize = number.FontSize * 0.6;
        }
    }
}
