using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class SpinBlurImageEffect : FilterImageEffect
{
    public override string Name => "Spin blur";
    public override string IconKey => "IconArrowsSpin";
    public override bool HasParameters => true;

    public float Angle { get; set; } = 20f; // 0..180
    public int Samples { get; set; } = 24; // 4..64
    public float CenterX { get; set; } = 50f; // 0..100
    public float CenterY { get; set; } = 50f; // 0..100

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        float angle = Math.Clamp(Angle, 0f, 180f);
        int sampleCount = Math.Clamp(Samples, 4, 64);
        if (angle <= 0.001f)
        {
            return source.Copy();
        }

        int width = source.Width;
        int height = source.Height;
        float cx = (Math.Clamp(CenterX, 0f, 100f) / 100f) * (width - 1);
        float cy = (Math.Clamp(CenterY, 0f, 100f) / 100f) * (height - 1);

        SKColor[] srcPixels = source.Pixels;
        SKColor[] dstPixels = new SKColor[srcPixels.Length];

        float maxRadians = angle * (MathF.PI / 180f);
        float half = (sampleCount - 1) * 0.5f;
        float invHalf = half <= 0.0001f ? 1f : 1f / half;
        float[] sinValues = new float[sampleCount];
        float[] cosValues = new float[sampleCount];
        float[] weights = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (i - half) * invHalf; // -1..1
            float theta = t * maxRadians;
            sinValues[i] = MathF.Sin(theta);
            cosValues[i] = MathF.Cos(theta);
            weights[i] = 1f - (MathF.Abs(t) * 0.7f);
        }

        Parallel.For(0, height, y =>
        {
            int row = y * width;

            for (int x = 0; x < width; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                if ((dx * dx) + (dy * dy) < 0.25f)
                {
                    dstPixels[row + x] = srcPixels[row + x];
                    continue;
                }

                float sumR = 0f;
                float sumG = 0f;
                float sumB = 0f;
                float sumA = 0f;
                float sumW = 0f;

                for (int i = 0; i < sampleCount; i++)
                {
                    float sin = sinValues[i];
                    float cos = cosValues[i];
                    float w = weights[i];

                    float sampleX = cx + ((dx * cos) - (dy * sin));
                    float sampleY = cy + ((dx * sin) + (dy * cos));
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
