using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public enum PixelSortDirection
{
    Horizontal,
    Vertical
}

public enum PixelSortMetric
{
    Brightness,
    Hue
}

public class PixelSortingImageEffect : FilterImageEffect
{
    public override string Name => "Pixel sorting";
    public override string IconKey => "IconArrowsH";
    public override bool HasParameters => true;

    public PixelSortDirection Direction { get; set; } = PixelSortDirection.Vertical;
    public PixelSortMetric Metric { get; set; } = PixelSortMetric.Brightness;
    public float ThresholdLow { get; set; } = 12f; // 0..100
    public float ThresholdHigh { get; set; } = 85f; // 0..100
    public int MinSpanLength { get; set; } = 8; // 2..256
    public int MaxSpanLength { get; set; } = 120; // 2..512
    public float SortProbability { get; set; } = 85f; // 0..100
    public int Seed { get; set; } = 3110;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        int width = source.Width;
        int height = source.Height;
        if (width <= 0 || height <= 0)
        {
            return source.Copy();
        }

        float low = Math.Clamp(ThresholdLow, 0f, 100f) / 100f;
        float high = Math.Clamp(ThresholdHigh, 0f, 100f) / 100f;
        if (high < low)
        {
            (low, high) = (high, low);
        }

        int minSpan = Math.Clamp(MinSpanLength, 2, 256);
        int maxSpan = Math.Clamp(MaxSpanLength, minSpan, 512);
        float probability = Math.Clamp(SortProbability, 0f, 100f) / 100f;

        SKColor[] srcPixels = source.Pixels;
        SKColor[] dstPixels = new SKColor[srcPixels.Length];
        Array.Copy(srcPixels, dstPixels, srcPixels.Length);

        if (Direction == PixelSortDirection.Horizontal)
        {
            SortHorizontal(dstPixels, width, height, low, high, minSpan, maxSpan, probability);
        }
        else
        {
            SortVertical(dstPixels, width, height, low, high, minSpan, maxSpan, probability);
        }

        return new SKBitmap(width, height, source.ColorType, source.AlphaType)
        {
            Pixels = dstPixels
        };
    }

    private void SortHorizontal(
        SKColor[] pixels,
        int width,
        int height,
        float low,
        float high,
        int minSpan,
        int maxSpan,
        float probability)
    {
        for (int y = 0; y < height; y++)
        {
            int x = 0;
            while (x < width)
            {
                int row = y * width;
                float metric = ComputeMetric(pixels[row + x]);
                if (metric < low || metric > high)
                {
                    x++;
                    continue;
                }

                int start = x;
                x++;
                while (x < width)
                {
                    float m = ComputeMetric(pixels[row + x]);
                    if (m < low || m > high)
                    {
                        break;
                    }
                    x++;
                }

                int spanLength = x - start;
                if (spanLength < minSpan)
                {
                    continue;
                }

                int cursor = start;
                while (cursor < x)
                {
                    int remaining = x - cursor;
                    int length = Math.Min(remaining, maxSpan);
                    if (length < minSpan)
                    {
                        break;
                    }

                    float selector = ProceduralEffectHelper.Hash01(cursor, y, Seed ^ 1187);
                    if (selector <= probability)
                    {
                        SortSegment(pixels, row + cursor, length, 1);
                    }

                    cursor += length;
                }
            }
        }
    }

    private void SortVertical(
        SKColor[] pixels,
        int width,
        int height,
        float low,
        float high,
        int minSpan,
        int maxSpan,
        float probability)
    {
        for (int x = 0; x < width; x++)
        {
            int y = 0;
            while (y < height)
            {
                float metric = ComputeMetric(pixels[(y * width) + x]);
                if (metric < low || metric > high)
                {
                    y++;
                    continue;
                }

                int start = y;
                y++;
                while (y < height)
                {
                    float m = ComputeMetric(pixels[(y * width) + x]);
                    if (m < low || m > high)
                    {
                        break;
                    }
                    y++;
                }

                int spanLength = y - start;
                if (spanLength < minSpan)
                {
                    continue;
                }

                int cursor = start;
                while (cursor < y)
                {
                    int remaining = y - cursor;
                    int length = Math.Min(remaining, maxSpan);
                    if (length < minSpan)
                    {
                        break;
                    }

                    float selector = ProceduralEffectHelper.Hash01(x, cursor, Seed ^ 7723);
                    if (selector <= probability)
                    {
                        SortSegment(pixels, (cursor * width) + x, length, width);
                    }

                    cursor += length;
                }
            }
        }
    }

    private void SortSegment(SKColor[] pixels, int startIndex, int length, int stride)
    {
        SKColor[] segment = new SKColor[length];
        for (int i = 0; i < length; i++)
        {
            segment[i] = pixels[startIndex + (i * stride)];
        }

        Array.Sort(segment, (a, b) => ComputeMetric(a).CompareTo(ComputeMetric(b)));

        for (int i = 0; i < length; i++)
        {
            pixels[startIndex + (i * stride)] = segment[i];
        }
    }

    private float ComputeMetric(SKColor color)
    {
        if (Metric == PixelSortMetric.Hue)
        {
            return ComputeHue(color);
        }

        return ((0.2126f * color.Red) + (0.7152f * color.Green) + (0.0722f * color.Blue)) / 255f;
    }

    private static float ComputeHue(SKColor color)
    {
        float r = color.Red / 255f;
        float g = color.Green / 255f;
        float b = color.Blue / 255f;

        float max = MathF.Max(r, MathF.Max(g, b));
        float min = MathF.Min(r, MathF.Min(g, b));
        float delta = max - min;

        if (delta <= 0.0001f)
        {
            return 0f;
        }

        float hue;
        if (max == r)
        {
            hue = ((g - b) / delta) % 6f;
        }
        else if (max == g)
        {
            hue = ((b - r) / delta) + 2f;
        }
        else
        {
            hue = ((r - g) / delta) + 4f;
        }

        hue /= 6f;
        if (hue < 0f) hue += 1f;
        return hue;
    }
}
