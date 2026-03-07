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
using ShareX.ImageEditor.Core.Annotations;
using SkiaSharp;

namespace ShareX.ImageEditor.Presentation.Controls
{
    /// <summary>
    /// Custom control for rendering a speech balloon with a draggable tail using Avalonia Path
    /// </summary>
    public class SpeechBalloonControl : Control
    {
        public static readonly StyledProperty<SpeechBalloonAnnotation?> AnnotationProperty =
            AvaloniaProperty.Register<SpeechBalloonControl, SpeechBalloonAnnotation?>(nameof(Annotation));

        public SpeechBalloonAnnotation? Annotation
        {
            get => GetValue(AnnotationProperty);
            set => SetValue(AnnotationProperty, value);
        }

        static SpeechBalloonControl()
        {
            AffectsRender<SpeechBalloonControl>(AnnotationProperty);
            AffectsMeasure<SpeechBalloonControl>(AnnotationProperty);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (Annotation == null || Bounds.Width <= 0 || Bounds.Height <= 0) return;

            // Get the annotation's bounds
            var annotationBounds = Annotation.GetBounds();
            var width = Math.Max(annotationBounds.Width, 20);
            var height = Math.Max(annotationBounds.Height, 20);

            // Create the speech balloon path using Avalonia geometry
            var geometry = CreateSpeechBalloonGeometry(width, height, Annotation.TailPoint);

            // Parse colors
            var strokeColor = Color.Parse(Annotation.StrokeColor);
            var fillColor = Color.Parse(Annotation.FillColor);

            // Draw fill
            context.DrawGeometry(
                new SolidColorBrush(fillColor),
                null,
                geometry
            );

            // Draw stroke
            context.DrawGeometry(
                null,
                new Pen(new SolidColorBrush(strokeColor), Annotation.StrokeWidth),
                geometry
            );

            // Draw text if present
            if (!string.IsNullOrEmpty(Annotation.Text))
            {
                var textColor = Color.Parse(Annotation.TextColor);
                var typeface = new Typeface(FontFamily.Default);
                var formattedText = new FormattedText(
                    Annotation.Text,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    Annotation.FontSize,
                    new SolidColorBrush(textColor)
                );

                // Calculate centered text position with padding
                var padding = 12; // Adjusted to match TextBox padding

                // Allow wrapping
                var maxTextWidth = Math.Max(0, width - (padding * 2));
                formattedText.MaxTextWidth = maxTextWidth;
                var textX = Math.Max(padding, (width - formattedText.Width) / 2);
                var textY = Math.Max(padding, (height - formattedText.Height) / 2);

                // Ensure text stays within bounds
                textX = Math.Min(textX, width - formattedText.Width - padding);
                textY = Math.Min(textY, height - formattedText.Height - padding);

                context.DrawText(formattedText, new Point(textX, textY));
            }
        }

