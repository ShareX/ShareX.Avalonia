using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class ThermalVisionImageEffect : FilterImageEffect
{
    public override string Name => "Thermal vision";
    public override string IconKey => "IconFlame";
    public override bool HasParameters => true;

    public int Levels { get; set; } = 8; // 3..16
    public float Contrast { get; set; } = 135f; // 50..200
    public float Glow { get; set; } = 28f; // 0..100
    public float Blend { get; set; } = 100f; // 0..100
    public bool Invert { get; set; }

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        int levels = Math.Clamp(Levels, 3, 16);
        float contrast = Math.Clamp(Contrast, 50f, 200f) / 100f;
        float glow = Math.Clamp(Glow, 0f, 100f) / 100f;
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

            float luminance = ((0.2126f * src.Red) + (0.7152f * src.Green) + (0.0722f * src.Blue)) / 255f;
            luminance = Math.Clamp(((luminance - 0.5f) * contrast) + 0.5f, 0f, 1f);
            if (Invert)
            {
                luminance = 1f - luminance;
            }

            float quantized = MathF.Round(luminance * (levels - 1)) / Math.Max(1f, levels - 1f);
            SKColor thermal = SampleThermalGradient(quantized);

            float glowBoost = glow * MathF.Pow(quantized, 2.2f);
            float tr = thermal.Red + (thermal.Red * glowBoost * 0.20f);
            float tg = thermal.Green + (thermal.Green * glowBoost * 0.25f);
            float tb = thermal.Blue + (thermal.Blue * glowBoost * 0.35f);

            byte r = ProceduralEffectHelper.ClampToByte(ProceduralEffectHelper.Lerp(src.Red, tr, blend));
            byte g = ProceduralEffectHelper.ClampToByte(ProceduralEffectHelper.Lerp(src.Green, tg, blend));
            byte b = ProceduralEffectHelper.ClampToByte(ProceduralEffectHelper.Lerp(src.Blue, tb, blend));

            dstPixels[i] = new SKColor(r, g, b, src.Alpha);
        }

        return new SKBitmap(width, height, source.ColorType, source.AlphaType)
        {
            Pixels = dstPixels
        };
    }

    private static SKColor SampleThermalGradient(float t)
    {
        t = Math.Clamp(t, 0f, 1f);

        if (t <= 0.20f) return Lerp(new SKColor(0, 0, 52), new SKColor(0, 34, 170), t / 0.20f);
        if (t <= 0.40f) return Lerp(new SKColor(0, 34, 170), new SKColor(0, 160, 255), (t - 0.20f) / 0.20f);
        if (t <= 0.60f) return Lerp(new SKColor(0, 160, 255), new SKColor(0, 255, 210), (t - 0.40f) / 0.20f);
        if (t <= 0.76f) return Lerp(new SKColor(0, 255, 210), new SKColor(255, 255, 0), (t - 0.60f) / 0.16f);
        if (t <= 0.90f) return Lerp(new SKColor(255, 255, 0), new SKColor(255, 80, 0), (t - 0.76f) / 0.14f);
        return Lerp(new SKColor(255, 80, 0), new SKColor(255, 255, 255), (t - 0.90f) / 0.10f);
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
