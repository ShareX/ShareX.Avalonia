using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class SmoothImageEffect : FilterImageEffect
{
    public override string Name => "Smooth";
    public override string IconKey => "IconMagic";

    private static readonly float[] Kernel =
    {
        1f / 9f, 1f / 9f, 1f / 9f,
        1f / 9f, 1f / 9f, 1f / 9f,
        1f / 9f, 1f / 9f, 1f / 9f
    };

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        return ConvolutionHelper.Apply3x3(source, Kernel);
    }
}
