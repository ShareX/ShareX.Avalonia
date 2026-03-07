using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class SliceImageEffect : FilterImageEffect
{
    public int MinHeight { get; set; }
    public int MaxHeight { get; set; }
    public int MinShift { get; set; }
    public int MaxShift { get; set; }

    public override string Name => "Slice";

    public SliceImageEffect(int minHeight, int maxHeight, int minShift, int maxShift)
    {
        MinHeight = minHeight;
        MaxHeight = maxHeight;
        MinShift = minShift;
        MaxShift = maxShift;
    }

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (MinHeight <= 0 && MaxHeight <= 0) return source.Copy();

        int minSliceHeight = Math.Max(1, Math.Min(MinHeight, MaxHeight));
        int maxSliceHeight = Math.Max(minSliceHeight, Math.Max(MinHeight, MaxHeight));

        int minSliceShift = Math.Min(Math.Abs(MinShift), Math.Abs(MaxShift));
        int maxSliceShift = Math.Max(Math.Abs(MinShift), Math.Abs(MaxShift));

        var rand = Random.Shared;
        int maxAbsShift = maxSliceShift;
        int newWidth = source.Width + maxAbsShift * 2;

        SKBitmap result = new SKBitmap(newWidth, source.Height);
        using SKCanvas canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        int y = 0;
        while (y < source.Height)
        {
            int sliceHeight = rand.Next(minSliceHeight, maxSliceHeight + 1);
            sliceHeight = Math.Min(sliceHeight, source.Height - y);

            int shift;
            if (rand.Next(2) == 0) // Shift left
            {
                shift = rand.Next(-maxSliceShift, -minSliceShift + 1);
            }
            else // Shift right
            {
                shift = rand.Next(minSliceShift, maxSliceShift + 1);
            }

            SKRect srcRect = new SKRect(0, y, source.Width, y + sliceHeight);
            SKRect dstRect = new SKRect(maxAbsShift + shift, y, maxAbsShift + shift + source.Width, y + sliceHeight);

            canvas.DrawBitmap(source, srcRect, dstRect);

            y += sliceHeight;
        }

        return result;
    }
}

