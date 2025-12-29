#region License Information (GPL v3)

/*
    ShareX.Avalonia - The Avalonia UI implementation of ShareX
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

using ShareX.Avalonia.Common;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ShareX.Avalonia.ImageEffects.Helpers
{
    public static class ImageEffectsProcessing
    {
        public static Bitmap BoxBlur(Bitmap bmp, int radius)
        {
            radius = Math.Max(1, radius);
            Bitmap result = (Bitmap)bmp.Clone();

            int diameter = (radius * 2) + 1;

            using (UnsafeBitmap source = new UnsafeBitmap(bmp, true, ImageLockMode.ReadOnly))
            using (UnsafeBitmap dest = new UnsafeBitmap(result, true, ImageLockMode.WriteOnly))
            {
                for (int y = 0; y < source.Height; y++)
                {
                    for (int x = 0; x < source.Width; x++)
                    {
                        int r = 0, g = 0, b = 0, a = 0, count = 0;

                        for (int ky = -radius; ky <= radius; ky++)
                        {
                            int py = (y + ky).Clamp(0, source.Height - 1);

                            for (int kx = -radius; kx <= radius; kx++)
                            {
                                int px = (x + kx).Clamp(0, source.Width - 1);
                                ColorBgra c = source.GetPixel(px, py);
                                r += c.Red;
                                g += c.Green;
                                b += c.Blue;
                                a += c.Alpha;
                                count++;
                            }
                        }

                        byte rb = (byte)(r / count);
                        byte gb = (byte)(g / count);
                        byte bb = (byte)(b / count);
                        byte ab = (byte)(a / count);

                        dest.SetPixel(x, y, new ColorBgra(bb, gb, rb, ab));
                    }
                }
            }

            return result;
        }

        public static Bitmap CropBitmap(Bitmap bmp, Rectangle rect)
        {
            Rectangle bounds = new Rectangle(0, 0, bmp.Width, bmp.Height);
            if (rect.Width <= 0 || rect.Height <= 0 || !bounds.Contains(rect))
            {
                return bmp;
            }

            return bmp.Clone(rect, PixelFormat.Format32bppArgb);
        }

        public static Bitmap GaussianBlur(Bitmap bmp, int radius)
        {
            radius = Math.Max(1, radius);
            int size = (radius * 2) + 1;
            double sigma = Math.Max(1.0, radius / 3.0);
            var kernel = ConvolutionMatrixManager.GaussianBlur(size, size, sigma);
            return kernel.Apply(bmp);
        }

        public static void ColorDepth(Bitmap bmp, int bitsPerChannel)
        {
            bitsPerChannel = MathHelpers.Clamp(bitsPerChannel, 1, 8);
            int levels = 1 << bitsPerChannel;
            float step = 255f / (levels - 1);

            using (UnsafeBitmap unsafeBitmap = new UnsafeBitmap(bmp, true, ImageLockMode.ReadWrite))
            {
                for (int i = 0; i < unsafeBitmap.PixelCount; i++)
                {
                    ColorBgra color = unsafeBitmap.GetPixel(i);
                    color.Red = Quantize(color.Red);
                    color.Green = Quantize(color.Green);
                    color.Blue = Quantize(color.Blue);
                    unsafeBitmap.SetPixel(i, color);
                }
            }

            byte Quantize(byte value)
            {
                int level = (int)Math.Round(value / step);
                return (byte)MathHelpers.Clamp((int)Math.Round(level * step), 0, 255);
            }
        }

        public static Bitmap Pixelate(Bitmap bmp, int size, int borderSize = 0, Color? borderColor = null)
        {
            size = Math.Max(1, size);
            Bitmap result = (Bitmap)bmp.Clone();

            using (UnsafeBitmap source = new UnsafeBitmap(bmp, true, ImageLockMode.ReadOnly))
            using (UnsafeBitmap dest = new UnsafeBitmap(result, true, ImageLockMode.WriteOnly))
            {
                for (int y = 0; y < source.Height; y += size)
                {
                    for (int x = 0; x < source.Width; x += size)
                    {
                        int maxX = Math.Min(x + size, source.Width);
                        int maxY = Math.Min(y + size, source.Height);
                        int r = 0, g = 0, b = 0, a = 0, count = 0;

                        for (int yy = y; yy < maxY; yy++)
                        {
                            for (int xx = x; xx < maxX; xx++)
                            {
                                ColorBgra c = source.GetPixel(xx, yy);
                                r += c.Red;
                                g += c.Green;
                                b += c.Blue;
                                a += c.Alpha;
                                count++;
                            }
                        }

                        byte rb = (byte)(r / count);
                        byte gb = (byte)(g / count);
                        byte bb = (byte)(b / count);
                        byte ab = (byte)(a / count);
                        ColorBgra avg = new ColorBgra(bb, gb, rb, ab);

                        for (int yy = y; yy < maxY; yy++)
                        {
                            for (int xx = x; xx < maxX; xx++)
                            {
                                dest.SetPixel(xx, yy, avg);
                            }
                        }
                    }
                }
            }

            if (borderSize > 0 && borderColor.HasValue)
            {
                using (Graphics g = Graphics.FromImage(result))
                using (Pen pen = new Pen(borderColor.Value, borderSize) { Alignment = PenAlignment.Inset })
                {
                    g.DrawRectangle(pen, 0, 0, result.Width, result.Height);
                }
            }

            return result;
        }

        public static Bitmap AddCanvas(Image img, CanvasMargin margin, Color canvasColor)
        {
            if (margin.Horizontal == 0 && margin.Vertical == 0)
            {
                return null;
            }

            int width = img.Width + margin.Horizontal;
            int height = img.Height + margin.Vertical;

            if (width < 1 || height < 1)
            {
                return null;
            }

            Bitmap bmp = img.CreateEmptyBitmap(margin.Horizontal, margin.Vertical);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImage(img, margin.Left, margin.Top, img.Width, img.Height);

                if (canvasColor.A > 0)
                {
                    g.CompositingMode = CompositingMode.SourceCopy;

                    using (Brush brush = new SolidBrush(canvasColor))
                    {
                        if (margin.Left > 0)
                        {
                            g.FillRectangle(brush, 0, 0, margin.Left, bmp.Height);
                        }

                        if (margin.Top > 0)
                        {
                            g.FillRectangle(brush, 0, 0, bmp.Width, margin.Top);
                        }

                        if (margin.Right > 0)
                        {
                            g.FillRectangle(brush, bmp.Width - margin.Right, 0, margin.Right, bmp.Height);
                        }

                        if (margin.Bottom > 0)
                        {
                            g.FillRectangle(brush, 0, bmp.Height - margin.Bottom, bmp.Width, margin.Bottom);
                        }
                    }
                }
            }

            return bmp;
        }

        [Flags]
        public enum AnchorSides
        {
            None = 0,
            Left = 1,
            Top = 2,
            Right = 4,
            Bottom = 8,
            All = Left | Top | Right | Bottom
        }

        public static Rectangle FindAutoCropRectangle(Bitmap bmp, bool sameColorCrop = false, AnchorSides sides = AnchorSides.All)
        {
            Rectangle source = new Rectangle(0, 0, bmp.Width, bmp.Height);

            if (sides == AnchorSides.None)
            {
                return source;
            }

            Rectangle crop = source;

            using (UnsafeBitmap unsafeBitmap = new UnsafeBitmap(bmp, true, ImageLockMode.ReadOnly))
            {
                bool leave = false;

                ColorBgra checkColor = unsafeBitmap.GetPixel(0, 0);
                uint mask = checkColor.Alpha == 0 ? 0xFF000000 : 0xFFFFFFFF;
                uint check = checkColor.Bgra & mask;

                if (sides.HasFlag(AnchorSides.Left))
                {
                    for (int x = 0; x < bmp.Width && !leave; x++)
                    {
                        for (int y = 0; y < bmp.Height; y++)
                        {
                            if ((unsafeBitmap.GetPixel(x, y).Bgra & mask) != check)
                            {
                                crop.X = x;
                                crop.Width -= x;
                                leave = true;
                                break;
                            }
                        }
                    }

                    if (!leave)
                    {
                        return crop;
                    }

                    leave = false;
                }

                if (sides.HasFlag(AnchorSides.Top))
                {
                    for (int y = 0; y < bmp.Height && !leave; y++)
                    {
                        for (int x = 0; x < bmp.Width; x++)
                        {
                            if ((unsafeBitmap.GetPixel(x, y).Bgra & mask) != check)
                            {
                                crop.Y = y;
                                crop.Height -= y;
                                leave = true;
                                break;
                            }
                        }
                    }
                }

                if (!sameColorCrop)
                {
                    checkColor = unsafeBitmap.GetPixel(bmp.Width - 1, bmp.Height - 1);
                    mask = checkColor.Alpha == 0 ? 0xFF000000 : 0xFFFFFFFF;
                    check = checkColor.Bgra & mask;
                }

                if (sides.HasFlag(AnchorSides.Right))
                {
                    leave = false;
                    for (int x = bmp.Width - 1; x >= 0 && !leave; x--)
                    {
                        for (int y = 0; y < bmp.Height; y++)
                        {
                            if ((unsafeBitmap.GetPixel(x, y).Bgra & mask) != check)
                            {
                                crop.Width = x - crop.X + 1;
                                leave = true;
                                break;
                            }
                        }
                    }
                }

                if (sides.HasFlag(AnchorSides.Bottom))
                {
                    leave = false;
                    for (int y = bmp.Height - 1; y >= 0 && !leave; y--)
                    {
                        for (int x = 0; x < bmp.Width; x++)
                        {
                            if ((unsafeBitmap.GetPixel(x, y).Bgra & mask) != check)
                            {
                                crop.Height = y - crop.Y + 1;
                                leave = true;
                                break;
                            }
                        }
                    }
                }
            }

            return crop;
        }

        public static Bitmap AutoCropImage(Bitmap bmp, bool sameColorCrop, AnchorSides sides, int padding)
        {
            Rectangle source = new Rectangle(0, 0, bmp.Width, bmp.Height);
            Rectangle crop = FindAutoCropRectangle(bmp, sameColorCrop, sides);

            if (source == crop)
            {
                return bmp;
            }

            Bitmap croppedBitmap = CropBitmap(bmp, crop);

            if (croppedBitmap == null)
            {
                return bmp;
            }

            using (bmp)
            {
                if (padding > 0)
                {
                    using (croppedBitmap)
                    {
                        Color color = bmp.GetPixel(0, 0);
                        Bitmap padded = AddCanvas(croppedBitmap, new CanvasMargin(padding), color);
                        return padded ?? bmp;
                    }
                }

                return croppedBitmap;
            }
        }

        public static Bitmap RotateImage(Bitmap bmp, float angle, bool upsize, bool clip)
        {
            if (angle == 0)
            {
                return bmp;
            }

            float rad = angle * (float)(Math.PI / 180.0);
            float cos = Math.Abs((float)Math.Cos(rad));
            float sin = Math.Abs((float)Math.Sin(rad));

            int newWidth = bmp.Width;
            int newHeight = bmp.Height;

            if (upsize)
            {
                newWidth = (int)Math.Ceiling((bmp.Width * cos) + (bmp.Height * sin));
                newHeight = (int)Math.Ceiling((bmp.Width * sin) + (bmp.Height * cos));
            }

            Bitmap rotated = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);
            rotated.SetResolution(bmp.HorizontalResolution, bmp.VerticalResolution);

            using (Graphics g = Graphics.FromImage(rotated))
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;

                g.TranslateTransform(newWidth / 2f, newHeight / 2f);
                g.RotateTransform(angle);
                g.TranslateTransform(-bmp.Width / 2f, -bmp.Height / 2f);

                Rectangle destRect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                g.DrawImage(bmp, destRect, new Rectangle(0, 0, bmp.Width, bmp.Height), GraphicsUnit.Pixel);
            }

            if (!upsize && clip)
            {
                int x = (rotated.Width - bmp.Width) / 2;
                int y = (rotated.Height - bmp.Height) / 2;
                Rectangle crop = new Rectangle(Math.Max(0, x), Math.Max(0, y), Math.Min(bmp.Width, rotated.Width), Math.Min(bmp.Height, rotated.Height));
                Bitmap clipped = rotated.Clone(crop, PixelFormat.Format32bppArgb);
                rotated.Dispose();
                return clipped;
            }

            return rotated;
        }

        public static Bitmap AddSkew(Image img, int x, int y)
        {
            Bitmap result = img.CreateEmptyBitmap(Math.Abs(x), Math.Abs(y));

            using (img)
            using (Graphics g = Graphics.FromImage(result))
            {
                g.SetHighQuality();
                int startX = -Math.Min(0, x);
                int startY = -Math.Min(0, y);
                int endX = Math.Max(0, x);
                int endY = Math.Max(0, y);
                System.Drawing.Point[] destinationPoints =
                {
                    new System.Drawing.Point(startX, startY),
                    new System.Drawing.Point(startX + img.Width - 1, endY),
                    new System.Drawing.Point(endX, startY + img.Height - 1)
                };
                g.DrawImage(img, destinationPoints);
            }

            return result;
        }

        public static Bitmap ResizeImage(Bitmap bmp, Size size)
        {
            if (size.Width < 1 || size.Height < 1 || (bmp.Width == size.Width && bmp.Height == size.Height))
            {
                return bmp;
            }

            Bitmap bmpResult = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            bmpResult.SetResolution(bmp.HorizontalResolution, bmp.VerticalResolution);

            using (Graphics g = Graphics.FromImage(bmpResult))
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;

                using (ImageAttributes ia = new ImageAttributes())
                {
                    ia.SetWrapMode(WrapMode.TileFlipXY);
                    g.DrawImage(bmp, new Rectangle(0, 0, size.Width, size.Height), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, ia);
                }
            }

            return bmpResult;
        }

        public static Bitmap ResizeImage(Bitmap bmp, int width, int height, bool allowEnlarge, bool centerImage, Color backColor)
        {
            if (!allowEnlarge && bmp.Width <= width && bmp.Height <= height)
            {
                return bmp;
            }

            double ratioX = (double)width / bmp.Width;
            double ratioY = (double)height / bmp.Height;
            double ratio = Math.Min(ratioX, ratioY);
            int newWidth = (int)(bmp.Width * ratio);
            int newHeight = (int)(bmp.Height * ratio);

            int offsetX = centerImage ? (width - newWidth) / 2 : 0;
            int offsetY = centerImage ? (height - newHeight) / 2 : 0;

            Bitmap bmpResult = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            bmpResult.SetResolution(bmp.HorizontalResolution, bmp.VerticalResolution);

            using (Graphics g = Graphics.FromImage(bmpResult))
            {
                if (backColor.A > 0)
                {
                    g.Clear(backColor);
                }

                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;

                g.DrawImage(bmp, offsetX, offsetY, newWidth, newHeight);
            }

            return bmpResult;
        }

        public static Bitmap RoundedCorners(Bitmap bmp, int cornerRadius)
        {
            Bitmap bmpResult = bmp.CreateEmptyBitmap();

            using (bmp)
            using (Graphics g = Graphics.FromImage(bmpResult))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                using (GraphicsPath gp = new GraphicsPath())
                {
                    AddRoundedRectangle(gp, new RectangleF(0, 0, bmp.Width, bmp.Height), cornerRadius);

                    using (TextureBrush brush = new TextureBrush(bmp))
                    {
                        g.FillPath(brush, gp);
                    }
                }
            }

            return bmpResult;
        }

        public static Size ApplyAspectRatio(int width, int height, Bitmap bmp)
        {
            int newWidth;
            int newHeight;

            if (width == 0 && height == 0)
            {
                return new Size(bmp.Width, bmp.Height);
            }
            else if (width == 0)
            {
                newWidth = (int)Math.Round((float)height / bmp.Height * bmp.Width);
                newHeight = height;
            }
            else if (height == 0)
            {
                newWidth = width;
                newHeight = (int)Math.Round((float)width / bmp.Width * bmp.Height);
            }
            else
            {
                newWidth = width;
                newHeight = height;
            }

            return new Size(newWidth, newHeight);
        }

        private static void AddRoundedRectangle(GraphicsPath graphicsPath, RectangleF rect, float radius)
        {
            if (radius <= 0f)
            {
                graphicsPath.AddRectangle(rect);
                return;
            }

            if (radius >= Math.Min(rect.Width, rect.Height) / 2f)
            {
                graphicsPath.AddEllipse(rect);
                return;
            }

            float diameter = radius * 2f;
            SizeF size = new SizeF(diameter, diameter);
            RectangleF arc = new RectangleF(rect.Location, size);

            graphicsPath.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            graphicsPath.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            graphicsPath.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            graphicsPath.AddArc(arc, 90, 90);
            graphicsPath.CloseFigure();
        }
    }
}
