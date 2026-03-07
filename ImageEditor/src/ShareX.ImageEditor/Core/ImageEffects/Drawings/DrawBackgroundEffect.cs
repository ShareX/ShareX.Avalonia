using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Drawings;

public sealed class DrawBackgroundEffect : ImageEffect
{
    public SKColor Color { get; set; } = SKColors.Black;

    public override string Name => "Background";

    public override ImageEffectCategory Category => ImageEffectCategory.Drawings;

    public override bool HasParameters => true;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        SKBitmap result = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);
        using SKCanvas canvas = new SKCanvas(result);

        using SKPaint paint = new SKPaint { IsAntialias = true, Color = Color };
        canvas.DrawRect(0, 0, source.Width, source.Height, paint);
        canvas.DrawBitmap(source, 0, 0);
        return result;
    }
}
