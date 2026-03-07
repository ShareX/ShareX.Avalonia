using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Drawings;

public sealed class DrawImageEffect : ImageEffect
{
    private int _opacity = 100;

    public string ImageLocation { get; set; } = string.Empty;

    public DrawingPlacement Placement { get; set; } = DrawingPlacement.TopLeft;

    public SKPointI Offset { get; set; } = new SKPointI(0, 0);

    public DrawingImageSizeMode SizeMode { get; set; } = DrawingImageSizeMode.DontResize;

    public SKSizeI Size { get; set; } = new SKSizeI(0, 0);

    public DrawingImageRotateFlipType RotateFlip { get; set; } = DrawingImageRotateFlipType.None;

    public bool Tile { get; set; }

    public bool AutoHide { get; set; }

    public DrawingInterpolationMode InterpolationMode { get; set; } = DrawingInterpolationMode.HighQualityBicubic;

    public DrawingCompositingMode CompositingMode { get; set; } = DrawingCompositingMode.SourceOver;

    public int Opacity
    {
        get => _opacity;
        set => _opacity = Math.Clamp(value, 0, 100);
    }

    public override string Name => "Image";

    public override ImageEffectCategory Category => ImageEffectCategory.Drawings;

    public override bool HasParameters => true;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (Opacity < 1 || (SizeMode != DrawingImageSizeMode.DontResize && Size.Width <= 0 && Size.Height <= 0))
        {
            return source.Copy();
        }

        string imagePath = DrawingEffectHelpers.ExpandVariables(ImageLocation);
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            return source.Copy();
        }

        using SKBitmap? sourceWatermark = SKBitmap.Decode(imagePath);
        if (sourceWatermark is null || sourceWatermark.Width <= 0 || sourceWatermark.Height <= 0)
        {
            return source.Copy();
        }

        using SKBitmap watermark = DrawingEffectHelpers.RotateFlip(sourceWatermark, RotateFlip);

        SKSizeI imageSize = SizeMode switch
        {
            DrawingImageSizeMode.AbsoluteSize => DrawingEffectHelpers.ApplyAspectRatio(
                Size.Width == -1 ? source.Width : Size.Width,
                Size.Height == -1 ? source.Height : Size.Height,
                watermark),
            DrawingImageSizeMode.PercentageOfWatermark => DrawingEffectHelpers.ApplyAspectRatio(
                (int)Math.Round(Size.Width / 100f * watermark.Width),
                (int)Math.Round(Size.Height / 100f * watermark.Height),
                watermark),
            DrawingImageSizeMode.PercentageOfCanvas => DrawingEffectHelpers.ApplyAspectRatio(
                (int)Math.Round(Size.Width / 100f * source.Width),
                (int)Math.Round(Size.Height / 100f * source.Height),
                watermark),
            _ => new SKSizeI(watermark.Width, watermark.Height)
        };

        if (imageSize.Width <= 0 || imageSize.Height <= 0)
        {
            return source.Copy();
        }

        SKPointI imagePosition = DrawingEffectHelpers.GetPosition(
            Placement,
            Offset,
            new SKSizeI(source.Width, source.Height),
            imageSize);

        SKRectI imageRect = new SKRectI(
            imagePosition.X,
            imagePosition.Y,
            imagePosition.X + imageSize.Width,
            imagePosition.Y + imageSize.Height);

        if (AutoHide && !DrawingEffectHelpers.Contains(new SKRectI(0, 0, source.Width, source.Height), imageRect))
        {
            return source.Copy();
        }

        SKBitmap result = source.Copy();
        using SKCanvas canvas = new SKCanvas(result);
        using SKPaint paint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = DrawingEffectHelpers.GetFilterQuality(InterpolationMode),
            BlendMode = DrawingEffectHelpers.GetBlendMode(CompositingMode)
        };

        if (Opacity < 100)
        {
            byte alpha = (byte)Math.Round(255 * (Opacity / 100f));
            paint.ColorFilter = SKColorFilter.CreateBlendMode(new SKColor(255, 255, 255, alpha), SKBlendMode.Modulate);
        }

        if (Tile)
        {
            using SKShader shader = SKShader.CreateBitmap(
                watermark,
                SKShaderTileMode.Repeat,
                SKShaderTileMode.Repeat);
            paint.Shader = shader;
            canvas.Save();
            canvas.Translate(imageRect.Left, imageRect.Top);
            canvas.DrawRect(0, 0, imageRect.Width, imageRect.Height, paint);
            canvas.Restore();
        }
        else
        {
            canvas.DrawBitmap(watermark, new SKRect(imageRect.Left, imageRect.Top, imageRect.Right, imageRect.Bottom), paint);
        }

        return result;
    }
}
