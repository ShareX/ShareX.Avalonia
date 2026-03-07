using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class AddNoiseImageEffect : FilterImageEffect
{
    public override string Name => "Add noise";
    public override string IconKey => "IconCloud";
    public override bool HasParameters => true;

    public float Amount { get; set; } = 8f;
    public int Seed { get; set; } = 1337;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        float amount = Math.Clamp(Amount, 0f, 100f);
        int amplitude = (int)MathF.Round(amount * 1.27f); // 0..127
        if (amplitude <= 0)
        {
            return source.Copy();
        }

        SKColor[] srcPixels = source.Pixels;
        SKColor[] dstPixels = new SKColor[srcPixels.Length];
        uint seed = unchecked((uint)Seed * 747796405u);

        for (int i = 0; i < srcPixels.Length; i++)
        {
            SKColor c = srcPixels[i];

            uint h = Hash((uint)i ^ seed);
            int nr = (((int)(h & 0xFF)) - 128) * amplitude / 128;
            h = Hash(h ^ 0x9E3779B9u);
            int ng = (((int)(h & 0xFF)) - 128) * amplitude / 128;
            h = Hash(h ^ 0x85EBCA6Bu);
            int nb = (((int)(h & 0xFF)) - 128) * amplitude / 128;

            byte r = ClampToByte(c.Red + nr);
            byte g = ClampToByte(c.Green + ng);
            byte b = ClampToByte(c.Blue + nb);

            dstPixels[i] = new SKColor(r, g, b, c.Alpha);
        }

        return new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType)
        {
            Pixels = dstPixels
        };
    }

    private static uint Hash(uint x)
    {
        x ^= x >> 16;
        x *= 0x7FEB352Du;
        x ^= x >> 15;
        x *= 0x846CA68Bu;
        x ^= x >> 16;
        return x;
    }

    private static byte ClampToByte(int value)
    {
        if (value <= 0) return 0;
        if (value >= 255) return 255;
        return (byte)value;
    }
}

