using SkiaSharp;


namespace ShareX.ImageEditor.Core.ImageEffects.Adjustments;

public class SepiaImageEffect : AdjustmentImageEffect
{
    public override string Name => "Sepia";
    public override string IconKey => "IconCoffee";
    public override bool HasParameters => true;
    public float Strength { get; set; } = 100f;

    public override SKBitmap Apply(SKBitmap source)
    {
        float s = Math.Clamp(Strength / 100f, 0f, 1f);

        if (s <= 0) return source.Copy();

        float[] sepiaMatrix = {
            0.393f, 0.769f, 0.189f, 0, 0,
            0.349f, 0.686f, 0.168f, 0, 0,
            0.272f, 0.534f, 0.131f, 0, 0,
            0,      0,      0,      1, 0
        };

        float[] identityMatrix = {
            1, 0, 0, 0, 0,
            0, 1, 0, 0, 0,
            0, 0, 1, 0, 0,
            0, 0, 0, 1, 0
        };

        float[] matrix = new float[20];
        for (int i = 0; i < 20; i++)
        {
            matrix[i] = identityMatrix[i] * (1 - s) + sepiaMatrix[i] * s;
        }

        return ApplyColorMatrix(source, matrix);
    }
}

