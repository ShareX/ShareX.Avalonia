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

using XerahS.Platform.Abstractions;
using SkiaSharp;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using System.Runtime.InteropServices;

namespace XerahS.Platform.Windows
{
    /// <summary>
    /// Modern Windows screen capture using Direct3D11 and DXGI Output Duplication.
    /// Falls back to GDI+ on older Windows versions.
    /// </summary>
#pragma warning disable CA1416 // Platform compatibility - we do runtime checks via IsSupported
    public class WindowsModernCaptureService : IScreenCaptureService
    {
        private readonly IScreenService _screenService;
        private readonly WindowsScreenCaptureService _fallbackService;

        [DllImport("user32.dll")]
        private static extern bool SetSystemCursor(IntPtr hcur, uint id);
        [DllImport("user32.dll")]
        private static extern IntPtr CopyIcon(IntPtr hIcon);
        [DllImport("user32.dll")]
        private static extern IntPtr CreateCursor(IntPtr hInst, int xHotSpot, int yHotSpot,
            int nWidth, int nHeight, byte[] pvANDPlane, byte[] pvXORPlane);
        [DllImport("user32.dll")]
        private static extern bool DestroyCursor(IntPtr hCursor);
        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        private const uint SPI_SETCURSORS = 0x0057;
        private static readonly uint[] AllCursorIds =
        {
            32512, // IDC_ARROW
            32513, // IDC_IBEAM
            32514, // IDC_WAIT
            32515, // IDC_CROSS
            32516, // IDC_UPARROW
            32642, // IDC_SIZENWSE
            32643, // IDC_SIZENESW
            32644, // IDC_SIZEWE
            32645, // IDC_SIZENS
            32646, // IDC_SIZEALL
            32648, // IDC_NO
            32649, // IDC_HAND
            32650, // IDC_APPSTARTING
        };

        /// <summary>
        /// Minimum Windows version for DXGI 1.2 OutputDuplication (Windows 8+)
        /// </summary>
        private static readonly Version MinimumDxgiVersion = new Version(6, 2);

        /// <summary>
        /// Check if the current OS supports DXGI output duplication
        /// </summary>
        public static bool IsSupported => Environment.OSVersion.Version >= MinimumDxgiVersion;

        public WindowsModernCaptureService(IScreenService screenService)
        {
            _screenService = screenService;
            _fallbackService = new WindowsScreenCaptureService(screenService);
        }

        public Task<SKRectI> SelectRegionAsync(CaptureOptions? options = null)
        {
            // This method should only be called from the UI layer wrapper
            return Task.FromResult(SKRectI.Empty);
        }

        public async Task<SKBitmap?> CaptureRegionAsync(CaptureOptions? options = null)
        {
            return await CaptureFullScreenAsync(options);
        }
        public async Task<SKBitmap?> CaptureRectAsync(SKRect rect, CaptureOptions? options = null)
        {
            var captureSettings = options?.WorkflowId != null 
                ? XerahS.Core.SettingsManager.GetWorkflowTaskSettings(options.WorkflowId)?.CaptureSettings 
                : XerahS.Core.SettingsManager.DefaultTaskSettings.CaptureSettings;

            // Default true to match TaskSettingsCapture.UseModernCapture and CaptureOptions; prefer DXGI over GDI.
            bool useModern = options?.UseModernCapture ?? captureSettings?.UseModernCapture ?? true;

            if (!IsSupported || !useModern)
            {
                XerahS.Common.DebugHelper.WriteLine("Screen capture: using GDI fallback (UseModernCapture=false or DXGI unavailable)");
                return await _fallbackService.CaptureRectAsync(rect, options);
            }

            var result = await Task.Run(() =>
            {
                try
                {
                    // DXGI captures the raw desktop without cursor
                    // We pass ShowCursor option to control cursor drawing
                    using var fullBitmap = CaptureFullScreenDxgi(options?.ShowCursor == true);
                    if (fullBitmap == null) return null;

                    // Get virtual desktop bounds to convert screen coordinates to bitmap coordinates
                    var virtualBounds = _screenService.GetVirtualScreenBounds();

                    // Convert screen coordinates to bitmap coordinates
                    // fullBitmap (0,0) corresponds to virtualBounds (Left,Top)
                    var cropRect = new SKRectI(
                        (int)rect.Left - virtualBounds.X,   // Convert screen Left to bitmap Left
                        (int)rect.Top - virtualBounds.Y,    // Convert screen Top to bitmap Top
                        (int)rect.Right - virtualBounds.X,  // Convert screen Right to bitmap Right
                        (int)rect.Bottom - virtualBounds.Y   // Convert screen Bottom to bitmap Bottom
                    );

                    // Clamp to bitmap bounds
                    cropRect.Left = Math.Max(0, cropRect.Left);
                    cropRect.Top = Math.Max(0, cropRect.Top);
                    cropRect.Right = Math.Min(fullBitmap.Width, cropRect.Right);
                    cropRect.Bottom = Math.Min(fullBitmap.Height, cropRect.Bottom);

                    if (cropRect.Width <= 0 || cropRect.Height <= 0)
                        return null;

                    var cropped = new SKBitmap(cropRect.Width, cropRect.Height);
                    using var canvas = new SKCanvas(cropped);
                    canvas.DrawBitmap(fullBitmap, cropRect, new SKRect(0, 0, cropRect.Width, cropRect.Height));
                    return cropped;
                }
                catch (Exception)
                {
                    // Fall back to GDI+ on error
                    return null;
                }
            });
            if (result == null)
            {
                XerahS.Common.DebugHelper.WriteLine("Screen capture: DXGI returned null; using GDI fallback");
                return await _fallbackService.CaptureRectAsync(rect, options);
            }
            return result;
        }

