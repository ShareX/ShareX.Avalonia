using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Adjustments;

public class ThresholdImageEffect : AdjustmentImageEffect
{
    public override string Name => "Threshold";
    public override string IconKey => "IconHighlighter";

    public int Value { get; set; } = 128;

    public override SKBitmap Apply(SKBitmap source)
    {
        int threshold = Math.Clamp(Value, 0, 255);

        return ApplyPixelOperation(source, c =>
        {
            int luma = ((c.Red * 77) + (c.Green * 150) + (c.Blue * 29)) >> 8;
            byte bw = (byte)(luma >= threshold ? 255 : 0);
            return new SKColor(bw, bw, bw, c.Alpha);
        });
    }
}

