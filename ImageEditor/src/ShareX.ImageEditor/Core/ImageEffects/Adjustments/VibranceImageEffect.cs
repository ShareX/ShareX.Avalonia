using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Adjustments;

public class VibranceImageEffect : AdjustmentImageEffect
{
    public override string Name => "Vibrance";
    public override string IconKey => "IconTint";

    // Similar to photo editors: boosts low-saturation colors more than already-saturated ones.
    public float Amount { get; set; } = 25f;

    public override SKBitmap Apply(SKBitmap source)
    {
        float amount = Math.Clamp(Amount, -100f, 100f) / 100f;
        if (Math.Abs(amount) < 0.0001f)
        {
            return source.Copy();
        }

        return ApplyPixelOperation(source, c =>
        {
            float r = c.Red;
            float g = c.Green;
            float b = c.Blue;

            float max = MathF.Max(r, MathF.Max(g, b));
            float min = MathF.Min(r, MathF.Min(g, b));
            float saturation = (max - min) / 255f;
            float gray = (r + g + b) / 3f;

            float factor = amount >= 0f
                ? 1f + (amount * (1f - saturation))
                : 1f + amount;

            r = gray + ((r - gray) * factor);
            g = gray + ((g - gray) * factor);
            b = gray + ((b - gray) * factor);

            return new SKColor(ClampToByte(r), ClampToByte(g), ClampToByte(b), c.Alpha);
        });
    }

    private static byte ClampToByte(float value)
    {
        if (value <= 0f) return 0;
        if (value >= 255f) return 255;
        return (byte)MathF.Round(value);
    }
}

