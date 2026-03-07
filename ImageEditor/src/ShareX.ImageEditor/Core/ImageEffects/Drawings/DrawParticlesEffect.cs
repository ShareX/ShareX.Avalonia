using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Drawings;

public sealed class DrawParticlesEffect : ImageEffect
{
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg"];

    private int _imageCount = 1;

    public string ImageFolder { get; set; } = string.Empty;

    public int ImageCount
    {
        get => _imageCount;
        set => _imageCount = Math.Clamp(value, 1, 1000);
    }

    public bool Background { get; set; }

    public bool RandomSize { get; set; }

    public int RandomSizeMin { get; set; } = 64;

    public int RandomSizeMax { get; set; } = 128;

    public bool RandomAngle { get; set; }

    public int RandomAngleMin { get; set; }

    public int RandomAngleMax { get; set; } = 360;

    public bool RandomOpacity { get; set; }

    public int RandomOpacityMin { get; set; }

    public int RandomOpacityMax { get; set; } = 100;

    public bool NoOverlap { get; set; }

    public int NoOverlapOffset { get; set; }

    public bool EdgeOverlap { get; set; }

    public override string Name => "Particles";

    public override ImageEffectCategory Category => ImageEffectCategory.Drawings;

    public override bool HasParameters => true;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        string folderPath = DrawingEffectHelpers.ExpandVariables(ImageFolder);
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return source.Copy();
        }

        string[] files = Directory.EnumerateFiles(folderPath)
            .Where(x => ImageExtensions.Contains(Path.GetExtension(x), StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (files.Length == 0)
        {
            return source.Copy();
        }

        SKBitmap result = Background
            ? new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType)
            : source.Copy();

        using SKCanvas canvas = new SKCanvas(result);
        if (Background)
        {
            canvas.Clear(SKColors.Transparent);
        }

        List<SKRectI> imageRectangles = [];
        Dictionary<string, SKBitmap> imageCache = new(StringComparer.OrdinalIgnoreCase);

        try
        {
            for (int i = 0; i < ImageCount; i++)
            {
                string file = files[Random.Shared.Next(0, files.Length)];
                if (!imageCache.TryGetValue(file, out SKBitmap? particleBitmap))
                {
                    particleBitmap = SKBitmap.Decode(file);
                    if (particleBitmap != null)
                    {
                        imageCache[file] = particleBitmap;
                    }
                }

                if (particleBitmap is null || particleBitmap.Width <= 0 || particleBitmap.Height <= 0)
                {
                    continue;
                }

                DrawParticle(result, canvas, particleBitmap, imageRectangles);
            }

            if (Background)
            {
                canvas.DrawBitmap(source, 0, 0);
            }
        }
        finally
        {
            foreach (SKBitmap cachedBitmap in imageCache.Values)
            {
                cachedBitmap.Dispose();
            }
        }

        return result;
    }

    private void DrawParticle(SKBitmap target, SKCanvas canvas, SKBitmap particle, List<SKRectI> imageRectangles)
    {
        int width;
        int height;

        if (RandomSize)
        {
            int size = NextInt(Math.Min(RandomSizeMin, RandomSizeMax), Math.Max(RandomSizeMin, RandomSizeMax));
            width = size;
            height = size;

            if (particle.Width > particle.Height)
            {
                height = (int)Math.Round(size * ((double)particle.Height / particle.Width));
            }
            else if (particle.Width < particle.Height)
            {
                width = (int)Math.Round(size * ((double)particle.Width / particle.Height));
            }
        }
        else
        {
            width = particle.Width;
            height = particle.Height;
        }

        if (width < 1 || height < 1)
        {
            return;
        }

        int minOffsetX = EdgeOverlap ? -width + 1 : 0;
        int minOffsetY = EdgeOverlap ? -height + 1 : 0;
        int maxOffsetX = target.Width - (EdgeOverlap ? 0 : width) - 1;
        int maxOffsetY = target.Height - (EdgeOverlap ? 0 : height) - 1;

        SKRectI rect = default;
        int attemptCount = 0;

        do
        {
            attemptCount++;

            if (attemptCount > 1000)
            {
                return;
            }

            int x = NextInt(Math.Min(minOffsetX, maxOffsetX), Math.Max(minOffsetX, maxOffsetX));
            int y = NextInt(Math.Min(minOffsetY, maxOffsetY), Math.Max(minOffsetY, maxOffsetY));
            rect = new SKRectI(x, y, x + width, y + height);
        } while (NoOverlap && imageRectangles.Any(x => DrawingEffectHelpers.Intersects(x, DrawingEffectHelpers.Inflate(rect, NoOverlapOffset))));

        imageRectangles.Add(rect);

        int alpha = 255;
        if (RandomOpacity)
        {
            int opacity = NextInt(Math.Min(RandomOpacityMin, RandomOpacityMax), Math.Max(RandomOpacityMin, RandomOpacityMax));
            opacity = Math.Clamp(opacity, 0, 100);
            alpha = (int)Math.Round(opacity / 100f * 255);
        }

        canvas.Save();

        if (RandomAngle)
        {
            float moveX = rect.Left + (rect.Width / 2f);
            float moveY = rect.Top + (rect.Height / 2f);
            int rotate = NextInt(Math.Min(RandomAngleMin, RandomAngleMax), Math.Max(RandomAngleMin, RandomAngleMax));

            canvas.Translate(moveX, moveY);
            canvas.RotateDegrees(rotate);
            canvas.Translate(-moveX, -moveY);
        }

        using SKPaint paint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High
        };

        if (alpha < 255)
        {
            paint.ColorFilter = SKColorFilter.CreateBlendMode(new SKColor(255, 255, 255, (byte)alpha), SKBlendMode.Modulate);
        }

        canvas.DrawBitmap(particle, new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom), paint);
        canvas.Restore();
    }

    private static int NextInt(int min, int max)
    {
        if (min == max)
        {
            return min;
        }

        if (min > max)
        {
            (min, max) = (max, min);
        }

        return Random.Shared.Next(min, max);
    }
}

