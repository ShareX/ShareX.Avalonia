using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Adjustments;

public class DuotoneGradientMapImageEffect : AdjustmentImageEffect
{
    public override string Name => "Duotone / Gradient map";
    public override string IconKey => "IconDroplets";

    public SKColor ShadowColor { get; set; } = new SKColor(24, 28, 78);
    public SKColor MidtoneColor { get; set; } = new SKColor(182, 60, 132);
    public SKColor HighlightColor { get; set; } = new SKColor(255, 224, 132);

    public float Contrast { get; set; } = 110f; // 50..200
    public float Gamma { get; set; } = 1f; // 0.3..3
    public float Blend { get; set; } = 100f; // 0..100

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        float contrast = Math.Clamp(Contrast, 50f, 200f) / 100f;
        float gamma = Math.Clamp(Gamma, 0.3f, 3f);
        float blend = Math.Clamp(Blend, 0f, 100f) / 100f;

        int width = source.Width;
        int height = source.Height;
        if (width <= 0 || height <= 0)
        {
            return source.Copy();
        }

        SKColor[] srcPixels = source.Pixels;
        SKColor[] dstPixels = new SKColor[srcPixels.Length];

        for (int i = 0; i < srcPixels.Length; i++)
        {
            SKColor src = srcPixels[i];
            float lum = ((0.2126f * src.Red) + (0.7152f * src.Green) + (0.0722f * src.Blue)) / 255f;
            lum = Math.Clamp(((lum - 0.5f) * contrast) + 0.5f, 0f, 1f);
            lum = MathF.Pow(lum, 1f / gamma);

            SKColor mapped = SampleThreeColorGradient(lum, ShadowColor, MidtoneColor, HighlightColor);

            float r = ProceduralEffectHelper.Lerp(src.Red, mapped.Red, blend);
            float g = ProceduralEffectHelper.Lerp(src.Green, mapped.Green, blend);
            float b = ProceduralEffectHelper.Lerp(src.Blue, mapped.Blue, blend);

            dstPixels[i] = new SKColor(
                ProceduralEffectHelper.ClampToByte(r),
                ProceduralEffectHelper.ClampToByte(g),
                ProceduralEffectHelper.ClampToByte(b),
                src.Alpha);
        }

        return new SKBitmap(width, height, source.ColorType, source.AlphaType)
        {
            Pixels = dstPixels
        };
    }

    private static SKColor SampleThreeColorGradient(float t, SKColor shadows, SKColor mids, SKColor highs)
    {
        t = Math.Clamp(t, 0f, 1f);
        if (t <= 0.5f)
        {
            return Lerp(shadows, mids, t * 2f);
        }

        return Lerp(mids, highs, (t - 0.5f) * 2f);
    }

    private static SKColor Lerp(SKColor a, SKColor b, float t)
    {
        return new SKColor(
            ProceduralEffectHelper.ClampToByte(ProceduralEffectHelper.Lerp(a.Red, b.Red, t)),
            ProceduralEffectHelper.ClampToByte(ProceduralEffectHelper.Lerp(a.Green, b.Green, t)),
            ProceduralEffectHelper.ClampToByte(ProceduralEffectHelper.Lerp(a.Blue, b.Blue, t)),
            255);
    }
}
