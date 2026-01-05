#region License Information (GPL v3)

/*
    ShareX.Ava - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2025 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using Avalonia;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace ShareX.Ava.UI.Helpers;

/// <summary>
/// Helper methods for converting between Avalonia and SkiaSharp types
/// </summary>
public static class SkiaSharpConversions
{
    /// <summary>
    /// Convert Avalonia Point to SkiaSharp SKPoint
    /// </summary>
    public static SKPoint ToSKPoint(this Point point)
    {
        return new SKPoint((float)point.X, (float)point.Y);
    }

    /// <summary>
    /// Convert SkiaSharp SKPoint to Avalonia Point
    /// </summary>
    public static Point ToAvaloniaPoint(this SKPoint point)
    {
        return new Point(point.X, point.Y);
    }

    /// <summary>
    /// Convert Avalonia Size to SkiaSharp SKSize
    /// </summary>
    public static SKSize ToSKSize(this Size size)
    {
        return new SKSize((float)size.Width, (float)size.Height);
    }

    /// <summary>
    /// Convert SkiaSharp SKSize to Avalonia Size
    /// </summary>
    public static Size ToAvaloniaSize(this SKSize size)
    {
        return new Size(size.Width, size.Height);
    }

    /// <summary>
    /// Convert Avalonia Rect to SkiaSharp SKRect
    /// </summary>
    public static SKRect ToSKRect(this Rect rect)
    {
        return new SKRect((float)rect.Left, (float)rect.Top, (float)rect.Right, (float)rect.Bottom);
    }

    /// <summary>
    /// Convert SkiaSharp SKRect to Avalonia Rect
    /// </summary>
    public static Rect ToAvaloniaRect(this SKRect rect)
    {
        return new Rect(rect.Left, rect.Top, rect.Width, rect.Height);
    }

    /// <summary>
    /// Convert Avalonia Bitmap to SkiaSharp SKBitmap
    /// </summary>
    public static SKBitmap? ToSKBitmap(this Bitmap? bitmap)
    {
        if (bitmap == null) return null;

        using var ms = new MemoryStream();
        bitmap.Save(ms);
        ms.Position = 0;
        return SKBitmap.Decode(ms);
    }

    /// <summary>
    /// Convert SkiaSharp SKBitmap to Avalonia Bitmap
    /// </summary>
    public static Bitmap? ToAvaloniaBitmap(this SKBitmap? skBitmap)
    {
        if (skBitmap == null) return null;

        using var image = SKImage.FromBitmap(skBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var ms = new MemoryStream();
        data.SaveTo(ms);
        ms.Position = 0;
        return new Bitmap(ms);
    }
}
