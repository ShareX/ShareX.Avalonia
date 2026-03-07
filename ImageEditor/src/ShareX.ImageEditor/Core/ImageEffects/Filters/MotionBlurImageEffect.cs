using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class MotionBlurImageEffect : FilterImageEffect
{
    public override string Name => "Motion blur";
    public override string IconKey => "IconRotateCW";
    public override bool HasParameters => true;

    public int Distance { get; set; } = 12;
    public float Angle { get; set; }

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        int distance = Math.Clamp(Distance, 1, 200);
        if (distance <= 1)
        {
            return source.Copy();
        }

        int width = source.Width;
        int height = source.Height;
        int right = width - 1;
        int bottom = height - 1;

        float radians = Angle * (MathF.PI / 180f);
        float dx = MathF.Cos(radians);
        float dy = MathF.Sin(radians);

        int start = -((distance - 1) / 2);
        int end = distance / 2;

        SKColor[] srcPixels = source.Pixels;
        SKColor[] dstPixels = new SKColor[srcPixels.Length];

        for (int y = 0; y < height; y++)
        {
            int dstRow = y * width;

            for (int x = 0; x < width; x++)
            {
                int sumR = 0;
                int sumG = 0;
                int sumB = 0;
                int sumA = 0;
                int samples = 0;

                for (int i = start; i <= end; i++)
                {
                    int sx = Clamp((int)MathF.Round(x + (i * dx)), 0, right);
                    int sy = Clamp((int)MathF.Round(y + (i * dy)), 0, bottom);
                    SKColor c = srcPixels[(sy * width) + sx];

                    sumR += c.Red;
                    sumG += c.Green;
                    sumB += c.Blue;
                    sumA += c.Alpha;
                    samples++;
                }

                dstPixels[dstRow + x] = new SKColor(
                    (byte)(sumR / samples),
                    (byte)(sumG / samples),
                    (byte)(sumB / samples),
                    (byte)(sumA / samples));
            }
        }

        return new SKBitmap(width, height, source.ColorType, source.AlphaType)
        {
            Pixels = dstPixels
        };
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}

