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
using System.Globalization;

namespace ShareX.ImageEditor.Presentation.Controls
{
    /// <summary>
    /// Custom control for rendering text with both an outline (stroke) and a fill.
    /// Avalonia's native TextBox does not support text strokes, so we render via FormattedText and Geometry.
    /// </summary>
    public class OutlinedTextControl : Control
    {
        public static readonly StyledProperty<TextAnnotation?> AnnotationProperty =
            AvaloniaProperty.Register<OutlinedTextControl, TextAnnotation?>(nameof(Annotation));

        public TextAnnotation? Annotation
        {
            get => GetValue(AnnotationProperty);
            set => SetValue(AnnotationProperty, value);
        }

        static OutlinedTextControl()
        {
            AffectsRender<OutlinedTextControl>(AnnotationProperty);
            AffectsMeasure<OutlinedTextControl>(AnnotationProperty);
        }

        public OutlinedTextControl()
        {
            ClipToBounds = false;
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (Annotation == null || string.IsNullOrEmpty(Annotation.Text)) return;

            var typeface = new Typeface(
                Annotation.FontFamily,
                Annotation.IsItalic ? FontStyle.Italic : FontStyle.Normal,
                Annotation.IsBold ? FontWeight.Bold : FontWeight.Normal);

            var formattedText = new FormattedText(
                Annotation.Text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                Annotation.FontSize,
                Brushes.Black); // Avalonia requires a brush to properly construct text extents in some backends

            // Create geometry from formatted text, centered within the control bounds
            double textWidth = formattedText.Width;
            double textHeight = formattedText.Height;
            double originX = (Bounds.Width - textWidth) / 2.0;
            double originY = (Bounds.Height - textHeight) / 2.0;

            var textGeometry = formattedText.BuildGeometry(new Point(originX, originY));
            if (textGeometry == null) return;

            // Setup brushes based on the annotation's color properties.
            // Text color is now TextColor, Outline color is StrokeColor.

            // Standard behavior in many editors is that transparent fill means no fill.
            IBrush? fillBrush = null;
            if (!string.IsNullOrEmpty(Annotation.TextColor))
            {
                var textColor = Color.Parse(Annotation.TextColor);
                if (textColor.A > 0)
                {
                    fillBrush = new SolidColorBrush(textColor);
                }
            }

            IPen? strokePen = null;
            if (Annotation.StrokeWidth > 0 && !string.IsNullOrEmpty(Annotation.StrokeColor))
            {
                var strokeColor = Color.Parse(Annotation.StrokeColor);
                if (strokeColor.A > 0)
                {
                    strokePen = new Pen(new SolidColorBrush(strokeColor), Annotation.StrokeWidth, lineJoin: PenLineJoin.Round);
                }
            }

            // Draw main text (Stroke then Fill, so fill is on top of stroke)
            // To get the stroke BEHIND the fill (standard typography outline), we must draw the stroke geometry first, then fill geometry.
            if (strokePen != null)
            {
                context.DrawGeometry(null, strokePen, textGeometry);
            }

            if (fillBrush != null)
            {
                context.DrawGeometry(fillBrush, null, textGeometry);
            }

            // Draw underline if enabled
            if (Annotation.IsUnderline)
            {
                var underlineBrush = fillBrush ?? (strokePen?.Brush) ?? Brushes.Black;
                var underlineThickness = Math.Max(1.0, Annotation.FontSize / 14.0);
                var underlineY = originY + textHeight * 0.95;
                var underlinePen = new Pen(underlineBrush, underlineThickness);
                context.DrawLine(underlinePen,
                    new Point(originX, underlineY),
                    new Point(originX + textWidth, underlineY));
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (Annotation == null || string.IsNullOrEmpty(Annotation.Text))
            {
                return new Size(0, 0);
            }

            var typeface = new Typeface(
                Annotation.FontFamily,
                Annotation.IsItalic ? FontStyle.Italic : FontStyle.Normal,
                Annotation.IsBold ? FontWeight.Bold : FontWeight.Normal);

            var formattedText = new FormattedText(
                Annotation.Text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                Annotation.FontSize,
                Brushes.Black);

            // Add padding (4px all sides) + stroke width padding to avoid clipping
            double strokePadding = Annotation.StrokeWidth;
            double padding = 8; // 4 * 2

            double width = formattedText.Width + padding + strokePadding;
            double height = formattedText.Height + padding + strokePadding;

            if (double.IsNaN(width) || width < 0) width = 0;
            if (double.IsNaN(height) || height < 0) height = 0;

            return new Size(width, height);
        }
    }
}
