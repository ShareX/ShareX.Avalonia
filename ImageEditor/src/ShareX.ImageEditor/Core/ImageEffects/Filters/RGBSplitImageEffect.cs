using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class RGBSplitImageEffect : FilterImageEffect
{
    public override string Name => "RGB split";
    public override string IconKey => "IconAdjust";
    public override bool HasParameters => true;

    public int OffsetRedX { get; set; } = -5;
    public int OffsetRedY { get; set; }

    public int OffsetGreenX { get; set; }
    public int OffsetGreenY { get; set; }

    public int OffsetBlueX { get; set; } = 5;
    public int OffsetBlueY { get; set; }

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        int width = source.Width;
        int height = source.Height;
        int right = width - 1;
        int bottom = height - 1;

        SKColor[] srcPixels = source.Pixels;
        SKColor[] dstPixels = new SKColor[srcPixels.Length];

        // Precompute clamped source X indices for each destination X per channel.
        int[] xRedMap = new int[width];
        int[] xGreenMap = new int[width];
        int[] xBlueMap = new int[width];

        for (int x = 0; x < width; x++)
        {
            xRedMap[x] = Clamp(x - OffsetRedX, 0, right);
            xGreenMap[x] = Clamp(x - OffsetGreenX, 0, right);
            xBlueMap[x] = Clamp(x - OffsetBlueX, 0, right);
        }

        // Precompute source row starts for each destination Y per channel.
        int[] rowRedMap = new int[height];
        int[] rowGreenMap = new int[height];
        int[] rowBlueMap = new int[height];

        for (int y = 0; y < height; y++)
        {
            rowRedMap[y] = Clamp(y - OffsetRedY, 0, bottom) * width;
            rowGreenMap[y] = Clamp(y - OffsetGreenY, 0, bottom) * width;
            rowBlueMap[y] = Clamp(y - OffsetBlueY, 0, bottom) * width;
        }

        for (int y = 0; y < height; y++)
        {
            int dstRow = y * width;
            int redRow = rowRedMap[y];
            int greenRow = rowGreenMap[y];
            int blueRow = rowBlueMap[y];

            for (int x = 0; x < width; x++)
            {
                SKColor colorR = srcPixels[redRow + xRedMap[x]];
                SKColor colorG = srcPixels[greenRow + xGreenMap[x]];
                SKColor colorB = srcPixels[blueRow + xBlueMap[x]];

                byte red = (byte)(colorR.Red * colorR.Alpha / 255);
                byte green = (byte)(colorG.Green * colorG.Alpha / 255);
                byte blue = (byte)(colorB.Blue * colorB.Alpha / 255);
                byte alpha = (byte)((colorR.Alpha + colorG.Alpha + colorB.Alpha) / 3);

                dstPixels[dstRow + x] = new SKColor(red, green, blue, alpha);
            }
        }

        SKBitmap result = new SKBitmap(width, height, source.ColorType, source.AlphaType)
        {
            Pixels = dstPixels
        };

        return result;
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
