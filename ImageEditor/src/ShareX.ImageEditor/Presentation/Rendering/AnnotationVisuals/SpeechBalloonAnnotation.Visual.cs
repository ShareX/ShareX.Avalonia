using Avalonia.Controls;
using ShareX.ImageEditor.Presentation.Controls;

namespace ShareX.ImageEditor.Core.Annotations;

public partial class SpeechBalloonAnnotation
{
    /// <summary>
    /// Creates the Avalonia visual for this annotation (SpeechBalloonControl)
    /// </summary>
    public Control CreateVisual()
    {
        var control = new SpeechBalloonControl
        {
            Annotation = this,
            IsHitTestVisible = true,
            Tag = this
        };

        if (ShadowEnabled)
        {
            control.Effect = new Avalonia.Media.DropShadowEffect
            {
                OffsetX = 3,
                OffsetY = 3,
                BlurRadius = 4,
                Color = Avalonia.Media.Color.FromArgb(128, 0, 0, 0)
            };
        }

        return control;
    }
}