        public async Task<SKBitmap?> CaptureFullScreenAsync(CaptureOptions? options = null)
        {
            var captureSettings = options?.WorkflowId != null 
                ? XerahS.Core.SettingsManager.GetWorkflowTaskSettings(options.WorkflowId)?.CaptureSettings 
                : XerahS.Core.SettingsManager.DefaultTaskSettings.CaptureSettings;

            // Default true to match TaskSettingsCapture.UseModernCapture and CaptureOptions; prefer DXGI over GDI.
            bool useModern = options?.UseModernCapture ?? captureSettings?.UseModernCapture ?? true;

            if (!IsSupported || !useModern)
            {
                XerahS.Common.DebugHelper.WriteLine("Screen capture: using GDI fallback (UseModernCapture=false or DXGI unavailable)");
                return await _fallbackService.CaptureFullScreenAsync(options);
            }

            var fullResult = await Task.Run(() =>
            {
                try
                {
                    return CaptureFullScreenDxgi(options?.ShowCursor == true);
                }
                catch (Exception)
                {
                    return null;
                }
            });
            if (fullResult == null)
            {
                XerahS.Common.DebugHelper.WriteLine("Screen capture: DXGI returned null; using GDI fallback");
                return await _fallbackService.CaptureFullScreenAsync(options);
            }
            return fullResult;
        }

        public async Task<SKBitmap?> CaptureActiveWindowAsync(IWindowService windowService, CaptureOptions? options = null)
        {
            // For window capture, we capture the window bounds
            var hwnd = windowService.GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return null;

            var bounds = windowService.GetWindowBounds(hwnd);
            if (bounds.Width <= 0 || bounds.Height <= 0) return null;

            return await CaptureRectAsync(new SKRect(bounds.X, bounds.Y, bounds.Right, bounds.Bottom), options);
        }

        public async Task<SKBitmap?> CaptureWindowAsync(IntPtr windowHandle, IWindowService windowService, CaptureOptions? options = null)
        {
            if (windowHandle == IntPtr.Zero) return null;

            var bounds = windowService.GetWindowBounds(windowHandle);
            if (bounds.Width <= 0 || bounds.Height <= 0) return null;

            return await CaptureRectAsync(new SKRect(bounds.X, bounds.Y, bounds.Right, bounds.Bottom), options);
        }

        public Task<CursorInfo?> CaptureCursorAsync()
        {
            // Delegate cursor capture to GDI approach
            return _fallbackService.CaptureCursorAsync();
        }

