using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Drawings;

public sealed class DrawCheckerboardEffect : ImageEffect
{
    private int _size = 10;

    public int Size
    {
        get => _size;
        set => _size = Math.Max(1, value);
    }

    public SKColor Color { get; set; } = new SKColor(211, 211, 211);

    public SKColor Color2 { get; set; } = SKColors.White;

    public override string Name => "Checkerboard";

    public override ImageEffectCategory Category => ImageEffectCategory.Drawings;

    public override bool HasParameters => true;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        using SKBitmap pattern = CreateCheckerPattern(Size, Size, Color, Color2);
        using SKShader shader = SKShader.CreateBitmap(pattern, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);
        using SKPaint checkerPaint = new SKPaint { Shader = shader, IsAntialias = true };

        SKBitmap result = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);
        using SKCanvas canvas = new SKCanvas(result);
        canvas.DrawRect(0, 0, result.Width, result.Height, checkerPaint);
        canvas.DrawBitmap(source, 0, 0);
        return result;
    }

    private static SKBitmap CreateCheckerPattern(int width, int height, SKColor color1, SKColor color2)
    {
        SKBitmap bitmap = new SKBitmap(width * 2, height * 2);
        using SKCanvas canvas = new SKCanvas(bitmap);
        using SKPaint paint1 = new SKPaint { Color = color1, Style = SKPaintStyle.Fill, IsAntialias = false };
        using SKPaint paint2 = new SKPaint { Color = color2, Style = SKPaintStyle.Fill, IsAntialias = false };

        canvas.DrawRect(0, 0, width, height, paint1);
        canvas.DrawRect(width, height, width, height, paint1);
        canvas.DrawRect(width, 0, width, height, paint2);
        canvas.DrawRect(0, height, width, height, paint2);

        return bitmap;
    }
}

