using Avalonia.Controls;
using Avalonia.Media;

namespace ShareX.ImageEditor.Core.Annotations;

public partial class PixelateAnnotation
{
    /// <summary>
    /// Creates the Avalonia visual for this annotation.
    /// </summary>
    public Control CreateVisual()
    {
        return new Avalonia.Controls.Shapes.Rectangle
        {
            Stroke = Brushes.Transparent,
            StrokeThickness = StrokeWidth,
            Fill = new SolidColorBrush(Color.Parse("#2000FF00")),
            Tag = this
        };
    }
}