        /// <summary>
        /// Captures the entire virtual screen using DXGI Output Duplication
        /// </summary>
        private SKBitmap? CaptureFullScreenDxgi(bool drawCursor = false)
        {
            // Hide cursor during capture if we don't want it (DWM renders cursor as part of desktop)
            // ShowCursor only affects current process window. To hide globally from DWM capture,
            // we must use SetSystemCursor to temporarily replace the global cursor with a transparent one.
            bool cursorHidden = false;
            
            if (!drawCursor)
            {
                try
                {
                    // Replace all system cursors with a transparent 32x32 cursor.
                    // SetSystemCursor(IntPtr.Zero, ...) is unreliable (NULL handle may fail).
                    var andMask = new byte[128]; // 32x32 / 8
                    var xorMask = new byte[128];
                    for (int i = 0; i < andMask.Length; i++) andMask[i] = 0xFF;

                    IntPtr blankCursor = CreateCursor(IntPtr.Zero, 0, 0, 32, 32, andMask, xorMask);
                    if (blankCursor != IntPtr.Zero)
                    {
                        try
                        {
                            foreach (uint id in AllCursorIds)
                            {
                                IntPtr copy = CopyIcon(blankCursor);
                                if (copy != IntPtr.Zero)
                                    SetSystemCursor(copy, id);
                            }
                            cursorHidden = true;
                        }
                        finally
                        {
                            DestroyCursor(blankCursor);
                        }
                    }

                    // Small delay to ensure DWM updates composition
                    if (cursorHidden) Thread.Sleep(50);
                }
                catch (Exception ex)
                {
                    XerahS.Common.DebugHelper.WriteLine($"CaptureFullScreenDxgi: Failed to hide cursor. {ex.Message}");
                }
            }

            try
            {
                // Create DXGI factory
                using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
                if (factory == null) return null;

            // Enumerate adapters and outputs
            var outputs = EnumerateOutputs(factory);
            if (outputs.Count == 0) return null;

            // Calculate captured virtual screen bounds
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (var (output, adapter, bounds, _, _, _) in outputs)
            {
                minX = Math.Min(minX, bounds.Left);
                minY = Math.Min(minY, bounds.Top);
                maxX = Math.Max(maxX, bounds.Right);
                maxY = Math.Max(maxY, bounds.Bottom);
            }

            int totalWidth = maxX - minX;
            int totalHeight = maxY - minY;

            if (totalWidth <= 0 || totalHeight <= 0) return null;

            // Create combined bitmap
            var combinedBitmap = new SKBitmap(totalWidth, totalHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
            using var canvas = new SKCanvas(combinedBitmap);
            canvas.Clear(SKColors.Black);

            // Group outputs by adapter to share ID3D11Device
            var outputsByAdapter = outputs.GroupBy(x => x.Adapter).ToList();

            // Track resources for batch processing
            var activeDuplications = new List<(
                IDXGIOutputDuplication Duplication,
                ID3D11Device Device,
                System.Drawing.Rectangle Bounds,
                ModeRotation Rotation,
                string DeviceName,
                ModeRotation DxgiRotation)>();
            var devicesToDispose = new List<ID3D11Device>();

            try
            {
                // 1. Initialize all duplications (Create Devices & Duplications)
                foreach (var group in outputsByAdapter)
                {
                    var adapter = group.Key;

                    // Create one device per adapter
                    if (D3D11.D3D11CreateDevice(adapter, DriverType.Unknown, DeviceCreationFlags.BgraSupport,
                        new[] { FeatureLevel.Level_11_1, FeatureLevel.Level_11_0 }, out var device).Failure || device == null)
                    {
                        continue;
                    }
                    devicesToDispose.Add(device);

                    // using var deviceContext = device.ImmediateContext; // REMOVED: Premature disposal causes NRE later

                    foreach (var (output, _, bounds, rotation, deviceName, dxgiRotation) in group)
                    {
                        try
                        {
                            // Duplicate output
                            var duplication = output.DuplicateOutput(device);
                            activeDuplications.Add((duplication, device, bounds, rotation, deviceName, dxgiRotation));
                        }
                        catch (Exception ex)
                        {
                            // Output might be disconnected or in use
                            XerahS.Common.DebugHelper.WriteLine($"CaptureFullScreenDxgi: Setup failed for output. {ex}");
                        }
                        finally
                        {
                            output.Dispose(); // Dispose IDXGIOutput1 immediately after use
                        }
                    }
                }

                // 2. Wait for next frame (important for first capture initialization)
                // Doing this ONCE creates a small delay that allows all duplications to accumulate a frame
                if (activeDuplications.Count > 0)
                {
                    System.Threading.Thread.Sleep(50); // Reduced from 100ms per screen to 50ms global
                }

                // 3. Acquire & Process Frames
                foreach (var (duplication, device, bounds, rotation, deviceName, dxgiRotation) in activeDuplications)
                {
                    bool frameAcquired = false;
                    try
                    {
                        // Acquire frame
                        var acquireResult = duplication.AcquireNextFrame(250, out var frameInfo, out var desktopResource);

                        if (acquireResult.Success && desktopResource != null)
                        {
                            frameAcquired = true;
                            using (desktopResource)
                            {
                                using var desktopTex = desktopResource.QueryInterface<ID3D11Texture2D>();
                                var sourceDesc = desktopTex.Description;
                                XerahS.Common.DebugHelper.WriteLine(
                                    $"CaptureFullScreenDxgi: Output {deviceName} frame source={sourceDesc.Width}x{sourceDesc.Height}, target={bounds.Width}x{bounds.Height}, rotation={rotation}, dxgiRotation={dxgiRotation}");

                                // For rotated outputs, Desktop Duplication can return an unrotated surface
                                // whose dimensions differ from desktop bounds. Always match staging to source.
                                var stagingDesc = new Texture2DDescription
                                {
                                    Width = sourceDesc.Width,
                                    Height = sourceDesc.Height,
                                    MipLevels = 1,
                                    ArraySize = 1,
                                    Format = sourceDesc.Format,
                                    SampleDescription = new SampleDescription(1, 0),
                                    Usage = ResourceUsage.Staging,
                                    BindFlags = BindFlags.None,
                                    CPUAccessFlags = CpuAccessFlags.Read,
                                    MiscFlags = ResourceOptionFlags.None
                                };

                                using var staging = device.CreateTexture2D(stagingDesc);
                                device.ImmediateContext.CopyResource(staging, desktopTex);

                                // Map staging texture
                                var dataBox = device.ImmediateContext.Map(staging, 0, MapMode.Read);
                                try
                                {
                                    // Draw to combined bitmap, applying output rotation correction.
                                    DrawMappedTextureToCanvas(
                                        dataBox,
                                        (int)sourceDesc.Width,
                                        (int)sourceDesc.Height,
                                        bounds.Left - minX,
                                        bounds.Top - minY,
                                        bounds.Width,
                                        bounds.Height,
                                        rotation,
                                        canvas);
                                }
                                finally
                                {
                                    device.ImmediateContext.Unmap(staging, 0);
                                }
                            }
                        }
                        else
                        {
                            XerahS.Common.DebugHelper.WriteLine($"CaptureFullScreenDxgi: AcquireFrame failed or timed out.");
                        }
                    }
                    catch (Exception ex)
                    {
                        XerahS.Common.DebugHelper.WriteLine($"CaptureFullScreenDxgi: Frame capture failed. {ex}");
                    }
                    finally
                    {
                        if (frameAcquired)
                        {
                            try
                            {
                                duplication.ReleaseFrame();
                            }
                            catch
                            {
                                // Ignore frame release failures during cleanup.
                            }
                        }

                        duplication.Dispose();
                    }
                }
            }
            finally
            {
                foreach (var d in devicesToDispose) d.Dispose();

                // Dispose unique adapters
                foreach (var group in outputsByAdapter)
                {
                    group.Key.Dispose();
                }
            }

            // Log success so the log file verifies DXGI (Vortice.Direct3D11/DXGI) was actually used
            XerahS.Common.DebugHelper.WriteLine($"Screen capture: DXGI Output Duplication succeeded ({combinedBitmap.Width}x{combinedBitmap.Height})");

            // Draw cursor if requested (after DXGI capture which doesn't include cursor)
            if (drawCursor)
            {
                try
                {
                    // Get virtual desktop offset for cursor position calculation
                    var virtualBounds = _screenService.GetVirtualScreenBounds();
                    
                    // CursorData draws the cursor onto a GDI DC, so we use GDI
                    using var tempBitmap = new System.Drawing.Bitmap(combinedBitmap.Width, combinedBitmap.Height);
                    using var g = System.Drawing.Graphics.FromImage(tempBitmap);
                    IntPtr hdc = g.GetHdc();
                    try
                    {
                        var cursor = new CursorData();
                        cursor.DrawCursor(hdc, new System.Drawing.Point(virtualBounds.X, virtualBounds.Y));
                    }
                    finally
                    {
                        g.ReleaseHdc(hdc);
                    }
                    
                    // Copy cursor overlay to SKBitmap using alpha blend
                    using var cursorStream = new MemoryStream();
                    tempBitmap.Save(cursorStream, System.Drawing.Imaging.ImageFormat.Png);
                    cursorStream.Seek(0, SeekOrigin.Begin);
                    using var cursorBitmap = SKBitmap.Decode(cursorStream);
                    
                    // Draw only the cursor (non-black pixels) onto combined bitmap
                    // Note: CursorData only draws the cursor, so tempBitmap should have cursor on transparent/black
                    using var cursorCanvas = new SKCanvas(combinedBitmap);
                    using var paint = new SKPaint { BlendMode = SKBlendMode.SrcOver };
                    cursorCanvas.DrawBitmap(cursorBitmap, 0, 0, paint);
                }
                catch
                {
                    // Ignore cursor drawing errors
                }
            }

            return combinedBitmap;
            }
            finally
            {
                // Restore cursors if we hid them using SetSystemCursor
                if (cursorHidden)
                {
                    SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, 0);
                }
            }
        }

