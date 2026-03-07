using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Manipulations;

public class PerspectiveWarpImageEffect : ImageEffect
{
    public override string Name => "Perspective warp";
    public override ImageEffectCategory Category => ImageEffectCategory.Manipulations;
    public override bool HasParameters => true;

    public float TopLeftX { get; set; }
    public float TopLeftY { get; set; }

    public float TopRightX { get; set; }
    public float TopRightY { get; set; }

    public float BottomRightX { get; set; }
    public float BottomRightY { get; set; }

    public float BottomLeftX { get; set; }
    public float BottomLeftY { get; set; }

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        if (TopLeftX == 0 && TopLeftY == 0 &&
            TopRightX == 0 && TopRightY == 0 &&
            BottomRightX == 0 && BottomRightY == 0 &&
            BottomLeftX == 0 && BottomLeftY == 0)
        {
            return source.Copy();
        }

        int width = source.Width;
        int height = source.Height;
        int right = width - 1;
        int bottom = height - 1;

        SKPoint[] srcQuad =
        {
            new(0, 0),
            new(right, 0),
            new(right, bottom),
            new(0, bottom)
        };

        SKPoint[] dstQuad =
        {
            new(TopLeftX, TopLeftY),
            new(right + TopRightX, TopRightY),
            new(right + BottomRightX, bottom + BottomRightY),
            new(BottomLeftX, bottom + BottomLeftY)
        };

        if (!TryComputeHomography(dstQuad, srcQuad, out double[] inverse))
        {
            return source.Copy();
        }

        SKColor[] srcPixels = source.Pixels;
        SKColor[] dstPixels = new SKColor[srcPixels.Length];

        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                double w = inverse[6] * x + inverse[7] * y + inverse[8];
                if (Math.Abs(w) < 1e-8)
                {
                    dstPixels[row + x] = SKColors.Transparent;
                    continue;
                }

                float sx = (float)((inverse[0] * x + inverse[1] * y + inverse[2]) / w);
                float sy = (float)((inverse[3] * x + inverse[4] * y + inverse[5]) / w);

                if (sx < 0 || sy < 0 || sx > right || sy > bottom)
                {
                    dstPixels[row + x] = SKColors.Transparent;
                    continue;
                }

                int srcX = (int)Math.Round(sx);
                int srcY = (int)Math.Round(sy);
                srcX = Clamp(srcX, 0, right);
                srcY = Clamp(srcY, 0, bottom);

                dstPixels[row + x] = srcPixels[srcY * width + srcX];
            }
        }

        return new SKBitmap(width, height, source.ColorType, source.AlphaType) { Pixels = dstPixels };
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static bool TryComputeHomography(SKPoint[] src, SKPoint[] dst, out double[] h)
    {
        h = Array.Empty<double>();
        if (src.Length != 4 || dst.Length != 4) return false;

        double[,] a = new double[8, 8];
        double[] b = new double[8];

        for (int i = 0; i < 4; i++)
        {
            double x = src[i].X;
            double y = src[i].Y;
            double u = dst[i].X;
            double v = dst[i].Y;

            int r0 = i * 2;
            int r1 = r0 + 1;

            a[r0, 0] = x;
            a[r0, 1] = y;
            a[r0, 2] = 1;
            a[r0, 3] = 0;
            a[r0, 4] = 0;
            a[r0, 5] = 0;
            a[r0, 6] = -u * x;
            a[r0, 7] = -u * y;
            b[r0] = u;

            a[r1, 0] = 0;
            a[r1, 1] = 0;
            a[r1, 2] = 0;
            a[r1, 3] = x;
            a[r1, 4] = y;
            a[r1, 5] = 1;
            a[r1, 6] = -v * x;
            a[r1, 7] = -v * y;
            b[r1] = v;
        }

        if (!TrySolveLinearSystem(a, b, out double[] solution))
        {
            return false;
        }

        h = new double[9];
        Array.Copy(solution, h, 8);
        h[8] = 1.0;
        return true;
    }

    private static bool TrySolveLinearSystem(double[,] a, double[] b, out double[] x)
    {
        int n = 8;
        x = new double[n];
        double[,] aug = new double[n, n + 1];

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                aug[i, j] = a[i, j];
            }
            aug[i, n] = b[i];
        }

        for (int col = 0; col < n; col++)
        {
            int pivot = col;
            double max = Math.Abs(aug[pivot, col]);
            for (int row = col + 1; row < n; row++)
            {
                double value = Math.Abs(aug[row, col]);
                if (value > max)
                {
                    max = value;
                    pivot = row;
                }
            }

            if (max < 1e-12) return false;

            if (pivot != col)
            {
                for (int j = col; j <= n; j++)
                {
                    (aug[col, j], aug[pivot, j]) = (aug[pivot, j], aug[col, j]);
                }
            }

            double div = aug[col, col];
            for (int j = col; j <= n; j++)
            {
                aug[col, j] /= div;
            }

            for (int row = 0; row < n; row++)
            {
                if (row == col) continue;
                double factor = aug[row, col];
                if (Math.Abs(factor) < 1e-12) continue;
                for (int j = col; j <= n; j++)
                {
                    aug[row, j] -= factor * aug[col, j];
                }
            }
        }

        for (int i = 0; i < n; i++)
        {
            x[i] = aug[i, n];
        }

        return true;
    }
}
