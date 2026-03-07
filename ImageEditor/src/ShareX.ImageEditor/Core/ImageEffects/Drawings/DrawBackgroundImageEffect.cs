using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Drawings;

public sealed class DrawBackgroundImageEffect : ImageEffect
{
    public string ImageFilePath { get; set; } = string.Empty;

    public bool Center { get; set; } = true;

    public bool Tile { get; set; }

    public override string Name => "Background image";

    public override ImageEffectCategory Category => ImageEffectCategory.Drawings;

    public override bool HasParameters => true;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        string imagePath = DrawingEffectHelpers.ExpandVariables(ImageFilePath);
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            return source.Copy();
        }

        using SKBitmap? backgroundImage = SKBitmap.Decode(imagePath);
        if (backgroundImage is null || backgroundImage.Width <= 0 || backgroundImage.Height <= 0)
        {
            return source.Copy();
        }

        SKBitmap result = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);
        using SKCanvas canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        if (Tile)
        {
            int tileX = 0;
            int tileY = 0;

            if (Center)
            {
                tileX = (result.Width - backgroundImage.Width) / 2 % backgroundImage.Width;
                tileY = (result.Height - backgroundImage.Height) / 2 % backgroundImage.Height;
            }

            using SKShader shader = SKShader.CreateBitmap(
                backgroundImage,
                SKShaderTileMode.Repeat,
                SKShaderTileMode.Repeat);
            using SKPaint paint = new SKPaint { Shader = shader, IsAntialias = true, FilterQuality = SKFilterQuality.High };

            if (Center)
            {
                canvas.Save();
                canvas.Translate(tileX, tileY);
                canvas.DrawRect(-tileX, -tileY, result.Width, result.Height, paint);
                canvas.Restore();
            }
            else
            {
                canvas.DrawRect(0, 0, result.Width, result.Height, paint);
            }
        }
        else
        {
            float aspectRatio = (float)backgroundImage.Width / backgroundImage.Height;
            int width = result.Width;
            int height = (int)(width / aspectRatio);

            if (height < result.Height)
            {
                height = result.Height;
                width = (int)(height * aspectRatio);
            }

            int x = Center ? (result.Width - width) / 2 : 0;
            int y = Center ? (result.Height - height) / 2 : 0;

            using SKPaint paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
            canvas.DrawBitmap(backgroundImage, new SKRect(x, y, x + width, y + height), paint);
        }

        canvas.DrawBitmap(source, 0, 0);
        return result;
    }
}
