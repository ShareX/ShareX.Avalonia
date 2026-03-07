using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class InkSplatterDripsImageEffect : FilterImageEffect
{
    public override string Name => "Ink splatter + drips";
    public override string IconKey => "IconInkStroke";
    public override bool HasParameters => true;

    public float InkAmount { get; set; } = 55f; // 0..100
    public float DripLength { get; set; } = 45f; // 0..100
    public float Spread { get; set; } = 35f; // 0..100
    public float PaperFade { get; set; } = 20f; // 0..100

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        float inkAmount = Math.Clamp(InkAmount, 0f, 100f) / 100f;
        float dripLength = Math.Clamp(DripLength, 0f, 100f) / 100f;
        float spread = Math.Clamp(Spread, 0f, 100f) / 100f;
        float paperFade = Math.Clamp(PaperFade, 0f, 100f) / 100f;

        if (inkAmount <= 0.0001f && paperFade <= 0.0001f)
        {
            return source.Copy();
        }

        int width = source.Width;
        int height = source.Height;
        int seed = Random.Shared.Next(1, int.MaxValue);

        float splatCell = 14f + ((1f - spread) * 16f);
        float splatRadius = 3.6f + (spread * 11.5f);
        float splatDensity = 0.04f + (inkAmount * 0.25f);
        float dripCell = 12f + ((1f - spread) * 24f);
        float dripProbability = 0.12f + (inkAmount * 0.42f);

        SKColor[] srcPixels = source.Pixels;
        SKColor[] dstPixels = new SKColor[srcPixels.Length];

        Parallel.For(0, height, y =>
        {
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                SKColor src = srcPixels[row + x];
                float r = src.Red;
                float g = src.Green;
                float b = src.Blue;
                float a = src.Alpha;

                if (paperFade > 0.0001f)
                {
                    float gray = (0.2126f * r) + (0.7152f * g) + (0.0722f * b);
                    float paperNoise = ((ProceduralEffectHelper.Hash01(x, y, seed ^ 131) * 2f) - 1f) * paperFade * 8f;
                    float paperTarget = gray + (9f * paperFade) + paperNoise;
                    float paperMix = paperFade * 0.48f;
                    r = ProceduralEffectHelper.Lerp(r, paperTarget, paperMix);
                    g = ProceduralEffectHelper.Lerp(g, paperTarget, paperMix);
                    b = ProceduralEffectHelper.Lerp(b, paperTarget + 4f, paperMix);
                }

                float splatter = SampleSplatMask(x, y, splatCell, splatRadius, splatDensity, seed ^ 991);
                float drips = SampleDripMask(x, y, height, dripCell, dripProbability, dripLength, spread, seed ^ 3701);
                float speckle = MathF.Pow(ProceduralEffectHelper.Hash01(x, y, seed ^ 5239), 26f) * inkAmount * 0.85f;

                float inkMask = Math.Clamp((splatter * 0.9f) + (drips * 0.85f) + speckle, 0f, 1f) * inkAmount;
                if (inkMask > 0.0001f)
                {
                    float bleed = MathF.Max(0f, splatter - 0.25f) * 0.3f;
                    float inkR = 11f + (bleed * 10f);
                    float inkG = 11f + (bleed * 8f);
                    float inkB = 15f + (bleed * 12f);
                    r = ProceduralEffectHelper.Lerp(r, inkR, inkMask);
                    g = ProceduralEffectHelper.Lerp(g, inkG, inkMask);
                    b = ProceduralEffectHelper.Lerp(b, inkB, inkMask);
                }

                dstPixels[row + x] = new SKColor(
                    ProceduralEffectHelper.ClampToByte(r),
                    ProceduralEffectHelper.ClampToByte(g),
                    ProceduralEffectHelper.ClampToByte(b),
                    ProceduralEffectHelper.ClampToByte(a));
            }
        });

        return new SKBitmap(width, height, source.ColorType, source.AlphaType)
        {
            Pixels = dstPixels
        };
    }

    private static float SampleSplatMask(int x, int y, float cellSize, float radius, float density, int seed)
    {
        int cellX = (int)MathF.Floor(x / cellSize);
        int cellY = (int)MathF.Floor(y / cellSize);
        float best = 0f;

        for (int oy = -1; oy <= 1; oy++)
        {
            for (int ox = -1; ox <= 1; ox++)
            {
                int nx = cellX + ox;
                int ny = cellY + oy;
                float spawn = ProceduralEffectHelper.Hash01(nx, ny, seed);
                if (spawn > density)
                {
                    continue;
                }

                float centerX = (nx + ProceduralEffectHelper.Hash01(nx, ny, seed ^ 177)) * cellSize;
                float centerY = (ny + ProceduralEffectHelper.Hash01(nx, ny, seed ^ 571)) * cellSize;
                float dx = x - centerX;
                float dy = y - centerY;
                float dist = MathF.Sqrt((dx * dx) + (dy * dy));
                float localRadius = radius * (0.42f + (ProceduralEffectHelper.Hash01(nx, ny, seed ^ 887) * 0.95f));
                float core = 1f - ProceduralEffectHelper.SmoothStep(localRadius * 0.28f, localRadius, dist);
                if (core > best)
                {
                    best = core;
                }
            }
        }

        return best;
    }

    private static float SampleDripMask(int x, int y, int height, float cellWidth, float probability, float dripLength, float spread, int seed)
    {
        int column = (int)MathF.Floor(x / cellWidth);
        float strongest = 0f;

        for (int c = column - 1; c <= column + 1; c++)
        {
            float trigger = ProceduralEffectHelper.Hash01(c, 0, seed);
            if (trigger > probability)
            {
                continue;
            }

            float centerX = (c + ProceduralEffectHelper.Hash01(c, 1, seed ^ 211)) * cellWidth;
            float startY = ProceduralEffectHelper.Hash01(c, 2, seed ^ 419) * height * 0.75f;
            float len = (0.08f + (dripLength * 0.85f)) * (0.4f + (ProceduralEffectHelper.Hash01(c, 3, seed ^ 677) * 0.9f)) * height;
            float thickness = (0.9f + (spread * 3.2f)) * (0.65f + (ProceduralEffectHelper.Hash01(c, 4, seed ^ 991) * 0.95f));

            float dy = y - startY;
            if (dy < 0f || dy > len)
            {
                continue;
            }

            float dx = MathF.Abs(x - centerX);
            float line = 1f - ProceduralEffectHelper.SmoothStep(thickness * 0.45f, thickness, dx);
            if (line <= 0f)
            {
                continue;
            }

            float tailFade = 1f - ProceduralEffectHelper.SmoothStep(0f, len, dy);
            float drip = line * (0.55f + (0.45f * tailFade));

            float tipY = startY + len;
            float tipDx = x - centerX;
            float tipDy = y - tipY;
            float tipDist = MathF.Sqrt((tipDx * tipDx) + ((tipDy * tipDy) * 0.55f));
            float tip = 1f - ProceduralEffectHelper.SmoothStep(thickness * 0.2f, thickness * 1.6f, tipDist);
            drip = MathF.Max(drip, tip * 0.9f);

            if (drip > strongest)
            {
                strongest = drip;
            }
        }

        return strongest;
    }
}
