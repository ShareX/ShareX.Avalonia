#region License Information (GPL v3)

/*
    XerahS - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2026 ShareX Team

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
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using ShareX.Avalonia.Platform.Abstractions.Capture;
using SkiaSharp;
using XerahS.Platform.Windows;

namespace ShareX.Avalonia.Platform.Windows.Capture;

/// <summary>
/// Fallback capture strategy using GDI+ Graphics.CopyFromScreen.
/// Simple, reliable, works on all Windows versions.
/// CPU-based, slower than DXGI but universally compatible.
/// </summary>
internal sealed class GdiCaptureStrategy : ICaptureStrategy
{
    public string Name => "GDI+ BitBlt";

    public static bool IsSupported() => true; // Always available on Windows

    public MonitorInfo[] GetMonitors()
    {
        var monitors = WindowsDisplayEnumeration.GetMonitors().Select((monitor, index) =>
        {
            return new MonitorInfo
            {
                Id = monitor.DeviceName,
                Name = monitor.DeviceName.Replace("\\\\.\\", "") ?? $"Monitor {index + 1}",
                IsPrimary = monitor.IsPrimary,
                Bounds = new PhysicalRectangle(
                    monitor.Bounds.X,
                    monitor.Bounds.Y,
                    monitor.Bounds.Width,
                    monitor.Bounds.Height),
                WorkingArea = new PhysicalRectangle(
                    monitor.WorkingArea.X,
                    monitor.WorkingArea.Y,
                    monitor.WorkingArea.Width,
                    monitor.WorkingArea.Height),
                ScaleFactor = monitor.ScaleFactor,
                BitsPerPixel = monitor.BitsPerPixel
            };
        }).ToArray();

        return monitors;
    }

    public Task<CapturedBitmap> CaptureRegionAsync(
        PhysicalRectangle physicalRegion,
        RegionCaptureOptions options)
    {
        return Task.Run(() =>
        {
            // Find monitor containing this region
            var monitors = GetMonitors();
            var monitor = monitors.FirstOrDefault(m => m.Bounds.Intersect(physicalRegion) != null);

            if (monitor == null)
                throw new InvalidOperationException($"Region {physicalRegion} does not intersect any monitor");

            // Create bitmap for the region
            using var bitmap = new Bitmap(
                physicalRegion.Width,
                physicalRegion.Height,
                PixelFormat.Format32bppArgb);

            // Copy screen content to bitmap
            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(
                    physicalRegion.X,
                    physicalRegion.Y,
                    0,
                    0,
                    new Size(physicalRegion.Width, physicalRegion.Height),
                    CopyPixelOperation.SourceCopy);
            }

            // Convert System.Drawing.Bitmap to SKBitmap
            var skBitmap = ConvertToSkBitmap(bitmap);

            return new CapturedBitmap(skBitmap, physicalRegion, monitor.ScaleFactor);
        });
    }

    public BackendCapabilities GetCapabilities()
    {
        return new BackendCapabilities
        {
            BackendName = "GDI+ BitBlt",
            Version = "Win32",
            SupportsHardwareAcceleration = false,
            SupportsCursorCapture = false,
            SupportsHDR = false,
            SupportsPerMonitorDpi = true,
            SupportsMonitorHotplug = true,
            MaxCaptureResolution = 32767, // GDI+ limit
            RequiresPermission = false
        };
    }

    private static SKBitmap ConvertToSkBitmap(Bitmap gdiBitmap)
    {
        // Lock bitmap data for direct pixel access
        var bitmapData = gdiBitmap.LockBits(
            new Rectangle(0, 0, gdiBitmap.Width, gdiBitmap.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        try
        {
            // Create SKBitmap
            var skBitmap = new SKBitmap(
                gdiBitmap.Width,
                gdiBitmap.Height,
                SKColorType.Bgra8888,
                SKAlphaType.Premul);

            // Copy pixel data
            var info = skBitmap.Info;
            var rowBytes = info.RowBytes;

            unsafe
            {
                var src = (byte*)bitmapData.Scan0;
                var dst = (byte*)skBitmap.GetPixels();

                for (int y = 0; y < gdiBitmap.Height; y++)
                {
                    Buffer.MemoryCopy(
                        src + y * bitmapData.Stride,
                        dst + y * rowBytes,
                        rowBytes,
                        Math.Min(bitmapData.Stride, rowBytes));
                }
            }

            return skBitmap;
        }
        finally
        {
            gdiBitmap.UnlockBits(bitmapData);
        }
    }

    public void Dispose()
    {
        // No resources to clean up
    }
}
