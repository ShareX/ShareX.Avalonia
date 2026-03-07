using SkiaSharp;

namespace ShareX.ImageEditor.Core.Annotations;

/// <summary>
/// Pixelate annotation - applies pixelation to the region
/// </summary>
public partial class PixelateAnnotation : BaseEffectAnnotation
{
    public PixelateAnnotation()
    {
        ToolType = EditorTool.Pixelate;
        StrokeColor = "#00000000";
        StrokeWidth = 0;
        Amount = 10; // Default pixel size
    }




    public override void UpdateEffect(SKBitmap source)
    {
        if (source == null) return;

        var rect = GetBounds();
        int fullW = (int)rect.Width;
        int fullH = (int)rect.Height;
        if (fullW <= 0 || fullH <= 0) return;

        // Convert annotation bounds to integer rect
        var annotationRect = new SKRectI((int)rect.Left, (int)rect.Top, (int)rect.Right, (int)rect.Bottom);

        // Find intersection with source image bounds
        var validRect = annotationRect;
        validRect.Intersect(new SKRectI(0, 0, source.Width, source.Height));

        // Create result bitmap at FULL annotation size
        var result = new SKBitmap(fullW, fullH);
        result.Erase(SKColors.Transparent);

        if (validRect.Width <= 0 || validRect.Height <= 0)
        {
            EffectBitmap?.Dispose();
            EffectBitmap = result;
            return;
        }

        using var crop = new SKBitmap(validRect.Width, validRect.Height);
        if (!source.ExtractSubset(crop, validRect))
        {
            EffectBitmap?.Dispose();
            EffectBitmap = result;
            return;
        }

        // Pixelate logic: Downscale then upscale
        var pixelSize = (int)Math.Max(1, Amount);
        int w = Math.Max(1, crop.Width / pixelSize);
        int h = Math.Max(1, crop.Height / pixelSize);

        var info = new SKImageInfo(w, h);
        using var small = crop.Resize(info, SKFilterQuality.Low);

        info = new SKImageInfo(crop.Width, crop.Height);
        using var pixelated = small.Resize(info, SKFilterQuality.None); // Nearest neighbor upscale

        // Draw pixelated region into result at correct offset
        int drawX = validRect.Left - annotationRect.Left;
        int drawY = validRect.Top - annotationRect.Top;

        using (var resultCanvas = new SKCanvas(result))
        {
            resultCanvas.DrawBitmap(pixelated, drawX, drawY);
        }

        EffectBitmap?.Dispose();
        EffectBitmap = result;
    }
}
