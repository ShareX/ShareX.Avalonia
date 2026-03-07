using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Manipulations;

public class Rotate3DImageEffect : ImageEffect
{
    public override string Name => "Rotate 3D";
    public override ImageEffectCategory Category => ImageEffectCategory.Manipulations;
    public override bool HasParameters => true;

    /// <summary>
    /// Rotation around the X-axis in degrees (-180 to 180).
    /// Positive values tilt the top of the image away from the viewer.
    /// </summary>
    public float RotateX { get; set; } = 0;

    /// <summary>
    /// Rotation around the Y-axis in degrees (-180 to 180).
    /// Positive values turn the left side of the image away from the viewer.
    /// </summary>
    public float RotateY { get; set; } = 0;

    /// <summary>
    /// Rotation around the Z-axis in degrees (-180 to 180).
    /// This is equivalent to standard 2D rotation.
    /// </summary>
    public float RotateZ { get; set; } = 0;

    /// <summary>
    /// When true, the output canvas expands to fit the full transformed image.
    /// When false, the output keeps the original dimensions.
    /// </summary>
    public bool AutoResize { get; set; } = true;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (RotateX == 0 && RotateY == 0 && RotateZ == 0) return source.Copy();

        float width = source.Width;
        float height = source.Height;
        float centerX = width / 2f;
        float centerY = height / 2f;

        // Calculate perspective depth based on image size
        float depth = Math.Max(width, height) * 2f;

        // Create the 3D transformation matrix
        SKMatrix44 matrix44 = SKMatrix44.CreateIdentity();

        // Translate to center
        matrix44.PostConcat(SKMatrix44.CreateTranslation(-centerX, -centerY, 0));

        // Apply rotations
        if (RotateX != 0)
        {
            matrix44.PostConcat(SKMatrix44.CreateRotationDegrees(1, 0, 0, RotateX));
        }
        if (RotateY != 0)
        {
            matrix44.PostConcat(SKMatrix44.CreateRotationDegrees(0, 1, 0, RotateY));
        }
        if (RotateZ != 0)
        {
            matrix44.PostConcat(SKMatrix44.CreateRotationDegrees(0, 0, 1, RotateZ));
        }

        // Apply perspective
        SKMatrix44 perspective = SKMatrix44.CreateIdentity();
        perspective[3, 2] = -1f / depth;
        matrix44.PostConcat(perspective);

        // Translate back
        matrix44.PostConcat(SKMatrix44.CreateTranslation(centerX, centerY, 0));

        // Convert to 2D matrix for drawing
        SKMatrix matrix = matrix44.Matrix;

        // Calculate bounds of the transformed image
        SKPoint[] corners = new SKPoint[]
        {
            new SKPoint(0, 0),
            new SKPoint(width, 0),
            new SKPoint(width, height),
            new SKPoint(0, height)
        };

        SKPoint[] transformedCorners = new SKPoint[4];
        for (int i = 0; i < 4; i++)
        {
            transformedCorners[i] = matrix.MapPoint(corners[i]);
        }

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        foreach (var point in transformedCorners)
        {
            minX = Math.Min(minX, point.X);
            minY = Math.Min(minY, point.Y);
            maxX = Math.Max(maxX, point.X);
            maxY = Math.Max(maxY, point.Y);
        }

        int newWidth, newHeight;

        if (AutoResize)
        {
            newWidth = Math.Max(1, (int)Math.Ceiling(maxX - minX));
            newHeight = Math.Max(1, (int)Math.Ceiling(maxY - minY));

            // Adjust translation to fit in new bounds
            matrix44.PostConcat(SKMatrix44.CreateTranslation(-minX, -minY, 0));
        }
        else
        {
            newWidth = (int)width;
            newHeight = (int)height;
        }

        matrix = matrix44.Matrix;

        SKBitmap result = new SKBitmap(newWidth, newHeight, source.ColorType, source.AlphaType);
        using (SKCanvas canvas = new SKCanvas(result))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.SetMatrix(matrix);
            canvas.DrawBitmap(source, 0, 0);
        }

        return result;
    }
}
