using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Adjustments;

public class ReplaceColorImageEffect : AdjustmentImageEffect
{
    public override string Name => "Replace Color";
    public override string IconKey => "IconSync";
    public SKColor TargetColor { get; set; }
    public SKColor ReplaceColor { get; set; }
    public float Tolerance { get; set; }

    public override SKBitmap Apply(SKBitmap source)
    {
        int tol = (int)(Tolerance * 2.55f);
        return ApplyPixelOperation(source, (c) =>
        {
            if (ImageHelpers.ColorsMatch(c, TargetColor, tol))
            {
                return ReplaceColor;
            }
            return c;
        });
    }
}

