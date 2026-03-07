using SkiaSharp;


namespace ShareX.ImageEditor.Core.ImageEffects.Adjustments;

public class InvertImageEffect : AdjustmentImageEffect
{
    public override string Name => "Invert";
    public override string IconKey => "IconExchangeAlt";
    public override SKBitmap Apply(SKBitmap source)
    {
        float[] matrix = {
            -1,  0,  0, 0, 1,
             0, -1,  0, 0, 1,
             0,  0, -1, 0, 1,
             0,  0,  0, 1, 0
        };
        return ApplyColorMatrix(source, matrix);
    }
}

