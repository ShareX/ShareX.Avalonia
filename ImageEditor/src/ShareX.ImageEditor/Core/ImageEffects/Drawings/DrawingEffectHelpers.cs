using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Drawings;

internal static class DrawingEffectHelpers
{
    public static string ExpandVariables(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return Environment.ExpandEnvironmentVariables(path.Trim());
    }

    public static SKPointI GetPosition(DrawingPlacement placement, SKPointI offset, SKSizeI backgroundSize, SKSizeI objectSize)
    {
        int midX = (int)Math.Round((backgroundSize.Width / 2f) - (objectSize.Width / 2f));
        int midY = (int)Math.Round((backgroundSize.Height / 2f) - (objectSize.Height / 2f));
        int right = backgroundSize.Width - objectSize.Width;
        int bottom = backgroundSize.Height - objectSize.Height;

        return placement switch
        {
            DrawingPlacement.TopCenter => new SKPointI(midX, offset.Y),
            DrawingPlacement.TopRight => new SKPointI(right - offset.X, offset.Y),
            DrawingPlacement.MiddleLeft => new SKPointI(offset.X, midY),
            DrawingPlacement.MiddleCenter => new SKPointI(midX, midY),
            DrawingPlacement.MiddleRight => new SKPointI(right - offset.X, midY),
            DrawingPlacement.BottomLeft => new SKPointI(offset.X, bottom - offset.Y),
            DrawingPlacement.BottomCenter => new SKPointI(midX, bottom - offset.Y),
            DrawingPlacement.BottomRight => new SKPointI(right - offset.X, bottom - offset.Y),
            _ => new SKPointI(offset.X, offset.Y)
        };
    }

    public static SKSizeI ApplyAspectRatio(int width, int height, SKBitmap bitmap)
    {
        int newWidth;
        int newHeight;

        if (width == 0)
        {
            newWidth = (int)Math.Round((float)height / bitmap.Height * bitmap.Width);
            newHeight = height;
        }
        else if (height == 0)
        {
            newWidth = width;
            newHeight = (int)Math.Round((float)width / bitmap.Width * bitmap.Height);
        }
        else
        {
            newWidth = width;
            newHeight = height;
        }

        return new SKSizeI(newWidth, newHeight);
    }

    public static SKFilterQuality GetFilterQuality(DrawingInterpolationMode interpolationMode)
    {
        return interpolationMode switch
        {
            DrawingInterpolationMode.Bicubic => SKFilterQuality.Medium,
            DrawingInterpolationMode.HighQualityBilinear => SKFilterQuality.Medium,
            DrawingInterpolationMode.Bilinear => SKFilterQuality.Low,
            DrawingInterpolationMode.NearestNeighbor => SKFilterQuality.None,
            _ => SKFilterQuality.High
        };
    }

    public static SKBlendMode GetBlendMode(DrawingCompositingMode compositingMode)
    {
        return compositingMode == DrawingCompositingMode.SourceCopy
            ? SKBlendMode.Src
            : SKBlendMode.SrcOver;
    }

    public static SKRectI Inflate(SKRectI rect, int amount)
    {
        return new SKRectI(rect.Left - amount, rect.Top - amount, rect.Right + amount, rect.Bottom + amount);
    }

    public static bool Contains(SKRectI outerRect, SKRectI innerRect)
    {
        return innerRect.Left >= outerRect.Left &&
               innerRect.Top >= outerRect.Top &&
               innerRect.Right <= outerRect.Right &&
               innerRect.Bottom <= outerRect.Bottom;
    }

    public static bool Intersects(SKRectI left, SKRectI right)
    {
        return left.Left < right.Right &&
               left.Right > right.Left &&
               left.Top < right.Bottom &&
               left.Bottom > right.Top;
    }

    public static SKBitmap RotateFlip(SKBitmap source, DrawingImageRotateFlipType rotateFlip)
    {
        return rotateFlip switch
        {
            DrawingImageRotateFlipType.Rotate90 => Rotate90(source),
            DrawingImageRotateFlipType.Rotate180 => Rotate180(source),
            DrawingImageRotateFlipType.Rotate270 => Rotate270(source),
            DrawingImageRotateFlipType.FlipX => FlipHorizontal(source),
            DrawingImageRotateFlipType.FlipY => FlipVertical(source),
            DrawingImageRotateFlipType.Rotate90FlipX => Rotate90FlipHorizontal(source),
            DrawingImageRotateFlipType.Rotate90FlipY => Rotate90FlipVertical(source),
            _ => source.Copy()
        };
    }

    private static SKBitmap Rotate90(SKBitmap source)
    {
        SKBitmap result = new SKBitmap(source.Height, source.Width, source.ColorType, source.AlphaType);
        using SKCanvas canvas = new SKCanvas(result);
        canvas.Translate(source.Height, 0);
        canvas.RotateDegrees(90);
        canvas.DrawBitmap(source, 0, 0);
        return result;
    }

    private static SKBitmap Rotate180(SKBitmap source)
    {
        SKBitmap result = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);
        using SKCanvas canvas = new SKCanvas(result);
        canvas.Translate(source.Width, source.Height);
        canvas.RotateDegrees(180);
        canvas.DrawBitmap(source, 0, 0);
        return result;
    }

    private static SKBitmap Rotate270(SKBitmap source)
    {
        SKBitmap result = new SKBitmap(source.Height, source.Width, source.ColorType, source.AlphaType);
        using SKCanvas canvas = new SKCanvas(result);
        canvas.Translate(0, source.Width);
        canvas.RotateDegrees(270);
        canvas.DrawBitmap(source, 0, 0);
        return result;
    }

    private static SKBitmap FlipHorizontal(SKBitmap source)
    {
        SKBitmap result = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);
        using SKCanvas canvas = new SKCanvas(result);
        canvas.Translate(source.Width, 0);
        canvas.Scale(-1, 1);
        canvas.DrawBitmap(source, 0, 0);
        return result;
    }

    private static SKBitmap FlipVertical(SKBitmap source)
    {
        SKBitmap result = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);
        using SKCanvas canvas = new SKCanvas(result);
        canvas.Translate(0, source.Height);
        canvas.Scale(1, -1);
        canvas.DrawBitmap(source, 0, 0);
        return result;
    }

    private static SKBitmap Rotate90FlipHorizontal(SKBitmap source)
    {
        using SKBitmap rotated = Rotate90(source);
        return FlipHorizontal(rotated);
    }

    private static SKBitmap Rotate90FlipVertical(SKBitmap source)
    {
        using SKBitmap rotated = Rotate90(source);
        return FlipVertical(rotated);
    }
}

