using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Adjustments;

public class FilmEmulationImageEffect : AdjustmentImageEffect
{
    public enum FilmEmulationPreset
    {
        Classic = 0,
        Warm = 1,
        Cool = 2,
        Faded = 3,
        CrossProcessed = 4
    }

    public override string Name => "Film emulation";
    public override string IconKey => "IconFilm";

    public FilmEmulationPreset Preset { get; set; } = FilmEmulationPreset.Classic;
    public float ToneStrength { get; set; } = 65f; // 0..100
    public float GrainAmount { get; set; } = 12f; // 0..100
    public float FadeAmount { get; set; } = 10f; // 0..100
    public float ContrastAmount { get; set; } = 110f; // 50..150 (%)
    public int Seed { get; set; } = 1977;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        float toneStrength = Math.Clamp(ToneStrength, 0f, 100f) / 100f;
        float grain = Math.Clamp(GrainAmount, 0f, 100f) / 100f;
        float fade = Math.Clamp(FadeAmount, 0f, 100f) / 100f;
        float contrast = Math.Clamp(ContrastAmount, 50f, 150f) / 100f;

        if (toneStrength <= 0f && grain <= 0f && fade <= 0f && Math.Abs(contrast - 1f) < 0.0001f)
        {
            return source.Copy();
        }

        int width = source.Width;
        int height = source.Height;
        SKColor[] srcPixels = source.Pixels;
        SKColor[] dstPixels = new SKColor[srcPixels.Length];

        for (int y = 0; y < height; y++)
        {
            int row = y * width;

            for (int x = 0; x < width; x++)
            {
                SKColor src = srcPixels[row + x];

                float r = src.Red / 255f;
                float g = src.Green / 255f;
                float b = src.Blue / 255f;

                (float tr, float tg, float tb) = ApplyPreset(Preset, r, g, b);

                r = ProceduralEffectHelper.Lerp(r, tr, toneStrength);
                g = ProceduralEffectHelper.Lerp(g, tg, toneStrength);
                b = ProceduralEffectHelper.Lerp(b, tb, toneStrength);

                r = ((r - 0.5f) * contrast) + 0.5f;
                g = ((g - 0.5f) * contrast) + 0.5f;
                b = ((b - 0.5f) * contrast) + 0.5f;

                float fadeScale = 1f - (fade * 0.35f);
                float blackLift = fade * 0.06f;
                r = (r * fadeScale) + blackLift;
                g = (g * fadeScale) + blackLift;
                b = (b * fadeScale) + blackLift;

                if (grain > 0f)
                {
                    float n0 = (ProceduralEffectHelper.Hash01(x, y, Seed) * 2f) - 1f;
                    float n1 = (ProceduralEffectHelper.Hash01(x, y, Seed ^ 117) * 2f) - 1f;
                    float n2 = (ProceduralEffectHelper.Hash01(x, y, Seed ^ 911) * 2f) - 1f;
                    float grainAmp = grain * 0.10f;
                    r += n0 * grainAmp;
                    g += n1 * grainAmp;
                    b += n2 * grainAmp;
                }

                dstPixels[row + x] = new SKColor(
                    ProceduralEffectHelper.ClampToByte(ProceduralEffectHelper.Clamp01(r) * 255f),
                    ProceduralEffectHelper.ClampToByte(ProceduralEffectHelper.Clamp01(g) * 255f),
                    ProceduralEffectHelper.ClampToByte(ProceduralEffectHelper.Clamp01(b) * 255f),
                    src.Alpha);
            }
        }

        return new SKBitmap(width, height, source.ColorType, source.AlphaType)
        {
            Pixels = dstPixels
        };
    }

    private static (float r, float g, float b) ApplyPreset(FilmEmulationPreset preset, float r, float g, float b)
    {
        return preset switch
        {
            FilmEmulationPreset.Warm => Warm(r, g, b),
            FilmEmulationPreset.Cool => Cool(r, g, b),
            FilmEmulationPreset.Faded => Faded(r, g, b),
            FilmEmulationPreset.CrossProcessed => CrossProcessed(r, g, b),
            _ => Classic(r, g, b)
        };
    }

    private static (float r, float g, float b) Classic(float r, float g, float b)
    {
        float tr = (r * 1.05f) + (g * 0.02f);
        float tg = (g * 1.02f) + (r * 0.01f);
        float tb = (b * 0.94f) + (g * 0.02f);
        return (ProceduralEffectHelper.Clamp01(tr), ProceduralEffectHelper.Clamp01(tg), ProceduralEffectHelper.Clamp01(tb));
    }

    private static (float r, float g, float b) Warm(float r, float g, float b)
    {
        float tr = (r * 1.10f) + (g * 0.015f);
        float tg = g * 1.04f;
        float tb = (b * 0.88f) + (r * 0.01f);
        return (ProceduralEffectHelper.Clamp01(tr), ProceduralEffectHelper.Clamp01(tg), ProceduralEffectHelper.Clamp01(tb));
    }

    private static (float r, float g, float b) Cool(float r, float g, float b)
    {
        float tr = (r * 0.94f) + (b * 0.02f);
        float tg = (g * 1.01f) + (b * 0.01f);
        float tb = (b * 1.10f) + (g * 0.01f);
        return (ProceduralEffectHelper.Clamp01(tr), ProceduralEffectHelper.Clamp01(tg), ProceduralEffectHelper.Clamp01(tb));
    }

    private static (float r, float g, float b) Faded(float r, float g, float b)
    {
        float lum = (0.2126f * r) + (0.7152f * g) + (0.0722f * b);
        float tr = ProceduralEffectHelper.Lerp(r, lum + 0.04f, 0.22f);
        float tg = ProceduralEffectHelper.Lerp(g, lum + 0.03f, 0.18f);
        float tb = ProceduralEffectHelper.Lerp(b, lum - 0.01f, 0.26f);
        return (ProceduralEffectHelper.Clamp01(tr), ProceduralEffectHelper.Clamp01(tg), ProceduralEffectHelper.Clamp01(tb));
    }

    private static (float r, float g, float b) CrossProcessed(float r, float g, float b)
    {
        float tr = MathF.Pow(MathF.Max(r, 0.0001f), 0.88f) * 1.08f;
        float tg = MathF.Pow(MathF.Max(g, 0.0001f), 1.05f) * 0.98f;
        float tb = MathF.Pow(MathF.Max(b, 0.0001f), 0.82f) * 1.12f;
        return (ProceduralEffectHelper.Clamp01(tr), ProceduralEffectHelper.Clamp01(tg), ProceduralEffectHelper.Clamp01(tb));
    }
}
