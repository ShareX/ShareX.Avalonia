using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Adjustments;

public abstract class AdjustmentImageEffect : ShareX.ImageEditor.Core.ImageEffects.ImageEffect
{
    public override ImageEffectCategory Category => ImageEffectCategory.Adjustments;
    public override bool HasParameters => true;

    protected static SKBitmap ApplyColorMatrix(SKBitmap source, float[] matrix)
    {
        using var filter = SKColorFilter.CreateColorMatrix(matrix);
        return ApplyColorFilter(source, filter);
    }

    protected static SKBitmap ApplyColorFilter(SKBitmap source, SKColorFilter filter)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        SKBitmap result = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);
        using (SKCanvas canvas = new SKCanvas(result))
        {
            canvas.Clear(SKColors.Transparent);
            using (SKPaint paint = new SKPaint())
            {
                paint.ColorFilter = filter;
                canvas.DrawBitmap(source, 0, 0, paint);
            }
        }
        return result;
    }

    protected unsafe static SKBitmap ApplyPixelOperation(SKBitmap source, Func<SKColor, SKColor> operation)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        SKBitmap result = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);

        if (source.ColorType == SKColorType.Bgra8888)
        {
            int count = source.Width * source.Height;
            SKColor* srcPtr = (SKColor*)source.GetPixels();
            SKColor* dstPtr = (SKColor*)result.GetPixels();

            for (int i = 0; i < count; i++)
            {
                *dstPtr++ = operation(*srcPtr++);
            }
        }
        else
        {
            var srcPixels = source.Pixels;
            var dstPixels = new SKColor[srcPixels.Length];

            for (int i = 0; i < srcPixels.Length; i++)
            {
                dstPixels[i] = operation(srcPixels[i]);
            }

            result.Pixels = dstPixels;
        }

        return result;
    }
}

