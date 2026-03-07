using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class ZoomBlurImageEffect : FilterImageEffect
{
    public override string Name => "Zoom blur";
    public override string IconKey => "IconMagnifyingGlass";
    public override bool HasParameters => true;

    public float Strength { get; set; } = 35f; // 0..100
    public int Samples { get; set; } = 24; // 4..64
    public float CenterX { get; set; } = 50f; // 0..100
    public float CenterY { get; set; } = 50f; // 0..100

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        float strength = Math.Clamp(Strength, 0f, 100f) / 100f;
        int sampleCount = Math.Clamp(Samples, 4, 64);
        if (strength <= 0.0001f)
        {
            return source.Copy();
        }

        int width = source.Width;
        int height = source.Height;
        float cx = (Math.Clamp(CenterX, 0f, 100f) / 100f) * (width - 1);
        float cy = (Math.Clamp(CenterY, 0f, 100f) / 100f) * (height - 1);

        SKColor[] srcPixels = source.Pixels;
        SKColor[] dstPixels = new SKColor[srcPixels.Length];

        float invSamples = sampleCount <= 1 ? 1f : 1f / (sampleCount - 1);
        float[] factors = new float[sampleCount];
        float[] weights = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i * invSamples;
            factors[i] = 1f - (t * strength);
            weights[i] = 1f - (t * 0.6f);
        }

        Parallel.For(0, height, y =>
        {
            int row = y * width;

            for (int x = 0; x < width; x++)
            {
                float dx = x - cx;
                float dy = y - cy;

                float sumR = 0f;
                float sumG = 0f;
                float sumB = 0f;
                float sumA = 0f;
                float sumW = 0f;

                for (int i = 0; i < sampleCount; i++)
                {
                    float factor = factors[i];
                    float w = weights[i];

                    float sampleX = cx + (dx * factor);
                    float sampleY = cy + (dy * factor);
                    SKColor sample = ProceduralEffectHelper.BilinearSample(srcPixels, width, height, sampleX, sampleY);

                    sumR += sample.Red * w;
                    sumG += sample.Green * w;
                    sumB += sample.Blue * w;
                    sumA += sample.Alpha * w;
                    sumW += w;
                }

                float inv = 1f / Math.Max(0.0001f, sumW);
                dstPixels[row + x] = new SKColor(
                    ProceduralEffectHelper.ClampToByte(sumR * inv),
                    ProceduralEffectHelper.ClampToByte(sumG * inv),
                    ProceduralEffectHelper.ClampToByte(sumB * inv),
                    ProceduralEffectHelper.ClampToByte(sumA * inv));
            }
        });

        return new SKBitmap(width, height, source.ColorType, source.AlphaType)
        {
            Pixels = dstPixels
        };
    }
}
