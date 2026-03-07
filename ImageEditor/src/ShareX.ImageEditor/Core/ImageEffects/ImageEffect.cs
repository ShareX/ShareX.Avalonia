using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects;

public abstract class ImageEffect
{
    public abstract string Name { get; }

    public abstract ImageEffectCategory Category { get; }

    public virtual bool HasParameters => false;

    public virtual string? IconKey => null;

    public abstract SKBitmap Apply(SKBitmap source);
}