        private Geometry CreateSpeechBalloonGeometry(float width, float height, SKPoint tailPoint)
        {
            var geometry = new StreamGeometry();

            using (var ctx = geometry.Open())
            {
                float radius = 10;
                float left = 0;
                float top = 0;
                float right = width;
                float bottom = height;

                // Default tail point if not set - place it below and centered
                var absoluteTailPoint = tailPoint == default
                    ? new SKPoint(Annotation!.StartPoint.X + width / 2, Annotation!.StartPoint.Y + height + 30)
                    : tailPoint;

                // Convert tail point to relative coordinates (relative to the balloon's top-left corner)
                var renderTailPoint = new Point(
                    absoluteTailPoint.X - Annotation!.StartPoint.X,
                    absoluteTailPoint.Y - Annotation!.StartPoint.Y
                );

                float tailX = (float)renderTailPoint.X;
                float tailY = (float)renderTailPoint.Y;

                // Determine which edge the tail should connect to based on tail position
                // Strategy: Find which edge is closest to the tail point
                float centerX = width / 2;
                float centerY = height / 2;

                // Calculate distances to each edge
                float distToTop = Math.Abs(tailY - top);
                float distToBottom = Math.Abs(tailY - bottom);
                float distToLeft = Math.Abs(tailX - left);
                float distToRight = Math.Abs(tailX - right);

                // Determine primary direction: use angle from center to tail
                float dx = tailX - centerX;
                float dy = tailY - centerY;

                // Determine which edge to connect to based on angle
                // Use a simplified approach: check if tail is more horizontal or vertical
                bool isMoreHorizontal = Math.Abs(dx) > Math.Abs(dy);

                bool tailOnTop = false;
                bool tailOnBottom = false;
                bool tailOnLeft = false;
                bool tailOnRight = false;

                if (isMoreHorizontal)
                {
                    // Horizontal - choose left or right
                    if (dx < 0)
                        tailOnLeft = true;
                    else
                        tailOnRight = true;
                }
                else
                {
                    // Vertical - choose top or bottom
                    if (dy < 0)
                        tailOnTop = true;
                    else
                        tailOnBottom = true;
                }

                float baseTailWidth = 20;
                float minConnectionMargin = radius + 5;

                // Start at top-left after the rounded corner
                ctx.BeginFigure(new Point(left + radius, top), true);

                // Top edge - check if tail connects here
                if (tailOnTop)
                {
                    float minX = left + minConnectionMargin;
                    float maxX = right - minConnectionMargin;
                    if (minX > maxX) minX = maxX = (left + right) / 2;

                    float connectionX = Math.Clamp(tailX, minX, maxX);
                    float halfTailWidth = baseTailWidth / 2;
                    float tailStartX = Math.Max(minX, connectionX - halfTailWidth);
                    float tailEndX = Math.Min(maxX, connectionX + halfTailWidth);

                    // Draw to tail start, then to tail point (outside), then to tail end
                    ctx.LineTo(new Point(tailStartX, top));
                    ctx.LineTo(renderTailPoint); // Tail point is outside
                    ctx.LineTo(new Point(tailEndX, top));
                    ctx.LineTo(new Point(right - radius, top));
                }
                else
                {
                    ctx.LineTo(new Point(right - radius, top));
                }

                // Top-right corner
                ctx.ArcTo(
                    new Point(right, top + radius),
                    new Size(radius, radius),
                    0,
                    false,
                    SweepDirection.Clockwise
                );

                // Right edge - check if tail connects here
                if (tailOnRight)
                {
                    float minY = top + minConnectionMargin;
                    float maxY = bottom - minConnectionMargin;
                    if (minY > maxY) minY = maxY = (top + bottom) / 2;

                    float connectionY = Math.Clamp(tailY, minY, maxY);
                    float halfTailWidth = baseTailWidth / 2;
                    float tailStartY = Math.Max(minY, connectionY - halfTailWidth);
                    float tailEndY = Math.Min(maxY, connectionY + halfTailWidth);

                    ctx.LineTo(new Point(right, tailStartY));
                    ctx.LineTo(renderTailPoint);
                    ctx.LineTo(new Point(right, tailEndY));
                    ctx.LineTo(new Point(right, bottom - radius));
                }
                else
                {
                    ctx.LineTo(new Point(right, bottom - radius));
                }

                // Bottom-right corner
                ctx.ArcTo(
                    new Point(right - radius, bottom),
                    new Size(radius, radius),
                    0,
                    false,
                    SweepDirection.Clockwise
                );

                // Bottom edge - check if tail connects here
                if (tailOnBottom)
                {
                    float minX = left + minConnectionMargin;
                    float maxX = right - minConnectionMargin;
                    if (minX > maxX) minX = maxX = (left + right) / 2;

                    float connectionX = Math.Clamp(tailX, minX, maxX);
                    float halfTailWidth = baseTailWidth / 2;

                    // For bottom edge, draw from right to left, so reverse order
                    float tailStartX = Math.Min(maxX, connectionX + halfTailWidth);
                    float tailEndX = Math.Max(minX, connectionX - halfTailWidth);

                    ctx.LineTo(new Point(tailStartX, bottom));
                    ctx.LineTo(renderTailPoint);
                    ctx.LineTo(new Point(tailEndX, bottom));
                    ctx.LineTo(new Point(left + radius, bottom));
                }
                else
                {
                    ctx.LineTo(new Point(left + radius, bottom));
                }

                // Bottom-left corner
                ctx.ArcTo(
                    new Point(left, bottom - radius),
                    new Size(radius, radius),
                    0,
                    false,
                    SweepDirection.Clockwise
                );

                // Left edge - check if tail connects here
                if (tailOnLeft)
                {
                    float minY = top + minConnectionMargin;
                    float maxY = bottom - minConnectionMargin;
                    if (minY > maxY) minY = maxY = (top + bottom) / 2;

                    float connectionY = Math.Clamp(tailY, minY, maxY);
                    float halfTailWidth = baseTailWidth / 2;

                    // For left edge, draw from bottom to top, so reverse order
                    float tailStartY = Math.Min(maxY, connectionY + halfTailWidth);
                    float tailEndY = Math.Max(minY, connectionY - halfTailWidth);

                    ctx.LineTo(new Point(left, tailStartY));
                    ctx.LineTo(renderTailPoint);
                    ctx.LineTo(new Point(left, tailEndY));
                    ctx.LineTo(new Point(left, top + radius));
                }
                else
                {
                    ctx.LineTo(new Point(left, top + radius));
                }

                // Top-left corner
                ctx.ArcTo(
                    new Point(left + radius, top),
                    new Size(radius, radius),
                    0,
                    false,
                    SweepDirection.Clockwise
                );

                ctx.EndFigure(true);
            }

            return geometry;
        }
    }
}
