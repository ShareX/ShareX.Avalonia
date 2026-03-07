using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Manipulations;

public class AutoCropImageEffect : ImageEffect
{
    private readonly SKColor _color;
    private readonly int _tolerance;

    public override string Name => "Auto crop image";
    public override ImageEffectCategory Category => ImageEffectCategory.Manipulations;

    public AutoCropImageEffect(SKColor color, int tolerance = 0)
    {
        _color = color;
        _tolerance = tolerance;
    }

    public AutoCropImageEffect()
    {
    }

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        int width = source.Width;
        int height = source.Height;

        int minX = width, minY = height, maxX = 0, maxY = 0;
        bool hasContent = false;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                SKColor pixel = source.GetPixel(x, y);
                if (!ImageHelpers.ColorsMatch(pixel, _color, _tolerance))
                {
                    hasContent = true;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (!hasContent)
        {
            return new SKBitmap(1, 1, source.ColorType, source.AlphaType);
        }

        int cropWidth = maxX - minX + 1;
        int cropHeight = maxY - minY + 1;

        return ImageHelpers.Crop(source, minX, minY, cropWidth, cropHeight);
    }
}

