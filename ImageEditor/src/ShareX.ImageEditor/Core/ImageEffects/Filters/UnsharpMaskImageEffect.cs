using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class UnsharpMaskImageEffect : FilterImageEffect
{
    public override string Name => "Unsharp mask";
    public override string IconKey => "IconMagic";
    public override bool HasParameters => true;

    public float Radius { get; set; } = 5f;
    public float Amount { get; set; } = 150f;
    public int Threshold { get; set; }

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        float radius = Math.Clamp(Radius, 1f, 100f);
        float amount = Math.Clamp(Amount, 0f, 500f) / 100f;
        int threshold = Math.Clamp(Threshold, 0, 255);

        if (amount <= 0f)
        {
            return source.Copy();
        }

        SKBitmap blurred = ApplyBlur(source, radius / 3f);

        SKColor[] srcPixels = source.Pixels;
        SKColor[] blurPixels = blurred.Pixels;
        SKColor[] dstPixels = new SKColor[srcPixels.Length];

        for (int i = 0; i < srcPixels.Length; i++)
        {
            SKColor o = srcPixels[i];
            SKColor b = blurPixels[i];

            byte r = SharpenChannel(o.Red, b.Red, amount, threshold);
            byte g = SharpenChannel(o.Green, b.Green, amount, threshold);
            byte bch = SharpenChannel(o.Blue, b.Blue, amount, threshold);

            dstPixels[i] = new SKColor(r, g, bch, o.Alpha);
        }

        blurred.Dispose();

        return new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType)
        {
            Pixels = dstPixels
        };
    }

    private static SKBitmap ApplyBlur(SKBitmap source, float sigma)
    {
        using SKImageFilter filter = SKImageFilter.CreateBlur(sigma, sigma);
        using SKPaint paint = new SKPaint { ImageFilter = filter };

        SKBitmap result = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);
        using SKCanvas canvas = new SKCanvas(result);
        canvas.DrawBitmap(source, 0, 0, paint);
        return result;
    }

    private static byte SharpenChannel(byte original, byte blurred, float amount, int threshold)
    {
        int diff = original - blurred;
        if (Math.Abs(diff) < threshold)
        {
            return original;
        }

        float value = original + (diff * amount);
        if (value <= 0f) return 0;
        if (value >= 255f) return 255;
        return (byte)MathF.Round(value);
    }
}

