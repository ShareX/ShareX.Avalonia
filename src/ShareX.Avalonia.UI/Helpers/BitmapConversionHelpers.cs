using Avalonia.Media.Imaging;
using SkiaSharp;
using System;
using System.IO;

namespace ShareX.Avalonia.UI.Helpers
{
    /// <summary>
    /// Helper class for converting between Avalonia Bitmap and SKBitmap
    /// </summary>
    public static class BitmapConversionHelpers
    {
        /// <summary>
        /// Convert Avalonia Bitmap to SKBitmap
        /// </summary>
        public static SKBitmap ToSKBitmap(Bitmap avaloniaBitmap)
        {
            if (avaloniaBitmap == null)
                throw new ArgumentNullException(nameof(avaloniaBitmap));

            using var memoryStream = new MemoryStream();
            avaloniaBitmap.Save(memoryStream);
            memoryStream.Position = 0;

            return SKBitmap.Decode(memoryStream);
        }

        /// <summary>
        /// Convert SKBitmap to Avalonia Bitmap
        /// </summary>
        public static Bitmap ToAvaloniBitmap(SKBitmap skBitmap)
        {
            if (skBitmap == null)
                throw new ArgumentNullException(nameof(skBitmap));

            using var image = SKImage.FromBitmap(skBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var memoryStream = new MemoryStream();
            
            data.SaveTo(memoryStream);
            memoryStream.Position = 0;

            return new Bitmap(memoryStream);
        }
    }
}
