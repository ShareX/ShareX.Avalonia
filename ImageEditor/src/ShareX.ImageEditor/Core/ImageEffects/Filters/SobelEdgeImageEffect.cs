using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class SobelEdgeImageEffect : FilterImageEffect
{
    public override string Name => "Sobel edge";
    public override string IconKey => "IconHighlighter";
    public override bool HasParameters => true;

    public float Strength { get; set; } = 1.2f;
    public int Threshold { get; set; } = 20;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        float strength = Math.Clamp(Strength, 0.1f, 5f);
        int threshold = Math.Clamp(Threshold, 0, 255);

        int width = source.Width;
        int height = source.Height;
        int right = width - 1;
        int bottom = height - 1;

        SKColor[] srcPixels = source.Pixels;
        SKColor[] dstPixels = new SKColor[srcPixels.Length];
        byte[] luma = new byte[srcPixels.Length];

        for (int i = 0; i < srcPixels.Length; i++)
        {
            SKColor c = srcPixels[i];
            luma[i] = (byte)(((c.Red * 77) + (c.Green * 150) + (c.Blue * 29)) >> 8);
        }

        for (int y = 0; y < height; y++)
        {
            int dstRow = y * width;
            int y0 = Clamp(y - 1, 0, bottom);
            int y1 = y;
            int y2 = Clamp(y + 1, 0, bottom);

            int row0 = y0 * width;
            int row1 = y1 * width;
            int row2 = y2 * width;

            for (int x = 0; x < width; x++)
            {
                int x0 = Clamp(x - 1, 0, right);
                int x1 = x;
                int x2 = Clamp(x + 1, 0, right);

                int p00 = luma[row0 + x0];
                int p01 = luma[row0 + x1];
                int p02 = luma[row0 + x2];
                int p10 = luma[row1 + x0];
                int p12 = luma[row1 + x2];
                int p20 = luma[row2 + x0];
                int p21 = luma[row2 + x1];
                int p22 = luma[row2 + x2];

                int gx = (-p00 + p02) + (-2 * p10 + 2 * p12) + (-p20 + p22);
                int gy = (-p00 - 2 * p01 - p02) + (p20 + 2 * p21 + p22);

                float magnitude = MathF.Sqrt((gx * gx) + (gy * gy)) * strength;
                byte edge = (magnitude >= threshold) ? ClampToByte(magnitude) : (byte)0;

                byte alpha = srcPixels[dstRow + x].Alpha;
                dstPixels[dstRow + x] = new SKColor(edge, edge, edge, alpha);
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

    private static byte ClampToByte(float value)
    {
        if (value <= 0f) return 0;
        if (value >= 255f) return 255;
        return (byte)MathF.Round(value);
    }
}

