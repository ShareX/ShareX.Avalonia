using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class VignetteImageEffect : FilterImageEffect
{
    public override string Name => "Vignette";
    public override string IconKey => "IconEyeDropper";
    public override bool HasParameters => true;

    public float Strength { get; set; } = 0.5f;
    public float Radius { get; set; } = 0.75f;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        float strength = Math.Clamp(Strength, 0f, 1f);
        float radius = Math.Clamp(Radius, 0.05f, 0.999f);
        if (strength <= 0f)
        {
            return source.Copy();
        }

        int width = source.Width;
        int height = source.Height;

        float cx = (width - 1) * 0.5f;
        float cy = (height - 1) * 0.5f;
        float invCx = cx > 0f ? 1f / cx : 1f;
        float invCy = cy > 0f ? 1f / cy : 1f;
        const float invSqrt2 = 0.70710678f;

        SKColor[] srcPixels = source.Pixels;
        SKColor[] dstPixels = new SKColor[srcPixels.Length];

        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            float dy = (y - cy) * invCy;

            for (int x = 0; x < width; x++)
            {
                float dx = (x - cx) * invCx;
                float distance01 = MathF.Sqrt((dx * dx) + (dy * dy)) * invSqrt2;

                float t = (distance01 - radius) / (1f - radius);
                if (t < 0f) t = 0f;
                if (t > 1f) t = 1f;

                // Smoothstep falloff for soft edge transition.
                float falloff = t * t * (3f - (2f * t));
                float factor = 1f - (strength * falloff);

                SKColor c = srcPixels[row + x];
                byte r = ClampToByte(c.Red * factor);
                byte g = ClampToByte(c.Green * factor);
                byte b = ClampToByte(c.Blue * factor);
                dstPixels[row + x] = new SKColor(r, g, b, c.Alpha);
            }
        }

        return new SKBitmap(width, height, source.ColorType, source.AlphaType)
        {
            Pixels = dstPixels
        };
    }

    private static byte ClampToByte(float value)
    {
        if (value <= 0f) return 0;
        if (value >= 255f) return 255;
        return (byte)MathF.Round(value);
    }
}
