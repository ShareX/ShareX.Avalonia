using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public enum TiltShiftMode
{
    Linear,
    Radial
}

public class TiltShiftImageEffect : FilterImageEffect
{
    public override string Name => "Tilt-shift";
    public override string IconKey => "IconCamera";
    public override bool HasParameters => true;

    public TiltShiftMode Mode { get; set; } = TiltShiftMode.Linear;
    public float BlurRadius { get; set; } = 12f; // 0..30
    public float FocusSize { get; set; } = 30f; // 5..90
    public float FocusPositionX { get; set; } = 50f; // 0..100
    public float FocusPositionY { get; set; } = 50f; // 0..100
    public float Falloff { get; set; } = 24f; // 1..60
    public float SaturationBoost { get; set; } = 35f; // 0..100

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        int width = source.Width;
        int height = source.Height;
        if (width <= 0 || height <= 0)
        {
            return source.Copy();
        }

        float blur = Math.Clamp(BlurRadius, 0f, 30f);
        float focusSize = Math.Clamp(FocusSize, 5f, 90f) / 100f;
        float focusX = Math.Clamp(FocusPositionX, 0f, 100f) / 100f;
        float focusY = Math.Clamp(FocusPositionY, 0f, 100f) / 100f;
        float falloff = Math.Clamp(Falloff, 1f, 60f) / 100f;
        float saturation = Math.Clamp(SaturationBoost, 0f, 100f) / 100f;

        using SKBitmap blurred = blur > 0.01f ? CreateBlurred(source, blur) : source.Copy();

        SKColor[] srcPixels = source.Pixels;
        SKColor[] blurPixels = blurred.Pixels;
        SKColor[] dstPixels = new SKColor[srcPixels.Length];

        float cx = focusX * (width - 1);
        float cy = focusY * (height - 1);
        float minDimension = Math.Min(width, height);
        float focusHalfLinear = (height * focusSize) * 0.5f;
        float focusRadiusRadial = (minDimension * focusSize) * 0.5f;
        float falloffLinear = Math.Max(1f, height * falloff);
        float falloffRadial = Math.Max(1f, minDimension * falloff);

        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                float mask = Mode == TiltShiftMode.Radial
                    ? ComputeRadialMask(x, y, cx, cy, focusRadiusRadial, falloffRadial)
                    : ComputeLinearMask(y, cy, focusHalfLinear, falloffLinear);

                SKColor src = srcPixels[row + x];
                SKColor blurColor = blurPixels[row + x];

                float r = ProceduralEffectHelper.Lerp(src.Red, blurColor.Red, mask);
                float g = ProceduralEffectHelper.Lerp(src.Green, blurColor.Green, mask);
                float b = ProceduralEffectHelper.Lerp(src.Blue, blurColor.Blue, mask);

                float localSatBoost = saturation * (1f - (mask * 0.7f));
                ApplySaturation(ref r, ref g, ref b, localSatBoost);

                dstPixels[row + x] = new SKColor(
                    ProceduralEffectHelper.ClampToByte(r),
                    ProceduralEffectHelper.ClampToByte(g),
                    ProceduralEffectHelper.ClampToByte(b),
                    src.Alpha);
            }
        }

        return new SKBitmap(width, height, source.ColorType, source.AlphaType)
        {
            Pixels = dstPixels
        };
    }

    private static SKBitmap CreateBlurred(SKBitmap source, float radius)
    {
        SKBitmap blurred = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);

        using SKCanvas canvas = new SKCanvas(blurred);
        using SKPaint paint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High,
            ImageFilter = SKImageFilter.CreateBlur(radius, radius)
        };
        canvas.DrawBitmap(source, 0, 0, paint);
        return blurred;
    }

    private static float ComputeLinearMask(float y, float centerY, float focusHalf, float falloff)
    {
        float distance = MathF.Abs(y - centerY);
        return ProceduralEffectHelper.SmoothStep(focusHalf, focusHalf + falloff, distance);
    }

    private static float ComputeRadialMask(float x, float y, float centerX, float centerY, float focusRadius, float falloff)
    {
        float dx = x - centerX;
        float dy = y - centerY;
        float distance = MathF.Sqrt((dx * dx) + (dy * dy));
        return ProceduralEffectHelper.SmoothStep(focusRadius, focusRadius + falloff, distance);
    }

    private static void ApplySaturation(ref float r, ref float g, ref float b, float amount01)
    {
        if (amount01 <= 0f)
        {
            return;
        }

        float gray = (r + g + b) / 3f;
        float factor = 1f + amount01;

        r = gray + ((r - gray) * factor);
        g = gray + ((g - gray) * factor);
        b = gray + ((b - gray) * factor);
    }
}
