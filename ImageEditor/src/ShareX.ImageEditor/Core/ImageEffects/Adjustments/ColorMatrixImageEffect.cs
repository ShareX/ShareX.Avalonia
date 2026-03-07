using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Adjustments;

public class ColorMatrixImageEffect : AdjustmentImageEffect
{
    public override string Name => "Color matrix";
    public override string IconKey => "IconTableCells";

    // Red output
    public float Rr { get; set; } = 1f;
    public float Rg { get; set; }
    public float Rb { get; set; }
    public float Ra { get; set; }
    public float Ro { get; set; }

    // Green output
    public float Gr { get; set; }
    public float Gg { get; set; } = 1f;
    public float Gb { get; set; }
    public float Ga { get; set; }
    public float Go { get; set; }

    // Blue output
    public float Br { get; set; }
    public float Bg { get; set; }
    public float Bb { get; set; } = 1f;
    public float Ba { get; set; }
    public float Bo { get; set; }

    // Alpha output
    public float Ar { get; set; }
    public float Ag { get; set; }
    public float Ab { get; set; }
    public float Aa { get; set; } = 1f;
    public float Ao { get; set; }

    public override SKBitmap Apply(SKBitmap source)
    {
        float[] matrix =
        {
            Rr, Rg, Rb, Ra, Ro,
            Gr, Gg, Gb, Ga, Go,
            Br, Bg, Bb, Ba, Bo,
            Ar, Ag, Ab, Aa, Ao
        };

        return ApplyColorMatrix(source, matrix);
    }
}
