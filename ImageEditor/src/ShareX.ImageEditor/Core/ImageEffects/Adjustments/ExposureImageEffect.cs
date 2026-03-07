using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Adjustments;

public class ExposureImageEffect : AdjustmentImageEffect
{
    public override string Name => "Exposure";
    public override string IconKey => "IconRotateCW";

    // Exposure in stops. Typical range: -5..5
    public float Amount { get; set; }

    public override SKBitmap Apply(SKBitmap source)
    {
        float amount = Math.Clamp(Amount, -10f, 10f);
        if (Math.Abs(amount) < 0.0001f)
        {
            return source.Copy();
        }

        float gain = MathF.Pow(2f, amount);

        return ApplyPixelOperation(source, c =>
        {
            byte r = ClampToByte(c.Red * gain);
            byte g = ClampToByte(c.Green * gain);
            byte b = ClampToByte(c.Blue * gain);
            return new SKColor(r, g, b, c.Alpha);
        });
    }

    private static byte ClampToByte(float value)
    {
        if (value <= 0f) return 0;
        if (value >= 255f) return 255;
        return (byte)MathF.Round(value);
    }
}
