using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class AnimeSpeedLinesImageEffect : FilterImageEffect
{
    public override string Name => "Anime speed lines";
    public override string IconKey => "IconBurst";
    public override bool HasParameters => true;

    public float Density { get; set; } = 70f; // 10..100
    public float Strength { get; set; } = 65f; // 0..100
    public float FocusRadius { get; set; } = 18f; // 0..80
    public float CenterX { get; set; } = 50f; // 0..100
    public float CenterY { get; set; } = 50f; // 0..100
    public float Contrast { get; set; } = 35f; // 0..100
    public int Seed { get; set; } = 9911;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        float density = Math.Clamp(Density, 10f, 100f) / 100f;
        float strength = Math.Clamp(Strength, 0f, 100f) / 100f;
        float focusRadius = Math.Clamp(FocusRadius, 0f, 80f) / 100f;
        float contrast = Math.Clamp(Contrast, 0f, 100f) / 100f;

        if (strength <= 0.0001f)
        {
            return source.Copy();
        }

        int width = source.Width;
        int height = source.Height;
        float centerX = (Math.Clamp(CenterX, 0f, 100f) / 100f) * (width - 1);
        float centerY = (Math.Clamp(CenterY, 0f, 100f) / 100f) * (height - 1);
        float minDim = MathF.Min(width, height);
        float focusPx = minDim * focusRadius;
        float featherPx = MathF.Max(8f, minDim * 0.22f);
        float freq = 40f + (density * 180f);

        SKColor[] srcPixels = source.Pixels;
        SKColor[] dstPixels = new SKColor[srcPixels.Length];

        Parallel.For(0, height, y =>
        {
            int row = y * width;

            for (int x = 0; x < width; x++)
            {
                SKColor src = srcPixels[row + x];
                float r = src.Red;
                float g = src.Green;
                float b = src.Blue;
                float a = src.Alpha;

                float gray = (0.2126f * r) + (0.7152f * g) + (0.0722f * b);
                float baseDesat = 0.25f + (contrast * 0.45f);
                r = ProceduralEffectHelper.Lerp(r, gray, baseDesat);
                g = ProceduralEffectHelper.Lerp(g, gray, baseDesat);
                b = ProceduralEffectHelper.Lerp(b, gray, baseDesat);

                float dx = x - centerX;
                float dy = y - centerY;
                float dist = MathF.Sqrt((dx * dx) + (dy * dy));
                float radialMask = ProceduralEffectHelper.SmoothStep(focusPx, focusPx + featherPx, dist);

                float theta = MathF.Atan2(dy, dx);
                float bandPhase = (dist * 0.018f) + (ProceduralEffectHelper.Hash01((int)(dist * 0.12f), (int)(theta * 160f), Seed) * 0.85f);
                float stripe = 0.5f + (0.5f * MathF.Sin((theta * freq) + bandPhase));

                float brightLine = MathF.Pow(stripe, 36f);
                float darkLine = MathF.Pow(1f - stripe, 30f);
                float lineMask = radialMask * strength;

                float brightMix = brightLine * lineMask * 0.92f;
                float darkMix = darkLine * lineMask * 0.58f;

                r = ProceduralEffectHelper.Lerp(r, 255f, brightMix);
                g = ProceduralEffectHelper.Lerp(g, 255f, brightMix);
                b = ProceduralEffectHelper.Lerp(b, 255f, brightMix);

                r = ProceduralEffectHelper.Lerp(r, 0f, darkMix);
                g = ProceduralEffectHelper.Lerp(g, 0f, darkMix);
                b = ProceduralEffectHelper.Lerp(b, 0f, darkMix);

                dstPixels[row + x] = new SKColor(
                    ProceduralEffectHelper.ClampToByte(r),
                    ProceduralEffectHelper.ClampToByte(g),
                    ProceduralEffectHelper.ClampToByte(b),
                    ProceduralEffectHelper.ClampToByte(a));
            }
        });

        return new SKBitmap(width, height, source.ColorType, source.AlphaType)
        {
            Pixels = dstPixels
        };
    }
}

