using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Manipulations;

public class ScaleImageEffect : ImageEffect
{
    public override string Name => "Scale";
    public override ImageEffectCategory Category => ImageEffectCategory.Manipulations;
    public override bool HasParameters => true;

    public float WidthPercentage { get; set; } = 100f;
    public float HeightPercentage { get; set; } = 0f;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        if (WidthPercentage <= 0f && HeightPercentage <= 0f)
        {
            return source.Copy();
        }

        int width = (int)Math.Round(WidthPercentage / 100f * source.Width);
        int height = (int)Math.Round(HeightPercentage / 100f * source.Height);

        if (width == 0)
        {
            width = (int)Math.Round((float)height / source.Height * source.Width);
        }
        else if (height == 0)
        {
            height = (int)Math.Round((float)width / source.Width * source.Height);
        }

        if (width <= 0 || height <= 0)
        {
            return source.Copy();
        }

        SKImageInfo info = new SKImageInfo(width, height, source.ColorType, source.AlphaType, source.ColorSpace);
        return source.Resize(info, SKFilterQuality.High) ?? source.Copy();
    }
}
