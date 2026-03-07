using Avalonia.Controls;
using Avalonia.Media;

namespace ShareX.ImageEditor.Core.Annotations;

public partial class MagnifyAnnotation
{
    /// <summary>
    /// Creates the Avalonia visual for this annotation.
    /// </summary>
    public Control CreateVisual()
    {
        return new Avalonia.Controls.Shapes.Rectangle
        {
            Stroke = Brushes.Transparent,
            StrokeThickness = 0,
            Fill = Brushes.Transparent,
            Tag = this
        };
    }
}
