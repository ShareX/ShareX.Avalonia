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
using ShareX.Avalonia.ImageEffects.Helpers;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using DrawingPoint = System.Drawing.Point;
using DrawingInterpolationMode = System.Drawing.Drawing2D.InterpolationMode;

namespace ShareX.Avalonia.ImageEffects.Drawings
{
    [Description("Image")]
    public class DrawImage : ImageEffect
    {
        [DefaultValue("")]
        public string ImageLocation { get; set; } = string.Empty;

        [DefaultValue(ContentAlignment.TopLeft)]
        public ContentAlignment Placement { get; set; }

        [DefaultValue(typeof(DrawingPoint), "0, 0")]
        public DrawingPoint Offset { get; set; }

        [DefaultValue(DrawImageSizeMode.DontResize), Description("How the image watermark should be rescaled, if at all.")]
        public DrawImageSizeMode SizeMode { get; set; }

        [DefaultValue(typeof(Size), "0, 0")]
        public Size Size { get; set; }

        [DefaultValue(ImageRotateFlipType.None)]
        public ImageRotateFlipType RotateFlip { get; set; }

        [DefaultValue(false)]
        public bool Tile { get; set; }

        [DefaultValue(false), Description("If image watermark size bigger than source image then don't draw it.")]
        public bool AutoHide { get; set; }

        [DefaultValue(ImageInterpolationMode.HighQualityBicubic)]
        public ImageInterpolationMode InterpolationMode { get; set; } = ImageInterpolationMode.HighQualityBicubic;

        [DefaultValue(CompositingMode.SourceOver)]
        public CompositingMode CompositingMode { get; set; } = CompositingMode.SourceOver;

        private int opacity = 100;

        [DefaultValue(100)]
        public int Opacity
        {
            get => opacity;
            set => opacity = Math.Clamp(value, 0, 100);
        }

        public override Bitmap Apply(Bitmap bmp)
        {
            if (bmp == null || Opacity < 1)
            {
                return bmp;
            }

            if (SizeMode != DrawImageSizeMode.DontResize && Size.Width <= 0 && Size.Height <= 0)
            {
                return bmp;
            }

            string imageFilePath = ResolveImagePath(ImageLocation);

            if (string.IsNullOrEmpty(imageFilePath) || !File.Exists(imageFilePath))
            {
                return bmp;
            }

            using Bitmap watermark = new Bitmap(imageFilePath);

            if (RotateFlip != ImageRotateFlipType.None)
            {
                watermark.RotateFlip(MapRotateFlip(RotateFlip));
            }

            Size imageSize = CalculateSize(SizeMode, Size, watermark, bmp);
            DrawingPoint imagePosition = WatermarkHelpers.GetPosition(Placement, Offset, bmp.Size, imageSize);
            Rectangle imageRectangle = new Rectangle(imagePosition, imageSize);

            if (AutoHide && !new Rectangle(DrawingPoint.Empty, bmp.Size).Contains(imageRectangle))
            {
                return bmp;
            }

            using Graphics g = Graphics.FromImage(bmp);
            g.InterpolationMode = MapInterpolation(InterpolationMode);
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.CompositingMode = CompositingMode;

            if (Tile)
            {
                using TextureBrush brush = new TextureBrush(watermark, WrapMode.Tile);
                brush.TranslateTransform(imageRectangle.X, imageRectangle.Y);
                g.FillRectangle(brush, imageRectangle);
            }
            else if (Opacity < 100)
            {
                using ImageAttributes ia = new ImageAttributes();
                ColorMatrix matrix = ColorMatrixManager.Alpha(Opacity / 100f);
                ia.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                g.DrawImage(watermark, imageRectangle, 0, 0, watermark.Width, watermark.Height, GraphicsUnit.Pixel, ia);
            }
            else
            {
                g.DrawImage(watermark, imageRectangle);
            }

            return bmp;
        }

        private static RotateFlipType MapRotateFlip(ImageRotateFlipType type)
        {
            return type switch
            {
                ImageRotateFlipType.Rotate90 => RotateFlipType.Rotate90FlipNone,
                ImageRotateFlipType.Rotate180 => RotateFlipType.Rotate180FlipNone,
                ImageRotateFlipType.Rotate270 => RotateFlipType.Rotate270FlipNone,
                ImageRotateFlipType.FlipX => RotateFlipType.RotateNoneFlipX,
                ImageRotateFlipType.Rotate90FlipX => RotateFlipType.Rotate90FlipX,
                ImageRotateFlipType.FlipY => RotateFlipType.RotateNoneFlipY,
                ImageRotateFlipType.Rotate90FlipY => RotateFlipType.Rotate90FlipY,
                _ => RotateFlipType.RotateNoneFlipNone
            };
        }

        private static DrawingInterpolationMode MapInterpolation(ImageInterpolationMode mode)
        {
            return mode switch
            {
                ImageInterpolationMode.Bicubic => DrawingInterpolationMode.Bicubic,
                ImageInterpolationMode.HighQualityBilinear => DrawingInterpolationMode.HighQualityBilinear,
                ImageInterpolationMode.Bilinear => DrawingInterpolationMode.Bilinear,
                ImageInterpolationMode.NearestNeighbor => DrawingInterpolationMode.NearestNeighbor,
                _ => DrawingInterpolationMode.HighQualityBicubic
            };
        }

        private static Size CalculateSize(DrawImageSizeMode sizeMode, Size requestedSize, Bitmap watermark, Bitmap canvas)
        {
            if (sizeMode == DrawImageSizeMode.AbsoluteSize)
            {
                int width = requestedSize.Width == -1 ? canvas.Width : requestedSize.Width;
                int height = requestedSize.Height == -1 ? canvas.Height : requestedSize.Height;
                return ImageEffectsProcessing.ApplyAspectRatio(width, height, watermark);
            }

            if (sizeMode == DrawImageSizeMode.PercentageOfWatermark)
            {
                int width = (int)Math.Round(requestedSize.Width / 100f * watermark.Width);
                int height = (int)Math.Round(requestedSize.Height / 100f * watermark.Height);
                return ImageEffectsProcessing.ApplyAspectRatio(width, height, watermark);
            }

            if (sizeMode == DrawImageSizeMode.PercentageOfCanvas)
            {
                int width = (int)Math.Round(requestedSize.Width / 100f * canvas.Width);
                int height = (int)Math.Round(requestedSize.Height / 100f * canvas.Height);
                return ImageEffectsProcessing.ApplyAspectRatio(width, height, watermark);
            }

            return watermark.Size;
        }

        private static string ResolveImagePath(string? location)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                return string.Empty;
            }

            string expanded = Environment.ExpandEnvironmentVariables(location).Trim();

            try
            {
                if (!Path.IsPathRooted(expanded))
                {
                    expanded = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expanded));
                }
                else
                {
                    expanded = Path.GetFullPath(expanded);
                }
            }
            catch
            {
                return string.Empty;
            }

            return expanded;
        }

        protected override string? GetSummary()
        {
            if (!string.IsNullOrEmpty(ImageLocation))
            {
                return Path.GetFileName(ImageLocation);
            }

            return null;
        }
    }
}
