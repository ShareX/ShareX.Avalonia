using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class CrystalizeShardsImageEffect : FilterImageEffect
{
    public override string Name => "Crystalize shards";
    public override string IconKey => "IconGem";
    public override bool HasParameters => true;

    public int ShardSize { get; set; } = 22; // 6..80
    public float Jitter { get; set; } = 65f; // 0..100
    public float EdgeStrength { get; set; } = 75f; // 0..100
    public float Shine { get; set; } = 30f; // 0..100
    public int Seed { get; set; } = 4242;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        float cellSize = Math.Clamp(ShardSize, 6, 80);
        float invCell = 1f / cellSize;
        float jitter = Math.Clamp(Jitter, 0f, 100f) / 100f;
        float edgeStrength = Math.Clamp(EdgeStrength, 0f, 100f) / 100f;
        float shine = Math.Clamp(Shine, 0f, 100f) / 100f;

        int width = source.Width;
        int height = source.Height;
        SKColor[] srcPixels = source.Pixels;
        SKColor[] dstPixels = new SKColor[srcPixels.Length];

        Parallel.For(0, height, y =>
        {
            int row = y * width;

            for (int x = 0; x < width; x++)
            {
                float fx = x * invCell;
                float fy = y * invCell;
                int cx = (int)MathF.Floor(fx);
                int cy = (int)MathF.Floor(fy);

                float nearestDist = float.MaxValue;
                float secondDist = float.MaxValue;
                float nearestSeedX = x;
                float nearestSeedY = y;
                int nearestCellX = cx;
                int nearestCellY = cy;

                for (int oy = -1; oy <= 1; oy++)
                {
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        int cellX = cx + ox;
                        int cellY = cy + oy;

                        float jx = ((ProceduralEffectHelper.Hash01(cellX, cellY, Seed) * 2f) - 1f) * 0.45f * jitter;
                        float jy = ((ProceduralEffectHelper.Hash01(cellX, cellY, Seed ^ 911) * 2f) - 1f) * 0.45f * jitter;

                        float seedX = (cellX + 0.5f + jx) * cellSize;
                        float seedY = (cellY + 0.5f + jy) * cellSize;

                        float dx = seedX - x;
                        float dy = seedY - y;
                        float dist = (dx * dx) + (dy * dy);

                        if (dist < nearestDist)
                        {
                            secondDist = nearestDist;
                            nearestDist = dist;
                            nearestSeedX = seedX;
                            nearestSeedY = seedY;
                            nearestCellX = cellX;
                            nearestCellY = cellY;
                        }
                        else if (dist < secondDist)
                        {
                            secondDist = dist;
                        }
                    }
                }

                SKColor sample = ProceduralEffectHelper.BilinearSample(srcPixels, width, height, nearestSeedX, nearestSeedY);
                float r = sample.Red;
                float g = sample.Green;
                float b = sample.Blue;
                float a = sample.Alpha;

                float nearest = MathF.Sqrt(MathF.Max(0f, nearestDist));
                float second = MathF.Sqrt(MathF.Max(0f, secondDist));
                float borderDiff = second - nearest;
                float edge = 1f - ProceduralEffectHelper.SmoothStep(0f, cellSize * 0.12f, borderDiff);
                edge *= edgeStrength;

                float facetNoise = ProceduralEffectHelper.Hash01(nearestCellX, nearestCellY, Seed ^ 1777);
                float centerFactor = 1f - MathF.Min(1f, nearest / MathF.Max(1f, cellSize));
                float shade = 1f + ((facetNoise - 0.5f) * 0.25f) + (centerFactor * shine * 0.45f);

                r *= shade;
                g *= shade;
                b *= shade;

                if (edge > 0.001f)
                {
                    float edgeDark = edge * 0.78f;
                    r = ProceduralEffectHelper.Lerp(r, r * 0.22f, edgeDark);
                    g = ProceduralEffectHelper.Lerp(g, g * 0.26f, edgeDark);
                    b = ProceduralEffectHelper.Lerp(b, b * 0.34f, edgeDark);

                    float glint = edge * (0.20f + (facetNoise * 0.25f));
                    r += 38f * glint;
                    g += 52f * glint;
                    b += 80f * glint;
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
}

