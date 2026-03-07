using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class MeanRemovalImageEffect : FilterImageEffect
{
    public override string Name => "Mean removal";
    public override string IconKey => "IconMagic";

    private static readonly float[] Kernel =
    {
        -1, -1, -1,
        -1,  9, -1,
        -1, -1, -1
    };

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        return ConvolutionHelper.Apply3x3(source, Kernel);
    }
}
