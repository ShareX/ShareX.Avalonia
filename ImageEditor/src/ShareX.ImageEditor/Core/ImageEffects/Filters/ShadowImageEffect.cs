using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class ShadowImageEffect : FilterImageEffect
{
    public float Opacity { get; set; }
    public int Size { get; set; }
    public SKColor Color { get; set; }
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }
    public bool AutoResize { get; set; }

    public override string Name => "Shadow";

    public ShadowImageEffect(float opacity, int size, SKColor color, int offsetX, int offsetY, bool autoResize)
    {
        Opacity = opacity;
        Size = size;
        Color = color;
        OffsetX = offsetX;
        OffsetY = offsetY;
        AutoResize = autoResize;
    }

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        // Compute one-sided canvas expansion based on offset direction:
        // Positive offset → shadow extends right/down → expand right/bottom.
        // Negative offset → shadow extends left/up  → expand left/top.
        int blurPad = Size;  // room needed for blur to not get clipped

        int expandLeft = AutoResize ? Math.Max(0, -OffsetX) + blurPad : 0;
        int expandRight = AutoResize ? Math.Max(0, OffsetX) + blurPad : 0;
        int expandTop = AutoResize ? Math.Max(0, -OffsetY) + blurPad : 0;
        int expandBottom = AutoResize ? Math.Max(0, OffsetY) + blurPad : 0;

        int newWidth = source.Width + expandLeft + expandRight;
        int newHeight = source.Height + expandTop + expandBottom;

        SKBitmap result = new SKBitmap(newWidth, newHeight);
        using SKCanvas canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        // The original image is always placed at (expandLeft, expandTop).
        int imageX = expandLeft;
        int imageY = expandTop;
        int shadowX = imageX + OffsetX;
        int shadowY = imageY + OffsetY;

        // Shadow color: chosen Color with Opacity as alpha.
        SKColor shadowColor = new SKColor(Color.Red, Color.Green, Color.Blue, (byte)(255 * Opacity / 100f));

        using SKPaint shadowPaint = new SKPaint
        {
            ColorFilter = SKColorFilter.CreateBlendMode(shadowColor, SKBlendMode.SrcIn),
            ImageFilter = SKImageFilter.CreateBlur(Size / 2f, Size / 2f)
        };

        canvas.DrawBitmap(source, shadowX, shadowY, shadowPaint);
        canvas.DrawBitmap(source, imageX, imageY);

        return result;
    }
}

