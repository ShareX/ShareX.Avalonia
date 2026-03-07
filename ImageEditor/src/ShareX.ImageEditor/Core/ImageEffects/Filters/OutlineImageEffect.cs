using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class OutlineImageEffect : FilterImageEffect
{
    public int Size { get; set; }
    public int Padding { get; set; }
    public bool OutlineOnly { get; set; }
    public SKColor Color { get; set; }

    public override string Name => "Outline";

    public OutlineImageEffect(int size, int padding, bool outlineOnly, SKColor color)
    {
        Size = size;
        Padding = padding;
        OutlineOnly = outlineOnly;
        Color = color;
    }

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (Size <= 0) return source.Copy();

        // totalExpand covers outline + gap between image and outline
        int totalExpand = Size + Padding;
        int newWidth = source.Width + totalExpand * 2;
        int newHeight = source.Height + totalExpand * 2;

        SKBitmap result = new SKBitmap(newWidth, newHeight);
        using SKCanvas canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        // Step 1: Dilate by (Size + Padding), then tint → big colored blob covering
        //         both the outline ring AND the gap area.
        using SKImageFilter outerDilate = SKImageFilter.CreateDilate(Size + Padding, Size + Padding);
        using SKImageFilter tintFilter = SKImageFilter.CreateColorFilter(
            SKColorFilter.CreateBlendMode(Color, SKBlendMode.SrcIn));
        using SKImageFilter outlineFilter = SKImageFilter.CreateCompose(tintFilter, outerDilate);
        using SKPaint outlinePaint = new SKPaint { ImageFilter = outlineFilter };
        canvas.DrawBitmap(source, totalExpand, totalExpand, outlinePaint);

        // Step 2: If Padding > 0, erase the inner gap strip (DstOut erases pixels
        //         where the dilated-by-Padding mask is opaque, punching a hole
        //         between the image edge and the start of the outline ring).
        if (Padding > 0)
        {
            using SKImageFilter gapDilate = SKImageFilter.CreateDilate(Padding, Padding);
            using SKPaint erasePaint = new SKPaint
            {
                ImageFilter = gapDilate,
                BlendMode = SKBlendMode.DstOut
            };
            canvas.DrawBitmap(source, totalExpand, totalExpand, erasePaint);
        }

        // Step 3a (Outline Only): punch out the original image's area using DstOut.
        //          This erases the pixels where the image sits, leaving only the ring.
        if (OutlineOnly)
        {
            using SKPaint holeErasePaint = new SKPaint { BlendMode = SKBlendMode.DstOut };
            canvas.DrawBitmap(source, totalExpand, totalExpand, holeErasePaint);
        }
        else
        {
            // Step 3b (Normal): draw the original image on top.
            canvas.DrawBitmap(source, totalExpand, totalExpand);
        }

        return result;
    }
}
