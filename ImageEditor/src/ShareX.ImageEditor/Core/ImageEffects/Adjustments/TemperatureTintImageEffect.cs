using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Adjustments;

public class TemperatureTintImageEffect : AdjustmentImageEffect
{
    public override string Name => "Temperature / Tint";
    public override string IconKey => "IconArrowsH";

    public float Temperature { get; set; } // -100..100
    public float Tint { get; set; } // -100..100

    public override SKBitmap Apply(SKBitmap source)
    {
        float temperature = Math.Clamp(Temperature, -100f, 100f);
        float tint = Math.Clamp(Tint, -100f, 100f);

        if (Math.Abs(temperature) < 0.0001f && Math.Abs(tint) < 0.0001f)
        {
            return source.Copy();
        }

        float tempDelta = temperature / 100f * 64f;
        float tintDelta = tint / 100f * 64f;

        return ApplyPixelOperation(source, c =>
        {
            float r = c.Red + tempDelta - tintDelta * 0.25f;
            float g = c.Green + tintDelta;
            float b = c.Blue - tempDelta - tintDelta * 0.25f;

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
