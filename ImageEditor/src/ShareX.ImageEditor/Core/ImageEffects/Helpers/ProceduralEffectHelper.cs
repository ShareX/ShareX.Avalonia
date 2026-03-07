using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Helpers;

internal static class ProceduralEffectHelper
{
    public static int ClampInt(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static float Clamp01(float value)
    {
        if (value <= 0f) return 0f;
        if (value >= 1f) return 1f;
        return value;
    }

    public static float Lerp(float a, float b, float t)
    {
        return a + ((b - a) * t);
    }

    public static float SmoothStep(float edge0, float edge1, float value)
    {
        if (Math.Abs(edge1 - edge0) < 0.000001f)
        {
            return value >= edge1 ? 1f : 0f;
        }

        float t = Clamp01((value - edge0) / (edge1 - edge0));
        return t * t * (3f - (2f * t));
    }

    public static float Hash01(int x, int y, int seed = 0)
    {
        unchecked
        {
            uint h = 2166136261u;
            h = (h ^ (uint)x) * 16777619u;
            h = (h ^ (uint)y) * 16777619u;
            h = (h ^ (uint)seed) * 16777619u;
            h ^= h >> 13;
            h *= 1274126177u;
            h ^= h >> 16;
            return (h & 0x00FFFFFFu) / 16777215f;
        }
    }

    public static SKColor BilinearSample(SKColor[] pixels, int width, int height, float x, float y)
    {
        int right = width - 1;
        int bottom = height - 1;

        x = Math.Clamp(x, 0f, right);
        y = Math.Clamp(y, 0f, bottom);

        int x0 = (int)MathF.Floor(x);
        int y0 = (int)MathF.Floor(y);
        int x1 = x0 < right ? x0 + 1 : right;
        int y1 = y0 < bottom ? y0 + 1 : bottom;

        float tx = x - x0;
        float ty = y - y0;

        SKColor c00 = pixels[(y0 * width) + x0];
        SKColor c10 = pixels[(y0 * width) + x1];
        SKColor c01 = pixels[(y1 * width) + x0];
        SKColor c11 = pixels[(y1 * width) + x1];

        float r0 = Lerp(c00.Red, c10.Red, tx);
        float g0 = Lerp(c00.Green, c10.Green, tx);
        float b0 = Lerp(c00.Blue, c10.Blue, tx);
        float a0 = Lerp(c00.Alpha, c10.Alpha, tx);

        float r1 = Lerp(c01.Red, c11.Red, tx);
        float g1 = Lerp(c01.Green, c11.Green, tx);
        float b1 = Lerp(c01.Blue, c11.Blue, tx);
        float a1 = Lerp(c01.Alpha, c11.Alpha, tx);

        return new SKColor(
            ClampToByte(Lerp(r0, r1, ty)),
            ClampToByte(Lerp(g0, g1, ty)),
            ClampToByte(Lerp(b0, b1, ty)),
            ClampToByte(Lerp(a0, a1, ty)));
    }

    public static byte ClampToByte(float value)
    {
        if (value <= 0f) return 0;
        if (value >= 255f) return 255;
        return (byte)MathF.Round(value);
    }
}
