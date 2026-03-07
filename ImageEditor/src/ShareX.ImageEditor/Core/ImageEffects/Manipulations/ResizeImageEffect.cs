using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Manipulations;

public class ResizeImageEffect : ImageEffect
{
    private readonly int _width;
    private readonly int _height;
    private readonly bool _maintainAspectRatio;
    private readonly string _name;

    public override string Name => _name;
    public override ImageEffectCategory Category => ImageEffectCategory.Manipulations;
    public override bool HasParameters => true;

    public ResizeImageEffect(int width, int height, bool maintainAspectRatio = false)
    {
        _width = width;
        _height = height;
        _maintainAspectRatio = maintainAspectRatio;
        _name = "Resize image";
    }

    public ResizeImageEffect()
    {
        _name = "Resize image";
    }

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        int width = _width > 0 ? _width : source.Width;
        int height = _height > 0 ? _height : source.Height;

        if (width <= 0) width = source.Width;
        if (height <= 0) height = source.Height;

        if (_maintainAspectRatio)
        {
            double sourceAspect = (double)source.Width / source.Height;
            double targetAspect = (double)width / height;

            if (sourceAspect > targetAspect)
            {
                height = (int)Math.Round(width / sourceAspect);
            }
            else
            {
                width = (int)Math.Round(height * sourceAspect);
            }
        }

        SKImageInfo info = new SKImageInfo(width, height, source.ColorType, source.AlphaType, source.ColorSpace);
        return source.Resize(info, SKFilterQuality.High);
    }
}

