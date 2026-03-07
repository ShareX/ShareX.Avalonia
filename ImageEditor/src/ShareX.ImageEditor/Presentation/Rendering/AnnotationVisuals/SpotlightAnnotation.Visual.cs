using Avalonia.Controls;
using ShareX.ImageEditor.Presentation.Controls;

namespace ShareX.ImageEditor.Core.Annotations;

public partial class SpotlightAnnotation
{
    /// <summary>
    /// Creates the Avalonia visual for this annotation.
    /// </summary>
    public Control CreateVisual()
    {
        return new SpotlightControl
        {
            Annotation = this,
            IsHitTestVisible = false,
            Tag = this
        };
    }
}
