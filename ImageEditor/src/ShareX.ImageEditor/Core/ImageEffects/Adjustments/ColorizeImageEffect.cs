using SkiaSharp;


namespace ShareX.ImageEditor.Core.ImageEffects.Adjustments;

public class ColorizeImageEffect : AdjustmentImageEffect
{
    public override string Name => "Colorize";
    public override string IconKey => "IconTint";
    public SKColor Color { get; set; } = SKColors.Red; // Default
    public float Strength { get; set; } = 50f;

    public override SKBitmap Apply(SKBitmap source)
    {
        float strength = Strength;
        if (strength <= 0) return source.Copy();
        if (strength > 100) strength = 100;

        using var paint = new SKPaint();

        var grayscaleMatrix = new float[] {
            0.2126f, 0.7152f, 0.0722f, 0, 0,
            0.2126f, 0.7152f, 0.0722f, 0, 0,
            0.2126f, 0.7152f, 0.0722f, 0, 0,
            0,       0,       0,       1, 0
        };
        using var grayscale = SKColorFilter.CreateColorMatrix(grayscaleMatrix);
        using var tint = SKColorFilter.CreateBlendMode(Color, SKBlendMode.Modulate);
        using var composed = SKColorFilter.CreateCompose(tint, grayscale);

        paint.ColorFilter = composed;

        SKBitmap result = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);
        using (SKCanvas canvas = new SKCanvas(result))
        {
            canvas.Clear(SKColors.Transparent);

            if (strength >= 100)
            {
                canvas.DrawBitmap(source, 0, 0, paint);
            }
            else
            {
                canvas.DrawBitmap(source, 0, 0);
                paint.Color = new SKColor(255, 255, 255, (byte)(255 * (strength / 100f)));
                canvas.DrawBitmap(source, 0, 0, paint);
            }
        }
        return result;
    }
}

