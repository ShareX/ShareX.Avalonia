using SkiaSharp;

namespace ShareX.ImageEditor.Core.Annotations;

/// <summary>
/// Highlight annotation - translucent color overlay
/// </summary>
public partial class HighlightAnnotation : BaseEffectAnnotation
{
    public HighlightAnnotation()
    {
        ToolType = EditorTool.Highlight;
        StrokeColor = "Transparent";
        FillColor = "#FFFF00"; // Default yellow (opaque for logic, transparency comes from blend)
        StrokeWidth = 0; // No border by default
    }




    public override void UpdateEffect(SKBitmap source)
    {
        if (source == null) return;

        var rect = GetBounds();
        var fullW = (int)rect.Width;
        var fullH = (int)rect.Height;

        if (fullW <= 0 || fullH <= 0) return;

        // Logical intersection with image
        var skRect = new SKRectI((int)rect.Left, (int)rect.Top, (int)rect.Right, (int)rect.Bottom);
        var intersect = skRect;
        intersect.Intersect(new SKRectI(0, 0, source.Width, source.Height));

        // Create the FULL size bitmap (matching rect)
        var result = new SKBitmap(fullW, fullH);
        result.Erase(SKColors.Transparent);

        // If specific intersection exists, process it
        if (intersect.Width > 0 && intersect.Height > 0)
        {
            // Extract the valid region to highlight
            using var crop = new SKBitmap(intersect.Width, intersect.Height);
            source.ExtractSubset(crop, intersect);

            // We will modify a copy of the crop (so we don't affect source)
            using var processedCrop = crop.Copy();

            var highlightColor = ParseColor(FillColor ?? "#FFFF00");
            byte r = highlightColor.Red;
            byte g = highlightColor.Green;
            byte b = highlightColor.Blue;

            unsafe
            {
                // Lock pixels for direct access
                var pixels = (byte*)processedCrop.GetPixels();
                int count = processedCrop.Width * processedCrop.Height;
                var colorType = processedCrop.ColorType;

                // Handle standard formats using channel-independent Min logic
                if (colorType == SKColorType.Bgra8888 || colorType == SKColorType.Rgba8888)
                {
                    // For both BGRA and RGBA, the channels are just bytes. 
                    // Since specific channel order implies valid pointers, we can optimize:
                    // We iterate all bytes 4 at a time.
                    // For BGRA: [0]=B, [1]=G, [2]=R.
                    // For RGBA: [0]=R, [1]=G, [2]=B.
                    // To do this correctly without conditional loop:

                    if (colorType == SKColorType.Bgra8888)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            int offset = i * 4;
                            pixels[offset] = Math.Min(pixels[offset], b);       // Blue
                            pixels[offset + 1] = Math.Min(pixels[offset + 1], g); // Green
                            pixels[offset + 2] = Math.Min(pixels[offset + 2], r); // Red
                        }
                    }
                    else
                    {
                        for (int i = 0; i < count; i++)
                        {
                            int offset = i * 4;
                            pixels[offset] = Math.Min(pixels[offset], r);       // Red
                            pixels[offset + 1] = Math.Min(pixels[offset + 1], g); // Green
                            pixels[offset + 2] = Math.Min(pixels[offset + 2], b); // Blue
                        }
                    }
                }
                else
                {
                    // Fallback using safe GetPixel/SetPixel
                    for (int x = 0; x < processedCrop.Width; x++)
                    {
                        for (int y = 0; y < processedCrop.Height; y++)
                        {
                            var c = processedCrop.GetPixel(x, y);
                            var newC = new SKColor(
                                Math.Min(c.Red, r),
                                Math.Min(c.Green, g),
                                Math.Min(c.Blue, b),
                                c.Alpha);
                            processedCrop.SetPixel(x, y, newC);
                        }
                    }
                }
            }

            // Draw the processed crop into the result at the correct offset
            // Offset is difference between intersection left/top and annotation left/top
            int dx = intersect.Left - skRect.Left;
            int dy = intersect.Top - skRect.Top;

            using (var canvas = new SKCanvas(result))
            {
                canvas.DrawBitmap(processedCrop, dx, dy);
            }
        }

        EffectBitmap?.Dispose();
        EffectBitmap = result;
    }
}
