using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class GlowImageEffect : FilterImageEffect
{
    public int Size { get; set; }
    public float Strength { get; set; }
    public SKColor Color { get; set; }
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }
    public bool AutoResize { get; set; }

    public override string Name => "Glow";

    public GlowImageEffect(int size, float strength, SKColor color, int offsetX, int offsetY, bool autoResize)
    {
        Size = size;
        Strength = strength;
        Color = color;
        OffsetX = offsetX;
        OffsetY = offsetY;
        AutoResize = autoResize;
    }

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        // Compute one-sided canvas expansion based on offset direction:
        // Size is used as padding for the blur.
        int pad = AutoResize ? Size : 0;

        int expandLeft = AutoResize ? Math.Max(0, -OffsetX) + pad : 0;
        int expandRight = AutoResize ? Math.Max(0, OffsetX) + pad : 0;
        int expandTop = AutoResize ? Math.Max(0, -OffsetY) + pad : 0;
        int expandBottom = AutoResize ? Math.Max(0, OffsetY) + pad : 0;

        int newWidth = source.Width + expandLeft + expandRight;
        int newHeight = source.Height + expandTop + expandBottom;

        SKBitmap result = new SKBitmap(newWidth, newHeight);
        using SKCanvas canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        int imageX = expandLeft;
        int imageY = expandTop;
        int glowX = imageX + OffsetX;
        int glowY = imageY + OffsetY;

        SKColor glowColor = Color.WithAlpha((byte)(255 * Strength / 100f));

        using SKPaint glowPaint = new SKPaint
        {
            ColorFilter = SKColorFilter.CreateBlendMode(glowColor, SKBlendMode.SrcIn),
            ImageFilter = SKImageFilter.CreateBlur(Size, Size)
        };

        // Draw glow
        canvas.DrawBitmap(source, glowX, glowY, glowPaint);
        // Draw original
        canvas.DrawBitmap(source, imageX, imageY);

        return result;
    }
}

