using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class EdgeDetectImageEffect : FilterImageEffect
{
    public override string Name => "Edge detect";
    public override string IconKey => "IconMagic";

    private static readonly float[] Kernel =
    {
        -1, -1, -1,
         0,  0,  0,
         1,  1,  1
    };

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        return ConvolutionHelper.Apply3x3(source, Kernel, gain: 1f, bias: 127f);
    }
}
