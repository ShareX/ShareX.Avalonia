using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public enum DitheringMethod
{
    FloydSteinberg,
    Bayer4x4
}

public enum DitheringPalette
{
    OneBitBW,
    WebSafe216,
    RGB332,
    Grayscale4
}

public class DitheringImageEffect : FilterImageEffect
{
    public override string Name => "Dithering";
    public override string IconKey => "IconTableCells";
    public override bool HasParameters => true;

    public DitheringMethod Method { get; set; } = DitheringMethod.FloydSteinberg;
    public DitheringPalette Palette { get; set; } = DitheringPalette.OneBitBW;
    public bool Serpentine { get; set; } = true;
    public float Strength { get; set; } = 100f; // 0..100

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        int width = source.Width;
        int height = source.Height;
        if (width <= 0 || height <= 0)
        {
            return source.Copy();
        }

        float strength = Math.Clamp(Strength, 0f, 100f) / 100f;
        if (strength <= 0f)
        {
            return source.Copy();
        }

        SKColor[] srcPixels = source.Pixels;
        SKColor[] dithered = Method == DitheringMethod.Bayer4x4
            ? ApplyBayer(srcPixels, width, height, Palette)
            : ApplyFloydSteinberg(srcPixels, width, height, Palette, Serpentine);

        SKColor[] outPixels = new SKColor[srcPixels.Length];
        for (int i = 0; i < srcPixels.Length; i++)
        {
            SKColor src = srcPixels[i];
            SKColor dst = dithered[i];
            outPixels[i] = new SKColor(
                ProceduralEffectHelper.ClampToByte(ProceduralEffectHelper.Lerp(src.Red, dst.Red, strength)),
                ProceduralEffectHelper.ClampToByte(ProceduralEffectHelper.Lerp(src.Green, dst.Green, strength)),
                ProceduralEffectHelper.ClampToByte(ProceduralEffectHelper.Lerp(src.Blue, dst.Blue, strength)),
                src.Alpha);
        }

        return new SKBitmap(width, height, source.ColorType, source.AlphaType)
        {
            Pixels = outPixels
        };
    }

    private static SKColor[] ApplyFloydSteinberg(SKColor[] srcPixels, int width, int height, DitheringPalette palette, bool serpentine)
    {
        int count = srcPixels.Length;
        float[] r = new float[count];
        float[] g = new float[count];
        float[] b = new float[count];
        SKColor[] output = new SKColor[count];

        for (int i = 0; i < count; i++)
        {
            r[i] = srcPixels[i].Red;
            g[i] = srcPixels[i].Green;
            b[i] = srcPixels[i].Blue;
        }

        for (int y = 0; y < height; y++)
        {
            bool reverse = serpentine && ((y & 1) == 1);
            int xStart = reverse ? width - 1 : 0;
            int xEnd = reverse ? -1 : width;
            int xStep = reverse ? -1 : 1;

            for (int x = xStart; x != xEnd; x += xStep)
            {
                int index = (y * width) + x;
                float cr = r[index];
                float cg = g[index];
                float cb = b[index];

                Quantize(cr, cg, cb, palette, out byte qr, out byte qg, out byte qb);
                output[index] = new SKColor(qr, qg, qb, srcPixels[index].Alpha);

                float er = cr - qr;
                float eg = cg - qg;
                float eb = cb - qb;

                int dir = reverse ? -1 : 1;
                DistributeError(r, g, b, width, height, x + dir, y, er, eg, eb, 7f / 16f);
                DistributeError(r, g, b, width, height, x - dir, y + 1, er, eg, eb, 3f / 16f);
                DistributeError(r, g, b, width, height, x, y + 1, er, eg, eb, 5f / 16f);
                DistributeError(r, g, b, width, height, x + dir, y + 1, er, eg, eb, 1f / 16f);
            }
        }

        return output;
    }

    private static SKColor[] ApplyBayer(SKColor[] srcPixels, int width, int height, DitheringPalette palette)
    {
        int[,] bayer4 =
        {
            { 0, 8, 2, 10 },
            { 12, 4, 14, 6 },
            { 3, 11, 1, 9 },
            { 15, 7, 13, 5 }
        };

        SKColor[] output = new SKColor[srcPixels.Length];

        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                int index = row + x;
                SKColor src = srcPixels[index];

                float threshold = (bayer4[y & 3, x & 3] / 16f) - 0.5f;
                float bias = threshold * 36f;

                float r = src.Red + bias;
                float g = src.Green + bias;
                float b = src.Blue + bias;

                Quantize(r, g, b, palette, out byte qr, out byte qg, out byte qb);
                output[index] = new SKColor(qr, qg, qb, src.Alpha);
            }
        }

        return output;
    }

    private static void DistributeError(
        float[] r,
        float[] g,
        float[] b,
        int width,
        int height,
        int x,
        int y,
        float er,
        float eg,
        float eb,
        float factor)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return;
        }

        int index = (y * width) + x;
        r[index] = Math.Clamp(r[index] + (er * factor), 0f, 255f);
        g[index] = Math.Clamp(g[index] + (eg * factor), 0f, 255f);
        b[index] = Math.Clamp(b[index] + (eb * factor), 0f, 255f);
    }

    private static void Quantize(float r, float g, float b, DitheringPalette palette, out byte qr, out byte qg, out byte qb)
    {
        switch (palette)
        {
            case DitheringPalette.OneBitBW:
                {
                    float lum = (0.2126f * r) + (0.7152f * g) + (0.0722f * b);
                    byte bw = lum >= 128f ? (byte)255 : (byte)0;
                    qr = bw;
                    qg = bw;
                    qb = bw;
                    break;
                }
            case DitheringPalette.WebSafe216:
                qr = QuantizeChannel(r, 6);
                qg = QuantizeChannel(g, 6);
                qb = QuantizeChannel(b, 6);
                break;
            case DitheringPalette.RGB332:
                qr = QuantizeChannel(r, 8);
                qg = QuantizeChannel(g, 8);
                qb = QuantizeChannel(b, 4);
                break;
            default:
                {
                    float lum = (0.2126f * r) + (0.7152f * g) + (0.0722f * b);
                    byte gray = QuantizeChannel(lum, 4);
                    qr = gray;
                    qg = gray;
                    qb = gray;
                    break;
                }
        }
    }

    private static byte QuantizeChannel(float value, int levels)
    {
        float clamped = Math.Clamp(value, 0f, 255f) / 255f;
        int stepCount = levels - 1;
        int quantized = (int)MathF.Round(clamped * stepCount);
        float output = quantized / (float)stepCount;
        return ProceduralEffectHelper.ClampToByte(output * 255f);
    }
}
