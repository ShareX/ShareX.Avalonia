using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class BorderImageEffect : FilterImageEffect
{
    public ImageHelpers.BorderType Type { get; set; }
    public int Size { get; set; }
    public ImageHelpers.DashStyle DashStyle { get; set; }
    public SKColor Color { get; set; }

    public override string Name => "Border";
    public override ImageEffectCategory Category => ImageEffectCategory.Drawings;

    public BorderImageEffect(ImageHelpers.BorderType type, int size, ImageHelpers.DashStyle dashStyle, SKColor color)
    {
        Type = type;
        Size = size;
        DashStyle = dashStyle;
        Color = color;
    }

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (Size <= 0) return source.Copy();

        int newWidth = Type == ImageHelpers.BorderType.Outside ? source.Width + Size * 2 : source.Width;
        int newHeight = Type == ImageHelpers.BorderType.Outside ? source.Height + Size * 2 : source.Height;

        SKBitmap result = new SKBitmap(newWidth, newHeight);
        using SKCanvas canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        int offsetX = Type == ImageHelpers.BorderType.Outside ? Size : 0;
        int offsetY = Type == ImageHelpers.BorderType.Outside ? Size : 0;

        // Draw image
        canvas.DrawBitmap(source, offsetX, offsetY);

        // Draw border
        using SKPaint paint = new SKPaint
        {
            Color = Color,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Size,
            IsAntialias = true
        };

        // Set dash effect
        float[] intervals = DashStyle switch
        {
            ImageHelpers.DashStyle.Dash => new float[] { Size * 3, Size },
            ImageHelpers.DashStyle.Dot => new float[] { Size, Size },
            ImageHelpers.DashStyle.DashDot => new float[] { Size * 3, Size, Size, Size },
            _ => null!
        };
        if (intervals != null)
        {
            paint.PathEffect = SKPathEffect.CreateDash(intervals, 0);
        }

        float halfStroke = Size / 2f;
        SKRect borderRect = Type == ImageHelpers.BorderType.Outside
            ? new SKRect(halfStroke, halfStroke, newWidth - halfStroke, newHeight - halfStroke)
            : new SKRect(halfStroke, halfStroke, source.Width - halfStroke, source.Height - halfStroke);

        canvas.DrawRect(borderRect, paint);

        return result;
    }
}

