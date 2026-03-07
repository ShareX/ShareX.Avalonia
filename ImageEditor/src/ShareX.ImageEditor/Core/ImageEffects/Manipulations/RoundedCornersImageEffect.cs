using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Manipulations;

public class RoundedCornersImageEffect : ImageEffect
{
    public override string Name => "Rounded Corners";
    public override ImageEffectCategory Category => ImageEffectCategory.Manipulations;
    public override bool HasParameters => true;
    public int CornerRadius { get; set; } = 20;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (CornerRadius <= 0) return source.Copy();

        SKBitmap result = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);
        using SKCanvas canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        using SKPath clipPath = new SKPath();
        clipPath.AddRoundRect(new SKRect(0, 0, source.Width, source.Height), CornerRadius, CornerRadius);
        canvas.ClipPath(clipPath, SKClipOperation.Intersect, true);

        canvas.DrawBitmap(source, 0, 0);
        return result;
    }
}

