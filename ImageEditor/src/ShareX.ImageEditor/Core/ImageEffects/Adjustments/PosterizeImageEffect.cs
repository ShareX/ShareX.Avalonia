using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Adjustments;

public class PosterizeImageEffect : AdjustmentImageEffect
{
    public override string Name => "Posterize";
    public override string IconKey => "IconPalette";

    public int Levels { get; set; } = 8;

    public override SKBitmap Apply(SKBitmap source)
    {
        int levels = Math.Clamp(Levels, 2, 64);
        float scale = levels - 1;

        return ApplyPixelOperation(source, c =>
        {
            byte r = Quantize(c.Red, scale);
            byte g = Quantize(c.Green, scale);
            byte b = Quantize(c.Blue, scale);
            return new SKColor(r, g, b, c.Alpha);
        });
    }

    private static byte Quantize(byte value, float scale)
    {
        float bucket = MathF.Round(value * scale / 255f);
        float mapped = bucket * 255f / scale;
        if (mapped <= 0f) return 0;
        if (mapped >= 255f) return 255;
        return (byte)MathF.Round(mapped);
    }
}

