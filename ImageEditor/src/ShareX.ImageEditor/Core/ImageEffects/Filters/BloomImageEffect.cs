using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class BloomImageEffect : FilterImageEffect
{
    public override string Name => "Bloom";
    public override string IconKey => "IconSparkles";
    public override bool HasParameters => true;

    public float Threshold { get; set; } = 65f; // 0..100
    public float SoftKnee { get; set; } = 35f; // 0..100
    public float Radius { get; set; } = 24f; // 1..100
    public float Intensity { get; set; } = 85f; // 0..200

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        float threshold = Math.Clamp(Threshold, 0f, 100f) / 100f;
        float softKnee = Math.Clamp(SoftKnee, 0f, 100f) / 100f;
        float radius = Math.Clamp(Radius, 1f, 100f);
        float intensity = Math.Clamp(Intensity, 0f, 200f) / 100f;

        if (intensity <= 0f)
        {
            return source.Copy();
        }

        using SKBitmap brightPass = ExtractBrightPass(source, threshold, softKnee);
        using SKBitmap blurred = ApplyBlurWithClamp(brightPass, radius);

        SKColor[] srcPixels = source.Pixels;
        SKColor[] bloomPixels = blurred.Pixels;
        SKColor[] dstPixels = new SKColor[srcPixels.Length];

        for (int i = 0; i < srcPixels.Length; i++)
        {
            SKColor src = srcPixels[i];
            SKColor bloom = bloomPixels[i];

            byte r = BlendBloomChannel(src.Red, bloom.Red, intensity);
            byte g = BlendBloomChannel(src.Green, bloom.Green, intensity);
            byte b = BlendBloomChannel(src.Blue, bloom.Blue, intensity);

            dstPixels[i] = new SKColor(r, g, b, src.Alpha);
        }

        return new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType)
        {
            Pixels = dstPixels
        };
    }

    private static SKBitmap ExtractBrightPass(SKBitmap source, float threshold, float softKnee)
    {
        SKColor[] srcPixels = source.Pixels;
        SKColor[] brightPixels = new SKColor[srcPixels.Length];

        float halfKnee = softKnee * 0.5f;
        float low = Math.Clamp(threshold - halfKnee, 0f, 1f);
        float high = Math.Clamp(threshold + halfKnee, 0f, 1f);

        for (int i = 0; i < srcPixels.Length; i++)
        {
            SKColor c = srcPixels[i];
            float luminance = Luminance(c);
            float mask = softKnee > 0f
                ? ProceduralEffectHelper.SmoothStep(low, high, luminance)
                : (luminance >= threshold ? 1f : 0f);

            if (mask <= 0f)
            {
                brightPixels[i] = new SKColor(0, 0, 0, 0);
                continue;
            }

            float gain = mask * (0.35f + (0.65f * luminance));

            brightPixels[i] = new SKColor(
                ProceduralEffectHelper.ClampToByte(c.Red * gain),
                ProceduralEffectHelper.ClampToByte(c.Green * gain),
                ProceduralEffectHelper.ClampToByte(c.Blue * gain),
                ProceduralEffectHelper.ClampToByte(c.Alpha * gain));
        }

        return new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType)
        {
            Pixels = brightPixels
        };
    }

    private static SKBitmap ApplyBlurWithClamp(SKBitmap source, float radius)
    {
        if (radius <= 0.01f)
        {
            return source.Copy();
        }

        int padding = Math.Max(2, (int)MathF.Ceiling(radius * 2f));
        int expandedWidth = source.Width + (padding * 2);
        int expandedHeight = source.Height + (padding * 2);

        using SKBitmap expanded = new SKBitmap(expandedWidth, expandedHeight, source.ColorType, source.AlphaType);
        using (SKCanvas expandCanvas = new SKCanvas(expanded))
        {
            using SKShader shader = SKShader.CreateBitmap(
                source,
                SKShaderTileMode.Clamp,
                SKShaderTileMode.Clamp,
                SKMatrix.CreateTranslation(padding, padding));
            using SKPaint paint = new SKPaint { Shader = shader };
            expandCanvas.DrawRect(new SKRect(0, 0, expandedWidth, expandedHeight), paint);
        }

        float sigma = Math.Max(0.001f, radius / 3f);

        using SKBitmap blurredExpanded = new SKBitmap(expandedWidth, expandedHeight, source.ColorType, source.AlphaType);
        using (SKCanvas blurCanvas = new SKCanvas(blurredExpanded))
        {
            using SKPaint blurPaint = new SKPaint
            {
                ImageFilter = SKImageFilter.CreateBlur(sigma, sigma)
            };
            blurCanvas.DrawBitmap(expanded, 0, 0, blurPaint);
        }

        SKBitmap result = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);
        using (SKCanvas resultCanvas = new SKCanvas(result))
        {
            resultCanvas.DrawBitmap(
                blurredExpanded,
                new SKRect(padding, padding, padding + source.Width, padding + source.Height),
                new SKRect(0, 0, source.Width, source.Height));
        }

        return result;
    }

    private static byte BlendBloomChannel(byte sourceChannel, byte bloomChannel, float intensity)
    {
        float src = sourceChannel / 255f;
        float bloom = (bloomChannel / 255f) * intensity;

        float screened = 1f - ((1f - src) * (1f - Math.Clamp(bloom, 0f, 1f)));
        if (bloom > 1f)
        {
            screened += (bloom - 1f) * 0.40f;
        }

        return ProceduralEffectHelper.ClampToByte(screened * 255f);
    }

    private static float Luminance(SKColor c)
    {
        return ((0.2126f * c.Red) + (0.7152f * c.Green) + (0.0722f * c.Blue)) / 255f;
    }
}
