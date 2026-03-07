using SkiaSharp;


namespace ShareX.ImageEditor.Core.ImageEffects.Adjustments;

public class BlackAndWhiteImageEffect : AdjustmentImageEffect
{
    public override string Name => "Black and White";
    public override string IconKey => "IconAdjust";
    public override SKBitmap Apply(SKBitmap source)
    {
        return ApplyPixelOperation(source, (color) =>
        {
            float lum = 0.2126f * color.Red + 0.7152f * color.Green + 0.0722f * color.Blue;
            return lum > 127 ? SKColors.White : SKColors.Black;
        });
    }
}

