using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class CrosshatchImageEffect : FilterImageEffect
{
    public override string Name => "Crosshatch";
    public override string IconKey => "IconHash";
    public override bool HasParameters => true;

    public int LineSpacing { get; set; } = 8; // 4..24
    public float LineThickness { get; set; } = 1.2f; // 0.5..4
    public float Contrast { get; set; } = 120f; // 50..200
    public int LayerCount { get; set; } = 4; // 1..6

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        int spacing = Math.Clamp(LineSpacing, 4, 24);
        float thickness = Math.Clamp(LineThickness, 0.5f, 4f);
        float contrast = Math.Clamp(Contrast, 50f, 200f) / 100f;
        int layers = Math.Clamp(LayerCount, 1, 6);

        int width = source.Width;
        int height = source.Height;
        if (width <= 0 || height <= 0)
        {
            return source.Copy();
        }

        SKColor[] srcPixels = source.Pixels;
        SKColor[] dstPixels = new SKColor[srcPixels.Length];

        float[] thresholds = { 0.14f, 0.28f, 0.42f, 0.58f, 0.74f, 0.88f };

        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                SKColor src = srcPixels[row + x];
                float luminance = ((0.2126f * src.Red) + (0.7152f * src.Green) + (0.0722f * src.Blue)) / 255f;
                luminance = Math.Clamp(((luminance - 0.5f) * contrast) + 0.5f, 0f, 1f);

                float darkness = 1f - luminance;
                int activeLayers = 0;

                if (layers >= 1 && darkness > thresholds[0] && IsLine(MathF.Abs((x + y) * 0.7071f), spacing, thickness)) activeLayers++;
                if (layers >= 2 && darkness > thresholds[1] && IsLine(MathF.Abs((x - y) * 0.7071f), spacing, thickness)) activeLayers++;
                if (layers >= 3 && darkness > thresholds[2] && IsLine(y, spacing, thickness)) activeLayers++;
                if (layers >= 4 && darkness > thresholds[3] && IsLine(x, spacing, thickness)) activeLayers++;
                if (layers >= 5 && darkness > thresholds[4] && IsLine((x * 0.5f) + y, spacing, thickness)) activeLayers++;
                if (layers >= 6 && darkness > thresholds[5] && IsLine((x * 0.5f) - y, spacing, thickness)) activeLayers++;

                float ink = activeLayers / (float)layers;
                float value = luminance * (1f - (ink * 0.94f));
                value = Math.Clamp(MathF.Min(value, luminance + 0.06f), 0f, 1f);

                byte v = ProceduralEffectHelper.ClampToByte(value * 255f);
                dstPixels[row + x] = new SKColor(v, v, v, src.Alpha);
            }
        }

        return new SKBitmap(width, height, source.ColorType, source.AlphaType)
        {
            Pixels = dstPixels
        };
    }

    private static bool IsLine(float value, float spacing, float thickness)
    {
        float mod = value % spacing;
        if (mod < 0f)
        {
            mod += spacing;
        }
        float distance = MathF.Min(mod, spacing - mod);
        return distance <= (thickness * 0.5f);
    }
}
