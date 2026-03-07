using SkiaSharp;


namespace ShareX.ImageEditor.Core.ImageEffects.Adjustments;

public class SaturationImageEffect : AdjustmentImageEffect
{
    public override string Name => "Saturation";
    public override string IconKey => "IconFillDrip";
    public float Amount { get; set; } = 0; // -100 to 100

    public override SKBitmap Apply(SKBitmap source)
    {
        float x = 1f + (Amount / 100f);
        float lumR = 0.3086f;
        float lumG = 0.6094f;
        float lumB = 0.0820f;

        float invSat = 1f - x;

        float r = (invSat * lumR);
        float g = (invSat * lumG);
        float b = (invSat * lumB);

        float[] matrix = {
            r + x, g,     b,     0, 0,
            r,     g + x, b,     0, 0,
            r,     g,     b + x, 0, 0,
            0,     0,     0,     1, 0
        };
        return ApplyColorMatrix(source, matrix);
    }
}