        private void DrawMappedTextureToCanvas(
            MappedSubresource dataBox,
            int sourceWidth,
            int sourceHeight,
            int destX,
            int destY,
            int destWidth,
            int destHeight,
            ModeRotation rotation,
            SKCanvas canvas)
        {
            // Create a temporary SKBitmap wrapping the data
            // Note: We cannot wrap directly because SKBitmap doesn't support strided data easily without copy or specialized installPixels
            // For simplicity and safety (handling pitch), we copy row by row to a temp bitmap

            using var sourceBitmap = new SKBitmap(sourceWidth, sourceHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
            var destPixels = sourceBitmap.GetPixels();
            int srcPitch = (int)dataBox.RowPitch;
            int sourcePitch = sourceWidth * 4;

            unsafe
            {
                for (int y = 0; y < sourceHeight; y++)
                {
                    IntPtr srcRow = IntPtr.Add(dataBox.DataPointer, y * srcPitch);
                    IntPtr destRow = IntPtr.Add(destPixels, y * sourcePitch);

                    Buffer.MemoryCopy((void*)srcRow, (void*)destRow, sourcePitch, sourcePitch);
                }
            }

            using SKBitmap bitmapToDraw = RotateBitmapForDesktop(sourceBitmap, rotation);
            var destRect = new SKRect(destX, destY, destX + destWidth, destY + destHeight);
            canvas.DrawBitmap(bitmapToDraw, destRect);
        }

        /// <summary>
        /// Rotates an output frame back into desktop orientation when DXGI provides unrotated output.
        /// </summary>
        private static SKBitmap RotateBitmapForDesktop(SKBitmap sourceBitmap, ModeRotation rotation)
        {
            if (rotation is ModeRotation.Identity or ModeRotation.Unspecified)
            {
                return sourceBitmap.Copy();
            }

            int targetWidth = sourceBitmap.Width;
            int targetHeight = sourceBitmap.Height;

            if (rotation is ModeRotation.Rotate90 or ModeRotation.Rotate270)
            {
                targetWidth = sourceBitmap.Height;
                targetHeight = sourceBitmap.Width;
            }

            var rotatedBitmap = new SKBitmap(targetWidth, targetHeight, SKColorType.Bgra8888, SKAlphaType.Premul);

            using var canvas = new SKCanvas(rotatedBitmap);
            canvas.Clear(SKColors.Black);

            // DXGI rotation describes how the display is rotated from identity.
            // The duplicated frame must be inverse-rotated to match desktop coordinates.
            switch (rotation)
            {
                case ModeRotation.Rotate90:
                    canvas.Translate(0, targetHeight);
                    canvas.RotateDegrees(-90);
                    break;
                case ModeRotation.Rotate180:
                    canvas.Translate(targetWidth, targetHeight);
                    canvas.RotateDegrees(180);
                    break;
                case ModeRotation.Rotate270:
                    canvas.Translate(targetWidth, 0);
                    canvas.RotateDegrees(90);
                    break;
            }

            canvas.DrawBitmap(sourceBitmap, 0, 0);
            return rotatedBitmap;
        }

        /// <summary>
        /// Enumerates all DXGI outputs from all adapters
        /// </summary>
        private System.Collections.Generic.List<(
            IDXGIOutput1 Output,
            IDXGIAdapter1 Adapter,
            System.Drawing.Rectangle Bounds,
            ModeRotation Rotation,
            string DeviceName,
            ModeRotation DxgiRotation)>
            EnumerateOutputs(IDXGIFactory1 factory)
        {
            var outputs = new System.Collections.Generic.List<(
                IDXGIOutput1,
                IDXGIAdapter1,
                System.Drawing.Rectangle,
                ModeRotation,
                string,
                ModeRotation)>();

            for (uint adapterIndex = 0; factory.EnumAdapters1(adapterIndex, out var adapter).Success; adapterIndex++)
            {
                var desc = adapter.Description1;

                // Skip software adapters
                if ((desc.Flags & AdapterFlags.Software) != 0)
                {
                    adapter.Dispose();
                    continue;
                }

                // Enumerate outputs for this adapter
                for (uint outputIndex = 0; adapter.EnumOutputs(outputIndex, out var output).Success; outputIndex++)
                {
                    var output1 = output.QueryInterface<IDXGIOutput1>();
                    var outputDesc = output1.Description;

                    var rect = outputDesc.DesktopCoordinates;
                    var bounds = new System.Drawing.Rectangle(
                        rect.Left,
                        rect.Top,
                        rect.Right - rect.Left,
                        rect.Bottom - rect.Top
                    );

                    var dxgiRotation = outputDesc.Rotation;
                    var effectiveRotation = TryGetDisplaySettingsRotation(outputDesc.DeviceName, out var displayRotation)
                        ? displayRotation
                        : dxgiRotation;

                    if (effectiveRotation != dxgiRotation)
                    {
                        XerahS.Common.DebugHelper.WriteLine(
                            $"CaptureFullScreenDxgi: Rotation override for {outputDesc.DeviceName}. DisplaySettings={effectiveRotation}, DXGI={dxgiRotation}");
                    }

                    outputs.Add((output1, adapter, bounds, effectiveRotation, outputDesc.DeviceName, dxgiRotation));
                    output.Dispose();
                }

                // Only dispose adapter if no outputs were added from it
                if (outputs.Count == 0 || outputs[^1].Item2 != adapter)
                {
                    adapter.Dispose();
                }
            }

            return outputs;
        }

        private static bool TryGetDisplaySettingsRotation(string deviceName, out ModeRotation rotation)
        {
            rotation = ModeRotation.Unspecified;

            var devMode = new NativeMethods.DEVMODE
            {
                dmSize = (short)Marshal.SizeOf(typeof(NativeMethods.DEVMODE))
            };

            if (!NativeMethods.EnumDisplaySettings(deviceName, NativeMethods.ENUM_CURRENT_SETTINGS, ref devMode))
            {
                return false;
            }

            rotation = devMode.dmDisplayOrientation switch
            {
                NativeMethods.DMDO_DEFAULT => ModeRotation.Identity,
                NativeMethods.DMDO_90 => ModeRotation.Rotate90,
                NativeMethods.DMDO_180 => ModeRotation.Rotate180,
                NativeMethods.DMDO_270 => ModeRotation.Rotate270,
                _ => ModeRotation.Unspecified
            };

            return true;
        }

        private static class NativeMethods
        {
            public const int ENUM_CURRENT_SETTINGS = -1;

            public const int DMDO_DEFAULT = 0;
            public const int DMDO_90 = 1;
            public const int DMDO_180 = 2;
            public const int DMDO_270 = 3;

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct DEVMODE
            {
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
                public string dmDeviceName;
                public short dmSpecVersion;
                public short dmDriverVersion;
                public short dmSize;
                public short dmDriverExtra;
                public int dmFields;
                public int dmPositionX;
                public int dmPositionY;
                public int dmDisplayOrientation;
                public int dmDisplayFixedOutput;
                public short dmColor;
                public short dmDuplex;
                public short dmYResolution;
                public short dmTTOption;
                public short dmCollate;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
                public string dmFormName;
                public short dmLogPixels;
                public int dmBitsPerPel;
                public int dmPelsWidth;
                public int dmPelsHeight;
                public int dmDisplayFlags;
                public int dmDisplayFrequency;
                public int dmICMMethod;
                public int dmICMIntent;
                public int dmMediaType;
                public int dmDitherType;
                public int dmReserved1;
                public int dmReserved2;
                public int dmPanningWidth;
                public int dmPanningHeight;
            }

            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            public static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);
        }
    }
#pragma warning restore CA1416
}
