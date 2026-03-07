using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;
using System.Collections.Concurrent;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class LensBlurImageEffect : FilterImageEffect
{
    private readonly record struct KernelOffset(int X, int Y, float Weight);
    private static readonly ConcurrentDictionary<int, KernelOffset[]> KernelCache = new();

    public override string Name => "Lens blur (bokeh)";
    public override string IconKey => "IconCircle";
    public override bool HasParameters => true;

    public int Radius { get; set; } = 8; // 1..15
    public float HighlightThreshold { get; set; } = 70f; // 0..100
    public float HighlightBoost { get; set; } = 85f; // 0..200

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        int radius = Math.Clamp(Radius, 1, 15);
        float threshold = Math.Clamp(HighlightThreshold, 0f, 100f) / 100f;
        float boost = Math.Clamp(HighlightBoost, 0f, 200f) / 100f;
        bool useHighlightBoost = boost > 0.001f;
        float thresholdInv = 1f / Math.Max(0.0001f, 1f - threshold);

        if (radius <= 1 && boost <= 0.01f)
        {
            return source.Copy();
        }

        int width = source.Width;
        int height = source.Height;
        int right = width - 1;
        int bottom = height - 1;

        SKColor[] srcPixels = source.Pixels;
        SKColor[] dstPixels = new SKColor[srcPixels.Length];
        KernelOffset[] kernel = GetKernel(radius);
        float[]? luminance = null;

        if (useHighlightBoost)
        {
            luminance = new float[srcPixels.Length];
            Parallel.For(0, srcPixels.Length, i =>
            {
                SKColor c = srcPixels[i];
                luminance[i] = ((0.2126f * c.Red) + (0.7152f * c.Green) + (0.0722f * c.Blue)) / 255f;
            });
        }

        Parallel.For(0, height, y =>
        {
            int dstRow = y * width;

            for (int x = 0; x < width; x++)
            {
                float sumR = 0f;
                float sumG = 0f;
                float sumB = 0f;
                float sumA = 0f;
                float sumW = 0f;

                for (int i = 0; i < kernel.Length; i++)
                {
                    KernelOffset k = kernel[i];
                    int sx = x + k.X;
                    if (sx < 0) sx = 0;
                    else if (sx > right) sx = right;

                    int sy = y + k.Y;
                    if (sy < 0) sy = 0;
                    else if (sy > bottom) sy = bottom;

                    int srcIndex = (sy * width) + sx;
                    SKColor c = srcPixels[srcIndex];

                    float highlightWeight = 1f;
                    if (useHighlightBoost)
                    {
                        float lum = luminance![srcIndex];
                        if (lum > threshold)
                        {
                            float over = (lum - threshold) * thresholdInv;
                            highlightWeight += over * boost;
                        }
                    }

                    float w = k.Weight * highlightWeight;
                    sumR += c.Red * w;
                    sumG += c.Green * w;
                    sumB += c.Blue * w;
                    sumA += c.Alpha * w;
                    sumW += w;
                }

                if (sumW <= 0.0001f)
                {
                    dstPixels[dstRow + x] = srcPixels[dstRow + x];
                    continue;
                }

                float inv = 1f / sumW;
                dstPixels[dstRow + x] = new SKColor(
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

    private static KernelOffset[] GetKernel(int radius)
    {
        return KernelCache.GetOrAdd(radius, BuildDiskKernel);
    }

    private static KernelOffset[] BuildDiskKernel(int radius)
    {
        List<KernelOffset> offsets = new();
        int radiusSq = radius * radius;
        float invRadius = 1f / radius;

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                int distSq = (x * x) + (y * y);
                if (distSq > radiusSq)
                {
                    continue;
                }

                float distance = MathF.Sqrt(distSq) * invRadius;
                float apertureWeight = 1f - (distance * 0.35f);
                offsets.Add(new KernelOffset(x, y, apertureWeight));
            }
        }

        return offsets.ToArray();
    }
}
