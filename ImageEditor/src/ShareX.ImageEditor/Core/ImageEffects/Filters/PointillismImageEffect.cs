using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class PointillismImageEffect : FilterImageEffect
{
    public override string Name => "Pointillism";
    public override string IconKey => "IconSparkles";
    public override bool HasParameters => true;

    public int DotSize { get; set; } = 7; // 2..24
    public float Density { get; set; } = 72f; // 10..100
    public float Jitter { get; set; } = 65f; // 0..100
    public float ColorBoost { get; set; } = 20f; // 0..100
    public float BackgroundMix { get; set; } = 20f; // 0..100
    public int Seed { get; set; } = 2026;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        int dotSize = Math.Clamp(DotSize, 2, 24);
        float density = Math.Clamp(Density, 10f, 100f) / 100f;
        float jitter = Math.Clamp(Jitter, 0f, 100f) / 100f;
        float boost = Math.Clamp(ColorBoost, 0f, 100f) / 100f;
        float backgroundMix = Math.Clamp(BackgroundMix, 0f, 100f) / 100f;

        int width = source.Width;
        int height = source.Height;
        if (width <= 0 || height <= 0)
        {
            return source.Copy();
        }

        SKColor[] srcPixels = source.Pixels;
        SKBitmap result = new SKBitmap(width, height, source.ColorType, source.AlphaType);

        using SKCanvas canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        if (backgroundMix > 0f)
        {
            using SKPaint basePaint = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(255, 255, 255, ProceduralEffectHelper.ClampToByte(backgroundMix * 255f))
            };
            canvas.DrawBitmap(source, 0, 0, basePaint);
        }

        using SKPaint dotPaint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High,
            Style = SKPaintStyle.Fill
        };

        int step = Math.Max(2, dotSize);
        float jitterAmount = jitter * step * 0.6f;

        for (int gy = 0; gy < height; gy += step)
        {
            for (int gx = 0; gx < width; gx += step)
            {
                int cellX = gx / step;
                int cellY = gy / step;
                float presence = ProceduralEffectHelper.Hash01(cellX, cellY, Seed);
                if (presence > density)
                {
                    continue;
                }

                float jx = ((ProceduralEffectHelper.Hash01(cellX, cellY, Seed ^ 111) * 2f) - 1f) * jitterAmount;
                float jy = ((ProceduralEffectHelper.Hash01(cellX, cellY, Seed ^ 257) * 2f) - 1f) * jitterAmount;

                float cx = Math.Clamp(gx + (step * 0.5f) + jx, 0f, width - 1f);
                float cy = Math.Clamp(gy + (step * 0.5f) + jy, 0f, height - 1f);

                SKColor sample = ProceduralEffectHelper.BilinearSample(srcPixels, width, height, cx, cy);
                if (boost > 0f)
                {
                    sample = BoostSaturation(sample, boost);
                }

                float radiusJitter = 0.6f + (ProceduralEffectHelper.Hash01(cellX, cellY, Seed ^ 509) * 0.9f);
                float radius = Math.Max(1f, dotSize * 0.36f * radiusJitter);

                dotPaint.Color = sample;
                canvas.DrawCircle(cx, cy, radius, dotPaint);
            }
        }

        return result;
    }

    private static SKColor BoostSaturation(SKColor color, float amount01)
    {
        float r = color.Red;
        float g = color.Green;
        float b = color.Blue;

        float gray = (r + g + b) / 3f;
        float factor = 1f + amount01;

        return new SKColor(
            ProceduralEffectHelper.ClampToByte(gray + ((r - gray) * factor)),
            ProceduralEffectHelper.ClampToByte(gray + ((g - gray) * factor)),
            ProceduralEffectHelper.ClampToByte(gray + ((b - gray) * factor)),
            color.Alpha);
    }
}
