using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class SharpenImageEffect : FilterImageEffect
{
    public override string Name => "Sharpen";
    public override string IconKey => "IconMagic";
    public override bool HasParameters => true;
    public int Strength { get; set; } = 50;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (Strength <= 0) return source.Copy();

        float strength = Strength / 100f;
        float s = Math.Clamp(strength, 0f, 1f);

        // Create sharpening convolution kernel
        float center = 1 + 4 * s;
        float edge = -s;
        float[] kernel = new float[]
        {
            0, edge, 0,
            edge, center, edge,
            0, edge, 0
        };

        // Apply convolution
        using SKImageFilter sharpenFilter = SKImageFilter.CreateMatrixConvolution(
            new SKSizeI(3, 3),
            kernel,
            1f, // gain
            0f, // bias
            new SKPointI(1, 1), // kernel offset
            SKShaderTileMode.Clamp,
            true // convolve alpha
        );

        using SKPaint paint = new SKPaint { ImageFilter = sharpenFilter };

        SKBitmap sharpened = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);
        using SKCanvas sharpCanvas = new SKCanvas(sharpened);
        sharpCanvas.DrawBitmap(source, 0, 0, paint);

        return sharpened;
    }
}

