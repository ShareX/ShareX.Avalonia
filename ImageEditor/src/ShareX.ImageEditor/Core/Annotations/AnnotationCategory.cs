namespace ShareX.ImageEditor.Core.Annotations;

/// <summary>
/// Groups annotation types by functional category, mirroring <see cref="ImageEffects.ImageEffectCategory"/>.
/// Each concrete <see cref="Annotation"/> subclass returns its category via the <see cref="Annotation.Category"/> property.
/// </summary>
public enum AnnotationCategory
{
    /// <summary>Geometric shapes: Arrow, Ellipse, Freehand, Image, Line, Rectangle, Crop, CutOut, SmartEraser.</summary>
    Shapes,

    /// <summary>Pixel-level effects applied to a region: Blur, Highlight, Magnify, Pixelate, Spotlight.</summary>
    Effects,

    /// <summary>Text-based overlays: Number, SpeechBalloon, Text.</summary>
    Text
}
