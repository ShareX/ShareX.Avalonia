using Avalonia.Media.Imaging;
using SkiaSharp;

namespace ShareX.ImageEditor.Presentation.Rendering
{
    /// <summary>
    /// Helper class for converting between Avalonia Bitmap and SKBitmap
    /// </summary>
    /// <summary>
    /// Helper class for converting between Avalonia Bitmap and SKBitmap
    /// </summary>
    public static class BitmapConversionHelpers
    {
        /// <summary>
        /// Convert Avalonia Bitmap to SKBitmap.
        /// Warning: This is expensive if the input is not a WriteableBitmap.
        /// </summary>
        public static SKBitmap ToSKBitmap(Bitmap avaloniaBitmap)
        {
            if (avaloniaBitmap == null)
                throw new ArgumentNullException(nameof(avaloniaBitmap));

            // Optimized path for WriteableBitmap
            if (avaloniaBitmap is WriteableBitmap writeableBitmap)
            {
                using (var locked = writeableBitmap.Lock())
                {
                    var info = new SKImageInfo(
                        (int)avaloniaBitmap.Size.Width,
                        (int)avaloniaBitmap.Size.Height,
                        SKColorType.Bgra8888, // Avalonia usually uses BGRA
                        SKAlphaType.Premul);

                    var skBitmap = new SKBitmap(info);
                    unsafe
                    {
                        var srcPtr = locked.Address;
                        var dstPtr = skBitmap.GetPixels();

                        // Copy row by row to handle stride differences if any
                        var height = info.Height;
                        var width = info.Width;
                        var srcStride = locked.RowBytes;
                        var dstStride = skBitmap.RowBytes;
                        var bytesPerRow = Math.Min(srcStride, dstStride); // Safe copy width

                        if (srcStride == dstStride)
                        {
                            // Full block copy if strides match (most common)
                            long totalBytes = (long)height * srcStride;
                            Buffer.MemoryCopy((void*)srcPtr, (void*)dstPtr, totalBytes, totalBytes);
                        }
                        else
                        {
                            for (int y = 0; y < height; y++)
                            {
                                var srcRow = (byte*)srcPtr + (y * srcStride);
                                var dstRow = (byte*)dstPtr + (y * dstStride);
                                Buffer.MemoryCopy(srcRow, dstRow, bytesPerRow, bytesPerRow);
                            }
                        }
                    }
                    return skBitmap;
                }
            }

            // Fallback for regular Bitmap (e.g. from file) - unfortunately still streams or saving to temporary,
            // as Avalonia Bitmap doesn't expose easy pixel access.
            // However, we can use a temporary WriteableBitmap to draw it, then use the fast path? 
            // Actually, stream is likely safer/easier for generic Bitmap type if we don't want to rely on WriteableBitmap internals.
            using var memoryStream = new MemoryStream();
            avaloniaBitmap.Save(memoryStream);
            memoryStream.Position = 0;

            return SKBitmap.Decode(memoryStream);
        }

        /// <summary>
        /// Convert SKBitmap to Avalonia Bitmap using WriteableBitmap for high performance.
        /// </summary>
        public static Bitmap ToAvaloniBitmap(SKBitmap skBitmap)
        {
            if (skBitmap == null)
                throw new ArgumentNullException(nameof(skBitmap));

            // Ensure we are in a compatible format for Avalonia (BGRA8888 is standard)
            // If not, we might need to convert. 
            // Avalonia WriteableBitmap usually expects Bgra8888 or Rgba8888 depending on platform, but Bgr8888 is safest default.

            var width = skBitmap.Width;
            var height = skBitmap.Height;

            // Create WriteableBitmap
            var writeableBitmap = new WriteableBitmap(
                new Avalonia.PixelSize(width, height),
                new Avalonia.Vector(96, 96), // DPI
                Avalonia.Platform.PixelFormat.Bgra8888,
                Avalonia.Platform.AlphaFormat.Premul);

            using (var locked = writeableBitmap.Lock())
            {
                var info = skBitmap.Info;

                // If the source is already Bgra8888, we can copy directly
                if (info.ColorType == SKColorType.Bgra8888)
                {
                    unsafe
                    {
                        var srcPtr = skBitmap.GetPixels();
                        var dstPtr = locked.Address;
                        var srcStride = skBitmap.RowBytes;
                        var dstStride = locked.RowBytes;
                        var copyWidth = Math.Min(srcStride, dstStride);

                        if (srcStride == dstStride)
                        {
                            long totalBytes = (long)height * srcStride;
                            Buffer.MemoryCopy((void*)srcPtr, (void*)dstPtr, totalBytes, totalBytes);
                        }
                        else
                        {
                            for (int y = 0; y < height; y++)
                            {
                                var srcRow = (byte*)srcPtr + (y * srcStride);
                                var dstRow = (byte*)dstPtr + (y * dstStride);
                                Buffer.MemoryCopy(srcRow, dstRow, copyWidth, copyWidth);
                            }
                        }
                    }
                }
                else
                {
                    // If ColorType differs, let Skia handle the pixel conversion into the destination buffer
                    var dstInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

                    // We can read pixels directly into the WriteableBitmap's buffer
                    // We can read pixels directly into the WriteableBitmap's buffer
                    // using var pixmap = skBitmap.PeekPixels();
                    // pixmap?.ReadPixels(dstInfo, locked.Address, locked.RowBytes, 0, 0);
                    // Or safer if PeekPixels returns null? It shouldn't if we have a bitmap.
                    // But to be safe and fix the compile error:
                    var pixmap = skBitmap.PeekPixels();
                    if (pixmap != null)
                    {
                        pixmap.ReadPixels(dstInfo, locked.Address, locked.RowBytes, 0, 0);
                    }
                    else
                    {
                        // Fallback? Or use GetPixels?
                        // If PeekPixels is null, maybe pixels aren't allocated.
                        // But we are converting valid bitmap.
                        // Force allocation/lock?
                        IntPtr ptr = skBitmap.GetPixels(); // Forces pixel lock
                        skBitmap.PeekPixels()?.ReadPixels(dstInfo, locked.Address, locked.RowBytes, 0, 0);
                    }
                }
            }

            return writeableBitmap;
        }
    }
}
