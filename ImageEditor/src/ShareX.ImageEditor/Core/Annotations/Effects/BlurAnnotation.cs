using SkiaSharp;

namespace ShareX.ImageEditor.Core.Annotations;

/// <summary>
/// Blur annotation - applies blur to the region
/// </summary>
public partial class BlurAnnotation : BaseEffectAnnotation
{
    public BlurAnnotation()
    {
        ToolType = EditorTool.Blur;
        StrokeColor = "#00000000"; // Transparent border
        StrokeWidth = 0;
        Amount = 10; // Default blur radius
    }

    /// <summary>
    /// Update the internal blurred bitmap based on the source image
    /// </summary>
    /// <param name="source">The full source image (SKBitmap)</param>
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

        // Create result bitmap at FULL annotation size (including out-of-bounds areas)
        var result = new SKBitmap(fullW, fullH);
        result.Erase(SKColors.Transparent);

        // If no valid intersection, return empty bitmap
        if (validRect.Width <= 0 || validRect.Height <= 0)
        {
            EffectBitmap?.Dispose();
            EffectBitmap = result;
            return;
        }

        // Calculate blur parameters
        var blurSigma = Amount;
        var padding = (int)System.Math.Ceiling(blurSigma * 3);

        // Calculate padded region for blur context (around the valid region)
        var wantedPaddedRect = new SKRectI(
            validRect.Left - padding,
            validRect.Top - padding,
            validRect.Right + padding,
            validRect.Bottom + padding
        );

        // Clamp padded region to source bounds
        var actualPaddedRect = wantedPaddedRect;
        actualPaddedRect.Intersect(new SKRectI(0, 0, source.Width, source.Height));

        if (actualPaddedRect.Width <= 0 || actualPaddedRect.Height <= 0)
        {
            EffectBitmap?.Dispose();
            EffectBitmap = result;
            return;
        }

        // Extract padded region from source
        using var paddedCrop = new SKBitmap(actualPaddedRect.Width, actualPaddedRect.Height);
        if (!source.ExtractSubset(paddedCrop, actualPaddedRect))
        {
            EffectBitmap?.Dispose();
            EffectBitmap = result;
            return;
        }

        // Step 1: Create extended surface with clamped edges
        using var extendedSurface = SKSurface.Create(new SKImageInfo(wantedPaddedRect.Width, wantedPaddedRect.Height));
        var extendedCanvas = extendedSurface.Canvas;

        // Use clamp shader to extend edge pixels where padding was clipped
        using var clampShader = paddedCrop.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);
        using var fillPaint = new SKPaint { Shader = clampShader };

        float cropOffsetX = actualPaddedRect.Left - wantedPaddedRect.Left;
        float cropOffsetY = actualPaddedRect.Top - wantedPaddedRect.Top;

        extendedCanvas.Save();
        extendedCanvas.Translate(cropOffsetX, cropOffsetY);
        extendedCanvas.DrawRect(new SKRect(-cropOffsetX, -cropOffsetY, wantedPaddedRect.Width - cropOffsetX, wantedPaddedRect.Height - cropOffsetY), fillPaint);
        extendedCanvas.Restore();

        // Step 2: Apply blur to the extended surface
        using var extendedImage = extendedSurface.Snapshot();
        using var extendedBitmap = SKBitmap.FromImage(extendedImage);

        using var blurSurface = SKSurface.Create(new SKImageInfo(wantedPaddedRect.Width, wantedPaddedRect.Height));
        var blurCanvas = blurSurface.Canvas;

        using var blurPaint = new SKPaint();
        blurPaint.ImageFilter = SKImageFilter.CreateBlur(blurSigma, blurSigma);
        blurCanvas.DrawBitmap(extendedBitmap, 0, 0, blurPaint);

        // Step 3: Extract the valid region from blurred result
        using var blurredImage = blurSurface.Snapshot();
        using var blurredBitmap = SKBitmap.FromImage(blurredImage);

        // The valid region within the blurred bitmap
        var validInBlurred = new SKRectI(
            padding,
            padding,
            padding + validRect.Width,
            padding + validRect.Height
        );
        validInBlurred.Intersect(new SKRectI(0, 0, blurredBitmap.Width, blurredBitmap.Height));

        if (validInBlurred.Width <= 0 || validInBlurred.Height <= 0)
        {
            EffectBitmap?.Dispose();
            EffectBitmap = result;
            return;
        }

        using var blurredRegion = new SKBitmap(validInBlurred.Width, validInBlurred.Height);
        if (!blurredBitmap.ExtractSubset(blurredRegion, validInBlurred))
        {
            EffectBitmap?.Dispose();
            EffectBitmap = result;
            return;
        }

        // Step 4: Draw blurred region into result at correct offset
        // The offset is where the valid region starts within the full annotation bounds
        int drawX = validRect.Left - annotationRect.Left;
        int drawY = validRect.Top - annotationRect.Top;

        using (var resultCanvas = new SKCanvas(result))
        {
            resultCanvas.DrawBitmap(blurredRegion, drawX, drawY);
        }

        EffectBitmap?.Dispose();
        EffectBitmap = result;
    }
}
