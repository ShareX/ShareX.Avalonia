using SkiaSharp;


namespace ShareX.ImageEditor.Core.ImageEffects.Adjustments;

public class ContrastImageEffect : AdjustmentImageEffect
{
    public override string Name => "Contrast";
    public override string IconKey => "IconAdjust";
    public float Amount { get; set; } = 0; // -100 to 100

    public override SKBitmap Apply(SKBitmap source)
    {
        float scale = (100f + Amount) / 100f;
        scale = scale * scale;
        float shift = 0.5f * (1f - scale);

        float[] matrix = {
            scale, 0, 0, 0, shift,
            0, scale, 0, 0, shift,
            0, 0, scale, 0, shift,
            0, 0, 0, 1, 0
        };
        return ApplyColorMatrix(source, matrix);
    }
}

