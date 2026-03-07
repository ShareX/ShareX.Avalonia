using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class ConvolutionMatrixImageEffect : FilterImageEffect
{
    public override string Name => "Convolution matrix";
    public override string IconKey => "IconAdjust";
    public override bool HasParameters => true;

    public int X0Y0 { get; set; }
    public int X1Y0 { get; set; }
    public int X2Y0 { get; set; }

    public int X0Y1 { get; set; }
    public int X1Y1 { get; set; } = 1;
    public int X2Y1 { get; set; }

    public int X0Y2 { get; set; }
    public int X1Y2 { get; set; }
    public int X2Y2 { get; set; }

    public double Factor { get; set; } = 1d;
    public int Offset { get; set; }

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        float factor = (float)Math.Max(0.01d, Factor);
        float gain = 1f / factor;
        float bias = Offset;

        float[] kernel =
        {
            X0Y0, X1Y0, X2Y0,
            X0Y1, X1Y1, X2Y1,
            X0Y2, X1Y2, X2Y2
        };

        return ConvolutionHelper.Apply3x3(source, kernel, gain, bias);
    }
}
