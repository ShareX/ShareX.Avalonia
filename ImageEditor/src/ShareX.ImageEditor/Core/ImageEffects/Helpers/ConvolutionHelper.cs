using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Helpers;

internal static class ConvolutionHelper
{
    public static SKBitmap Apply3x3(
        SKBitmap source,
        float[] kernel,
        float gain = 1f,
        float bias = 0f,
        bool convolveAlpha = false,
        SKShaderTileMode tileMode = SKShaderTileMode.Clamp)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (kernel is null) throw new ArgumentNullException(nameof(kernel));
        if (kernel.Length != 9) throw new ArgumentException("Kernel must contain 9 values for a 3x3 convolution.", nameof(kernel));

        using SKImageFilter filter = SKImageFilter.CreateMatrixConvolution(
            new SKSizeI(3, 3),
            kernel,
            gain,
            bias,
            new SKPointI(1, 1),
            tileMode,
            convolveAlpha);

        using SKPaint paint = new SKPaint { ImageFilter = filter };

        SKBitmap result = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);
        using SKCanvas canvas = new SKCanvas(result);
        canvas.DrawBitmap(source, 0, 0, paint);

        return result;
    }
}
