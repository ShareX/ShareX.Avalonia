using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class BlockGlitchImageEffect : FilterImageEffect
{
    public override string Name => "Block glitch / Databending";
    public override string IconKey => "IconFileImage";
    public override bool HasParameters => true;

    public int BlockCount { get; set; } = 36; // 1..240
    public int MinBlockWidth { get; set; } = 24; // 4..400
    public int MaxBlockWidth { get; set; } = 200; // 4..900
    public int MinBlockHeight { get; set; } = 6; // 2..200
    public int MaxBlockHeight { get; set; } = 50; // 2..500
    public int MaxDisplacement { get; set; } = 50; // 0..500
    public int ChannelShift { get; set; } = 4; // 0..64
    public float NoiseAmount { get; set; } = 10f; // 0..100
    public int Seed { get; set; } = 1945;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        int width = source.Width;
        int height = source.Height;
        if (width <= 0 || height <= 0)
        {
            return source.Copy();
        }

        int blockCount = Math.Clamp(BlockCount, 1, 240);
        int minW = Math.Clamp(MinBlockWidth, 4, 400);
        int maxW = Math.Clamp(MaxBlockWidth, minW, 900);
        int minH = Math.Clamp(MinBlockHeight, 2, 200);
        int maxH = Math.Clamp(MaxBlockHeight, minH, 500);
        int maxShift = Math.Clamp(MaxDisplacement, 0, 500);
        int channelShift = Math.Clamp(ChannelShift, 0, 64);
        float noise = Math.Clamp(NoiseAmount, 0f, 100f) / 100f;

        SKColor[] src = source.Pixels;
        SKColor[] dst = new SKColor[src.Length];
        Array.Copy(src, dst, src.Length);

        Random random = new Random(Seed);

        for (int i = 0; i < blockCount; i++)
        {
            int bw = random.Next(minW, maxW + 1);
            int bh = random.Next(minH, maxH + 1);

            int x = random.Next(0, Math.Max(1, width - 1));
            int y = random.Next(0, Math.Max(1, height - 1));

            int right = Math.Min(width, x + bw);
            int bottom = Math.Min(height, y + bh);
            if (right <= x || bottom <= y)
            {
                continue;
            }

            int dx = random.Next(-maxShift, maxShift + 1);
            int dy = random.Next(-Math.Max(1, maxShift / 6), Math.Max(2, (maxShift / 6) + 1));

            int redShift = random.Next(-channelShift, channelShift + 1);
            int blueShift = random.Next(-channelShift, channelShift + 1);

            for (int py = y; py < bottom; py++)
            {
                for (int px = x; px < right; px++)
                {
                    int sx = Clamp(px + dx, 0, width - 1);
                    int sy = Clamp(py + dy, 0, height - 1);

                    int srX = Clamp(sx + redShift, 0, width - 1);
                    int sbX = Clamp(sx - blueShift, 0, width - 1);

                    SKColor baseColor = src[(sy * width) + sx];
                    SKColor rColor = src[(sy * width) + srX];
                    SKColor bColor = src[(sy * width) + sbX];

                    float nr = noise > 0f ? ((float)random.NextDouble() * 2f - 1f) * noise * 48f : 0f;
                    float ng = noise > 0f ? ((float)random.NextDouble() * 2f - 1f) * noise * 48f : 0f;
                    float nb = noise > 0f ? ((float)random.NextDouble() * 2f - 1f) * noise * 48f : 0f;

                    dst[(py * width) + px] = new SKColor(
                        ProceduralEffectHelper.ClampToByte(rColor.Red + nr),
                        ProceduralEffectHelper.ClampToByte(baseColor.Green + ng),
                        ProceduralEffectHelper.ClampToByte(bColor.Blue + nb),
                        baseColor.Alpha);
                }
            }
        }

        return new SKBitmap(width, height, source.ColorType, source.AlphaType)
        {
            Pixels = dst
        };
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
