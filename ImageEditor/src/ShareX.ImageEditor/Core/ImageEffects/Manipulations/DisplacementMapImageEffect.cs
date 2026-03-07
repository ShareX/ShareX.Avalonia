using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Manipulations;

public class DisplacementMapImageEffect : ImageEffect
{
    public override string Name => "Displacement map";
    public override ImageEffectCategory Category => ImageEffectCategory.Manipulations;
    public override bool HasParameters => true;

    // Uses source red channel as X-map and green channel as Y-map.
    public float AmountX { get; set; } = 20f;
    public float AmountY { get; set; } = 20f;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        int width = source.Width;
        int height = source.Height;
        int right = width - 1;
        int bottom = height - 1;

        if (Math.Abs(AmountX) < 0.0001f && Math.Abs(AmountY) < 0.0001f)
        {
            return source.Copy();
        }

        SKColor[] srcPixels = source.Pixels;
        SKColor[] dstPixels = new SKColor[srcPixels.Length];

        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                SKColor map = srcPixels[row + x];
                float dx = ((map.Red / 255f) - 0.5f) * 2f * AmountX;
                float dy = ((map.Green / 255f) - 0.5f) * 2f * AmountY;

                int sampleX = Clamp((int)MathF.Round(x + dx), 0, right);
                int sampleY = Clamp((int)MathF.Round(y + dy), 0, bottom);

                dstPixels[row + x] = srcPixels[sampleY * width + sampleX];
            }
        }

        return new SKBitmap(width, height, source.ColorType, source.AlphaType) { Pixels = dstPixels };
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
