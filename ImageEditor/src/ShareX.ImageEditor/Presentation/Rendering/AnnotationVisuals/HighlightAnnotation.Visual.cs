using Avalonia.Controls;
using Avalonia.Media;

namespace ShareX.ImageEditor.Core.Annotations;

public partial class HighlightAnnotation
{
    /// <summary>
    /// Creates the Avalonia visual for this annotation.
    /// </summary>
    public Control CreateVisual()
    {
        var baseColor = Color.Parse(FillColor ?? "#FFFF00");
        var highlightColor = Color.FromArgb(0x55, baseColor.R, baseColor.G, baseColor.B);

        return new Avalonia.Controls.Shapes.Rectangle
        {
            Fill = new SolidColorBrush(highlightColor),
            Stroke = Brushes.Transparent,
            StrokeThickness = 0,
            Tag = this
        };
    }
}
