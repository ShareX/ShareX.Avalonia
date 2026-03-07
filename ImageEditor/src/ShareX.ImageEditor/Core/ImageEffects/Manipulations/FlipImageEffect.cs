using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Manipulations;

public class FlipImageEffect : ImageEffect
{
    public enum FlipDirection { Horizontal, Vertical }

    private readonly FlipDirection? _direction;
    private readonly string _name;

    public bool Horizontally { get; set; }
    public bool Vertically { get; set; }

    public override string Name => _name;
    public override ImageEffectCategory Category => ImageEffectCategory.Manipulations;
    public override bool HasParameters => _direction == null;

    public FlipImageEffect()
    {
        _name = "Flip";
    }

    private FlipImageEffect(FlipDirection direction, string name)
    {
        _direction = direction;
        _name = name;
    }

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        bool flipHorizontal = _direction == FlipDirection.Horizontal || (_direction == null && Horizontally);
        bool flipVertical = _direction == FlipDirection.Vertical || (_direction == null && Vertically);

        if (!flipHorizontal && !flipVertical)
        {
            return source.Copy();
        }

        SKBitmap result = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);
        using (SKCanvas canvas = new SKCanvas(result))
        {
            canvas.Clear(SKColors.Transparent);
            if (flipHorizontal && flipVertical)
            {
                canvas.Scale(-1, -1, source.Width / 2f, source.Height / 2f);
            }
            else if (flipHorizontal)
            {
                canvas.Scale(-1, 1, source.Width / 2f, source.Height / 2f);
            }
            else
            {
                canvas.Scale(1, -1, source.Width / 2f, source.Height / 2f);
            }
            canvas.DrawBitmap(source, 0, 0);
        }
        return result;
    }

    public static FlipImageEffect Horizontal => new FlipImageEffect(FlipDirection.Horizontal, "Flip horizontal");
    public static FlipImageEffect Vertical => new FlipImageEffect(FlipDirection.Vertical, "Flip vertical");
}

