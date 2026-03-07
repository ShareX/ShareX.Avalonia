using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class BlurImageEffect : FilterImageEffect
{
    public override string Name => "Blur";
    public override string IconKey => "IconCloud";
    public override bool HasParameters => true;
    public int Radius { get; set; } = 5;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (Radius <= 0) return source.Copy();

        // Expand canvas to prevent edge transparency
        int padding = Radius * 2;
        int expandedWidth = source.Width + padding * 2;
        int expandedHeight = source.Height + padding * 2;

        // Create expanded bitmap with edge pixels extended
        SKBitmap expanded = new SKBitmap(expandedWidth, expandedHeight, source.ColorType, source.AlphaType);
        using (SKCanvas expandCanvas = new SKCanvas(expanded))
        {
            // Fill with edge-extended version using clamp shader
            using var shader = SKShader.CreateBitmap(source, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp,
                SKMatrix.CreateTranslation(padding, padding));
            using var paint = new SKPaint { Shader = shader };
            expandCanvas.DrawRect(new SKRect(0, 0, expandedWidth, expandedHeight), paint);
        }

        // Apply blur to expanded bitmap
        SKBitmap blurred = new SKBitmap(expandedWidth, expandedHeight, source.ColorType, source.AlphaType);
        using (SKCanvas blurCanvas = new SKCanvas(blurred))
        {
            using SKPaint blurPaint = new SKPaint
            {
                ImageFilter = SKImageFilter.CreateBlur(Radius, Radius)
            };
            blurCanvas.DrawBitmap(expanded, 0, 0, blurPaint);
        }
        expanded.Dispose();

        // Crop back to original size
        SKBitmap result = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);
        using (SKCanvas resultCanvas = new SKCanvas(result))
        {
            resultCanvas.DrawBitmap(blurred, new SKRect(padding, padding, padding + source.Width, padding + source.Height),
                new SKRect(0, 0, source.Width, source.Height));
        }
        blurred.Dispose();

        return result;
    }
}

