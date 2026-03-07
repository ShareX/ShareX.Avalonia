using SkiaSharp;

namespace ShareX.ImageEditor.Core.Annotations;

/// <summary>
/// Image annotation - stickers or inserted images
/// </summary>
public class ImageAnnotation : Annotation, IDisposable
{
    public override AnnotationCategory Category => AnnotationCategory.Shapes;
    private SKBitmap? _imageBitmap;

    /// <summary>
    /// File path to the image (if external)
    /// </summary>
    public string ImagePath { get; set; } = "";

    /// <summary>
    /// The loaded image bitmap
    /// </summary>
    public SKBitmap? ImageBitmap => _imageBitmap;

    public ImageAnnotation()
    {
        ToolType = EditorTool.Image;
        StrokeWidth = 0; // Usually no border
    }

    public void LoadImage(string path)
    {
        if (File.Exists(path))
        {
            try
            {
                ImagePath = path;
                _imageBitmap?.Dispose();
                _imageBitmap = SKBitmap.Decode(path);
            }
            catch { }
        }
    }

    public void SetImage(SKBitmap bitmap)
    {
        _imageBitmap?.Dispose();
        _imageBitmap = bitmap;
    }



    public override bool HitTest(SKPoint point, float tolerance = 5)
    {
        var bounds = GetBounds();
        var inflated = SKRect.Inflate(bounds, tolerance, tolerance);
        return inflated.Contains(point);
    }

    /// <summary>
    /// Dispose unmanaged resources (ImageBitmap)
    /// </summary>
    public void Dispose()
    {
        _imageBitmap?.Dispose();
        _imageBitmap = null;
        GC.SuppressFinalize(this);
    }

    public override Annotation Clone()
    {
        var clone = (ImageAnnotation)base.Clone();
        // Deep-copy bitmap for undo/redo to properly preserve image data
        clone._imageBitmap = _imageBitmap?.Copy();
        return clone;
    }
}
