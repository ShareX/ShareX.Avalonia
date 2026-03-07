using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Drawings;

public sealed class DrawTextEffect : ImageEffect
{
    public string Text { get; set; } = "Text";

    public DrawingPlacement Placement { get; set; } = DrawingPlacement.TopLeft;

    public SKPointI Offset { get; set; } = new SKPointI(0, 0);

    public int Angle { get; set; }

    public bool AutoHide { get; set; }

    public string FontFamily { get; set; } = "Arial";

    public float FontSize { get; set; } = 36f;

    public bool Bold { get; set; }

    public bool Italic { get; set; }

    public SKColor Color { get; set; } = new SKColor(235, 235, 235);

    public bool Outline { get; set; }

    public int OutlineSize { get; set; } = 5;

    public SKColor OutlineColor { get; set; } = new SKColor(235, 0, 0);

    public bool Shadow { get; set; }

    public SKPointI ShadowOffset { get; set; } = new SKPointI(0, 5);

    public SKColor ShadowColor { get; set; } = new SKColor(0, 0, 0, 125);

    public override string Name => "Text";

    public override ImageEffectCategory Category => ImageEffectCategory.Drawings;

    public override bool HasParameters => true;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (string.IsNullOrWhiteSpace(Text) || FontSize < 1f)
        {
            return source.Copy();
        }

        SKFontStyleWeight weight = Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
        SKFontStyleSlant slant = Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
        using SKTypeface? typeface = SKTypeface.FromFamilyName(FontFamily, weight, SKFontStyleWidth.Normal, slant);

        using SKPaint textPaint = new SKPaint
        {
            IsAntialias = true,
            Typeface = typeface,
            TextSize = FontSize
        };

        using SKPath textPath = CreateTextPath(Text, textPaint);
        if (textPath.IsEmpty)
        {
            return source.Copy();
        }

        if (Angle != 0)
        {
            SKMatrix rotation = SKMatrix.CreateRotationDegrees(Angle);
            textPath.Transform(rotation);
        }

        SKRect pathRect = textPath.Bounds;
        if (pathRect.IsEmpty)
        {
            return source.Copy();
        }

        SKSizeI textSize = new SKSizeI(
            (int)Math.Ceiling(pathRect.Width) + 1,
            (int)Math.Ceiling(pathRect.Height) + 1);

        SKPointI textPosition = DrawingEffectHelpers.GetPosition(
            Placement,
            Offset,
            new SKSizeI(source.Width, source.Height),
            textSize);

        SKRectI textRectangle = new SKRectI(
            textPosition.X,
            textPosition.Y,
            textPosition.X + textSize.Width,
            textPosition.Y + textSize.Height);

        if (AutoHide && !DrawingEffectHelpers.Contains(new SKRectI(0, 0, source.Width, source.Height), textRectangle))
        {
            return source.Copy();
        }

        SKMatrix translation = SKMatrix.CreateTranslation(textRectangle.Left - pathRect.Left, textRectangle.Top - pathRect.Top);
        textPath.Transform(translation);

        SKBitmap result = source.Copy();
        using SKCanvas canvas = new SKCanvas(result);

        if (Shadow && ShadowColor.Alpha > 0)
        {
            using SKPath shadowPath = new SKPath(textPath);
            SKMatrix shadowTranslation = SKMatrix.CreateTranslation(ShadowOffset.X, ShadowOffset.Y);
            shadowPath.Transform(shadowTranslation);

            if (Outline && OutlineSize > 0)
            {
                DrawStroke(
                    canvas,
                    shadowPath,
                    OutlineSize,
                    ShadowColor);
            }
            else
            {
                DrawFill(
                    canvas,
                    shadowPath,
                    ShadowColor);
            }
        }

        if (Outline && OutlineSize > 0)
        {
            DrawStroke(
                canvas,
                textPath,
                OutlineSize,
                OutlineColor);
        }

        DrawFill(
            canvas,
            textPath,
            Color);

        return result;
    }

    private static void DrawStroke(
        SKCanvas canvas,
        SKPath path,
        int strokeSize,
        SKColor color)
    {
        if (color.Alpha == 0)
        {
            return;
        }

        using SKPaint paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(1, strokeSize),
            StrokeJoin = SKStrokeJoin.Round,
            IsAntialias = true,
            Color = color
        };

        canvas.DrawPath(path, paint);
    }

    private static void DrawFill(
        SKCanvas canvas,
        SKPath path,
        SKColor color)
    {
        if (color.Alpha == 0)
        {
            return;
        }

        using SKPaint paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            Color = color
        };

        canvas.DrawPath(path, paint);
    }

    private static SKPath CreateTextPath(string text, SKPaint textPaint)
    {
        SKPath result = new SKPath { FillType = SKPathFillType.Winding };
        string[] lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        if (lines.Length == 0)
        {
            return result;
        }

        SKFontMetrics metrics = textPaint.FontMetrics;
        float lineHeight = Math.Max(metrics.Descent - metrics.Ascent + metrics.Leading, textPaint.TextSize);
        float baselineOffset = -metrics.Ascent;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.Length == 0)
            {
                continue;
            }

            using SKPath linePath = textPaint.GetTextPath(line, 0, baselineOffset + (i * lineHeight));
            result.AddPath(linePath);
        }

        return result;
    }

}
