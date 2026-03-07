using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Adjustments;

public class SolarizeImageEffect : AdjustmentImageEffect
{
    public override string Name => "Solarize";
    public override string IconKey => "IconSun";

    public int Threshold { get; set; } = 128;

    public override SKBitmap Apply(SKBitmap source)
    {
        int threshold = Math.Clamp(Threshold, 0, 255);

        return ApplyPixelOperation(source, c =>
        {
            byte r = c.Red > threshold ? (byte)(255 - c.Red) : c.Red;
            byte g = c.Green > threshold ? (byte)(255 - c.Green) : c.Green;
            byte b = c.Blue > threshold ? (byte)(255 - c.Blue) : c.Blue;
            return new SKColor(r, g, b, c.Alpha);
        });
    }
}

