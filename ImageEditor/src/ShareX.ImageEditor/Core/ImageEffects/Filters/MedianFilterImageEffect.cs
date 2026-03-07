using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class MedianFilterImageEffect : FilterImageEffect
{
    public override string Name => "Median filter";
    public override string IconKey => "IconTableCells";
    public override bool HasParameters => true;

    public int Radius { get; set; } = 1;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        int radius = Math.Clamp(Radius, 1, 5);
        int width = source.Width;
        int height = source.Height;
        int right = width - 1;
        int bottom = height - 1;

        SKColor[] srcPixels = source.Pixels;
        SKColor[] dstPixels = new SKColor[srcPixels.Length];

        int diameter = (radius * 2) + 1;
        int maxSamples = diameter * diameter;

        byte[] rValues = new byte[maxSamples];
        byte[] gValues = new byte[maxSamples];
        byte[] bValues = new byte[maxSamples];
        byte[] aValues = new byte[maxSamples];

        for (int y = 0; y < height; y++)
        {
            int dstRow = y * width;

            for (int x = 0; x < width; x++)
            {
                int count = 0;

                for (int ky = -radius; ky <= radius; ky++)
                {
                    int sy = Clamp(y + ky, 0, bottom);
                    int srcRow = sy * width;

                    for (int kx = -radius; kx <= radius; kx++)
                    {
                        int sx = Clamp(x + kx, 0, right);
                        SKColor c = srcPixels[srcRow + sx];

                        rValues[count] = c.Red;
                        gValues[count] = c.Green;
                        bValues[count] = c.Blue;
                        aValues[count] = c.Alpha;
                        count++;
                    }
                }

                Array.Sort(rValues, 0, count);
                Array.Sort(gValues, 0, count);
                Array.Sort(bValues, 0, count);
                Array.Sort(aValues, 0, count);

                int mid = count >> 1;
                dstPixels[dstRow + x] = new SKColor(
                    rValues[mid],
                    gValues[mid],
                    bValues[mid],
                    aValues[mid]);
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

