using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Manipulations;

public class SkewImageEffect : ImageEffect
{
    public override string Name => "Skew";
    public override ImageEffectCategory Category => ImageEffectCategory.Manipulations;
    public override bool HasParameters => true;

    public float Horizontally { get; set; } = 0;
    public float Vertically { get; set; } = 0;

    /// <summary>
    /// When true, the output canvas expands to fit the full skewed image.
    /// When false, the output keeps the original dimensions.
    /// </summary>
    public bool AutoResize { get; set; } = true;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (Horizontally == 0 && Vertically == 0) return source.Copy();

        float w = source.Width;
        float h = source.Height;

        // Create skew matrix
        SKMatrix matrix = SKMatrix.CreateSkew(
            (float)Math.Tan(Horizontally * Math.PI / 180),
            (float)Math.Tan(Vertically * Math.PI / 180)
        );

        // Map corners to find actual bounds
        SKPoint[] corners = new SKPoint[]
        {
            new SKPoint(0, 0),
            new SKPoint(w, 0),
            new SKPoint(w, h),
            new SKPoint(0, h)
        };

        SKPoint[] mapped = matrix.MapPoints(corners);

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        foreach (var pt in mapped)
        {
            minX = Math.Min(minX, pt.X);
            minY = Math.Min(minY, pt.Y);
            maxX = Math.Max(maxX, pt.X);
            maxY = Math.Max(maxY, pt.Y);
        }

        int newWidth, newHeight;
        float offsetX, offsetY;

        if (AutoResize)
        {
            newWidth = Math.Max(1, (int)Math.Ceiling(maxX - minX));
            newHeight = Math.Max(1, (int)Math.Ceiling(maxY - minY));
            offsetX = -minX;
            offsetY = -minY;
        }
        else
        {
            newWidth = (int)w;
            newHeight = (int)h;
            offsetX = 0;
            offsetY = 0;
        }

        SKBitmap result = new SKBitmap(newWidth, newHeight, source.ColorType, source.AlphaType);
        using (SKCanvas canvas = new SKCanvas(result))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.Translate(offsetX, offsetY);
            canvas.SetMatrix(canvas.TotalMatrix.PreConcat(matrix));
            canvas.DrawBitmap(source, 0, 0);
        }

        return result;
    }
}

