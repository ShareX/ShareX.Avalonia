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

using Avalonia.Threading;
using XerahS.Common;
using XerahS.Core;
using XerahS.Core.Helpers;
using XerahS.Platform.Abstractions;
using XerahS.RegionCapture;
using XerahS.RegionCapture.Services;
using SkiaSharp;
using System;
using System.Drawing;
using System.Diagnostics;

namespace XerahS.UI.Services
{
    public class ScreenCaptureService : IScreenCaptureService
    {
        private readonly IScreenCaptureService _platformImpl;

        public ScreenCaptureService(IScreenCaptureService platformImpl)
        {
            _platformImpl = platformImpl;
        }

        public Task<SKBitmap?> CaptureRectAsync(SKRect rect, CaptureOptions? options = null)
        {
            return _platformImpl.CaptureRectAsync(rect, options);
        }

        public async Task<SKRectI> SelectRegionAsync(CaptureOptions? options = null)
        {
            if (IsLinuxWayland() && (options?.UseModernCapture ?? true))
            {
                // On Wayland, prefer native region selectors (slurp/portal tools) to avoid
                // compositor issues with custom transparent overlays (notably Hyprland/wlroots).
                try
                {
                    var nativeSelection = await _platformImpl.SelectRegionAsync(options);
                    if (!nativeSelection.IsEmpty && nativeSelection.Width > 0 && nativeSelection.Height > 0)
                    {
                        return nativeSelection;
                    }
                }
                catch
                {
                    // Fall through to in-app selector as a best-effort fallback.
                }
            }

            SKRectI selection = SKRectI.Empty;

            try
            {
                // UI interaction must run on the UI thread
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    // Capture cursor if requested
                    XerahS.Platform.Abstractions.CursorInfo? cursorInfo = null;
                    if (options?.ShowCursor == true)
                    {
                        try
                        {
                            cursorInfo = await _platformImpl.CaptureCursorAsync();
                        }
                        catch
                        {
                            // Ignore cursor capture errors
                        }
                    }

                    bool useFastOverlay = OperatingSystem.IsLinux() || (options?.UseTransparentOverlay ?? false);

                    // Capture background only when using the frozen-overlay path.
                    SkiaSharp.SKBitmap? backgroundForMagnifier = null;
                    if (!useFastOverlay)
                    {
                        try
                        {
                            backgroundForMagnifier = await _platformImpl.CaptureFullScreenAsync(new CaptureOptions { ShowCursor = false });
                        }
                        catch
                        {
                            // Ignore
                        }
                    }

                    var captureService = new RegionCaptureService
                    {
                        Options = new XerahS.RegionCapture.RegionCaptureOptions
                        {
                            ShowCursor = options?.ShowCursor ?? false,
                            BackgroundImage = backgroundForMagnifier,
                            UseTransparentOverlay = useFastOverlay,
                            EditorOptions = RegionCaptureAnnotationOptionsStore.GetEditorOptions(options?.WorkflowId),
                        }
                    };

                    XerahS.RegionCapture.Models.RegionSelectionResult? result;
                    try
                    {
                        result = await captureService.CaptureRegionAsync(cursorInfo);
                    }
                    finally
                    {
                        RegionCaptureAnnotationOptionsStore.Persist();
                    }

                    if (result is not null)
                    {
                        var r = result.Value.Region;
                        selection = new SKRectI((int)r.X, (int)r.Y, (int)r.Right, (int)r.Bottom);
                    }
                });
            }
            catch
            {
                // Ignore errors to ensure robustness
            }

            return selection;
        }

        public async Task<SKBitmap?> CaptureFullScreenAsync(CaptureOptions? options = null)
        {
            var totalStopwatch = Stopwatch.StartNew();
            // TroubleshootingHelper.Log("FullscreenCapture", "INIT", "Fullscreen capture started");

            var captureStopwatch = Stopwatch.StartNew();
            var result = await _platformImpl.CaptureFullScreenAsync(options);
            captureStopwatch.Stop();

            var resultText = result == null ? "null" : $"{result.Width}x{result.Height}";
            // TroubleshootingHelper.Log("FullscreenCapture", "CAPTURE", $"Platform capture finished in {captureStopwatch.ElapsedMilliseconds}ms, Result={resultText}");
            totalStopwatch.Stop();
            // TroubleshootingHelper.Log("FullscreenCapture", "TOTAL", $"Fullscreen capture total elapsed: {totalStopwatch.ElapsedMilliseconds}ms");

            return result;
        }

        public Task<SKBitmap?> CaptureActiveWindowAsync(IWindowService windowService, CaptureOptions? options = null)
        {
            return _platformImpl.CaptureActiveWindowAsync(windowService, options);
        }

        public Task<XerahS.Platform.Abstractions.CursorInfo?> CaptureCursorAsync()
        {
            return _platformImpl.CaptureCursorAsync();
        }

        public async Task<SKBitmap?> CaptureRegionAsync(CaptureOptions? options = null)
        {
            // On Linux, when UseModernCapture is true use the XDG Portal / system dialog for region capture
            // (behaviour prior to commit 58283cb). When false, use in-app overlay with crosshair.
            if (OperatingSystem.IsLinux() && (options?.UseModernCapture ?? true))
            {
                try
                {
                    var portalBitmap = await _platformImpl.CaptureRegionAsync(options);
                    if (portalBitmap != null)
                        DebugHelper.WriteLine("[RegionCapture] Region captured via platform (XDG Portal / system dialog).");
                    return portalBitmap;
                }
                catch (Exception ex)
                {
                    DebugHelper.WriteLine($"[RegionCapture] Platform region capture failed ({ex.Message}); falling back to overlay.");
                    // Fall through to overlay path
                }
            }

            DateTime sessionStartUtc = DateTime.UtcNow;
            DebugHelper.WriteLine($"[RegionCapture] Milestone: region capture started (+0 ms)");

            // 1. Capture cursor BEFORE showing overlay (if ShowCursor is enabled)
            XerahS.Platform.Abstractions.CursorInfo? ghostCursor = null;
            if (options?.ShowCursor == true)
            {
                try
                {
                    ghostCursor = await _platformImpl.CaptureCursorAsync();
                }
                catch
                {
                    // Ignore cursor capture errors
                }
            }

            // 2. Capture background for frozen overlay and magnifier BEFORE showing overlay
            // When UseTransparentOverlay is true, or on Linux (slow full-screen capture), skip so overlay appears immediately.
            // After selection we use CaptureRectAsync when fullScreenBitmap is null.
            bool useFastOverlay = OperatingSystem.IsLinux() || (options?.UseTransparentOverlay ?? false);
            SKBitmap? fullScreenBitmap = null;
            if (!useFastOverlay)
            {
                try
                {
                    fullScreenBitmap = await _platformImpl.CaptureFullScreenAsync(new CaptureOptions
                    {
                        ShowCursor = false,
                        UseModernCapture = options?.UseModernCapture ?? true
                    });
                    if (fullScreenBitmap != null)
                    {
                        DebugHelper.WriteLine($"[RegionCapture] Pre-capture fullScreenBitmap: {fullScreenBitmap.Width}x{fullScreenBitmap.Height}");
                    }
                    else
                    {
                        DebugHelper.WriteLine("[RegionCapture] Pre-capture fullScreenBitmap: null");
                    }
                }
                catch
                {
                    // Ignore - full screen capture is optional for fallback
                }
            }

            // 3. Select Region (UI) - pass ghost cursor for overlay display
            DebugHelper.WriteLine($"[RegionCapture] Milestone: region capture UI invoked (+{(DateTime.UtcNow - sessionStartUtc).TotalMilliseconds:F0} ms)");
            SKRectI selection = SKRectI.Empty;
            SKBitmap? annotationLayer = null;
            XerahS.RegionCapture.Models.PixelPoint annotationMonitorOrigin = default;
            try
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    var captureService = new RegionCaptureService
                    {
                        Options = new XerahS.RegionCapture.RegionCaptureOptions
                        {
                            ShowCursor = options?.ShowCursor ?? false,
                            BackgroundImage = fullScreenBitmap,
                            UseTransparentOverlay = useFastOverlay,
                            EditorOptions = RegionCaptureAnnotationOptionsStore.GetEditorOptions(options?.WorkflowId),
                            SessionStartUtc = sessionStartUtc,
                        }
                    };

                    XerahS.RegionCapture.Models.RegionSelectionResult? result;
                    try
                    {
                        result = await captureService.CaptureRegionAsync(ghostCursor);
                    }
                    finally
                    {
                        RegionCaptureAnnotationOptionsStore.Persist();
                    }

                    if (result is not null)
                    {
                        var r = result.Value.Region;
                        selection = new SKRectI((int)r.X, (int)r.Y, (int)r.Right, (int)r.Bottom);
                        annotationLayer = result.Value.AnnotationLayer;
                        annotationMonitorOrigin = result.Value.MonitorOrigin;
                    }
                });
                double elapsedMs = (DateTime.UtcNow - sessionStartUtc).TotalMilliseconds;
                DebugHelper.WriteLine($"[RegionCapture] Milestone: region UI returned (+{elapsedMs:F0} ms)");
            }
            catch
            {
                // Ignore errors
            }
            // Note: fullScreenBitmap is disposed later after cropping

            if (selection.IsEmpty || selection.Width <= 0 || selection.Height <= 0)
            {
                DebugHelper.Flush();
                return null;
            }

            bool showCursor = options?.ShowCursor == true;

            // 3. Optional delay before capture starts (cancellable)
            if (options?.CaptureStartDelaySeconds > 0)
            {
                var delayMs = (int)Math.Round(options.CaptureStartDelaySeconds * 1000, MidpointRounding.AwayFromZero);
                var workflowId = string.IsNullOrWhiteSpace(options.WorkflowId) ? "none" : options.WorkflowId;
                var workflowCategory = string.IsNullOrWhiteSpace(options.WorkflowCategory) ? "Unknown" : options.WorkflowCategory;
                TroubleshootingHelper.Log("CaptureDelay", "REGION", $"WorkflowId={workflowId}, Category={workflowCategory}, DelaySeconds={options.CaptureStartDelaySeconds:F3}, DelayMs={delayMs}");

                try
                {
                    await Task.Delay(delayMs, options.CaptureStartDelayCancellationToken);
                    TroubleshootingHelper.Log("CaptureDelay", "REGION", $"WorkflowId={workflowId}, Category={workflowCategory}, DelayCompleted=true");
                }
                catch (OperationCanceledException)
                {
                    TroubleshootingHelper.Log("CaptureDelay", "REGION", $"WorkflowId={workflowId}, Category={workflowCategory}, DelayCancelled=true");
                    DebugHelper.Flush();
                    return null;
                }
            }

            // 4. Small delay to allow overlay windows to close fully and cursor to hide
            await Task.Delay(200);
            DebugHelper.WriteLine($"[RegionCapture] Milestone: post-overlay delay done (+{(DateTime.UtcNow - sessionStartUtc).TotalMilliseconds:F0} ms)");

            // 5. Capture Screen (Platform) - ALWAYS without cursor.
            // The ghost cursor (captured at workflow start) is drawn manually in step 6.
            // Never let the platform draw the live cursor, as it may have moved into the
            // captured region during the delay between overlay close and bitmap acquisition.
            var captureOptions = new CaptureOptions
            {
                ShowCursor = false,
                UseModernCapture = options?.UseModernCapture ?? true,
                WorkflowId = options?.WorkflowId,
                WorkflowCategory = options?.WorkflowCategory
            };

            SKBitmap? bitmap = null;
            try
            {
                if (fullScreenBitmap != null)
                {
                    var cropRect = selection;
                    try
                    {
                        var virtualBounds = PlatformServices.Screen.GetVirtualScreenBounds();
                        DebugHelper.WriteLine($"[RegionCapture] Pre-capture crop: fullBitmap={fullScreenBitmap.Width}x{fullScreenBitmap.Height} virtualBounds=({virtualBounds.X},{virtualBounds.Y},{virtualBounds.Width}x{virtualBounds.Height}) selection=({selection.Left},{selection.Top},{selection.Right},{selection.Bottom})");
                        if (!virtualBounds.IsEmpty)
                        {
                            var offsetX = virtualBounds.X;
                            var offsetY = virtualBounds.Y;
                            cropRect = new SKRectI(
                                selection.Left - offsetX,
                                selection.Top - offsetY,
                                selection.Right - offsetX,
                                selection.Bottom - offsetY);
                            DebugHelper.WriteLine($"[RegionCapture] Pre-capture crop: offset=({offsetX},{offsetY}) cropRect=({cropRect.Left},{cropRect.Top},{cropRect.Right},{cropRect.Bottom}) size={cropRect.Width}x{cropRect.Height}");
                        }
                    }
                    catch
                    {
                        // Ignore virtual screen lookup failures
                    }

                    SKBitmap cropped = ImageHelpers.Crop(fullScreenBitmap, cropRect);
                    if (cropped.Width > 0 && cropped.Height > 0)
                    {
                        bitmap = cropped;
                    }
                    else
                    {
                        cropped.Dispose();
                    }
                }
            }
            catch
            {
                // Ignore crop errors and fall back to live capture
            }
            finally
            {
                fullScreenBitmap?.Dispose();
            }

            if (bitmap != null)
            {
                DebugHelper.WriteLine($"[RegionCapture] Milestone: bitmap obtained (+{(DateTime.UtcNow - sessionStartUtc).TotalMilliseconds:F0} ms)");
            }

            // Only when there was no pre-capture (e.g. Linux fast overlay). On Windows we normally have
            // fullScreenBitmap and crop from it above; this block is not used for standard Windows region capture.
            if (bitmap == null)
            {
                DebugHelper.WriteLine($"[RegionCapture] CaptureRectAsync path (no pre-capture). Selection physical: L={selection.Left}, T={selection.Top}, R={selection.Right}, B={selection.Bottom}, Size={selection.Right - selection.Left}x{selection.Bottom - selection.Top}");

                // Start with selection in physical pixels. Windows (and macOS) keep this; Linux converts to logical below.
                SKRect rectForCapture = new SKRect(selection.Left, selection.Top, selection.Right, selection.Bottom);
                try
                {
                    var monitors = MonitorEnumerationService.GetAllMonitors();
                    DebugHelper.WriteLine($"[RegionCapture] Monitor count={monitors.Count}, Linux={OperatingSystem.IsLinux()}");

                    if (monitors.Count > 0)
                    {
                        // Linux only: portal screenshot is in logical pixels. Convert selection to logical and pass
                        // logical virtual bounds so crop mapping is correct. Windows and other platforms are unchanged.
                        if (OperatingSystem.IsLinux())
                        {
                            var ov = monitors[0].OverlayBounds;
                            double vLeft = ov.X, vTop = ov.Y, vRight = ov.Right, vBottom = ov.Bottom;
                            for (int i = 1; i < monitors.Count; i++)
                            {
                                var o = monitors[i].OverlayBounds;
                                vLeft = Math.Min(vLeft, o.X);
                                vTop = Math.Min(vTop, o.Y);
                                vRight = Math.Max(vRight, o.Right);
                                vBottom = Math.Max(vBottom, o.Bottom);
                            }
                            captureOptions.VirtualScreenBoundsForCrop = Rectangle.FromLTRB(
                                (int)Math.Round(vLeft),
                                (int)Math.Round(vTop),
                                (int)Math.Round(vRight),
                                (int)Math.Round(vBottom));
                            DebugHelper.WriteLine($"[RegionCapture] Virtual bounds (logical, OverlayBounds union): L={vLeft:F0}, T={vTop:F0}, R={vRight:F0}, B={vBottom:F0}, Size={vRight - vLeft:F0}x{vBottom - vTop:F0}");

                            // Also pass physical virtual bounds so LinuxScreenCaptureService can detect
                            // whether the portal returned a physical-resolution bitmap (e.g. KDE Plasma)
                            // and apply the correct crop without a uniform linear scale.
                            var pb0 = monitors[0].PhysicalBounds;
                            double physVLeft = pb0.X, physVTop = pb0.Y, physVRight = pb0.Right, physVBottom = pb0.Bottom;
                            for (int i = 1; i < monitors.Count; i++)
                            {
                                var pb = monitors[i].PhysicalBounds;
                                physVLeft = Math.Min(physVLeft, pb.X);
                                physVTop = Math.Min(physVTop, pb.Y);
                                physVRight = Math.Max(physVRight, pb.Right);
                                physVBottom = Math.Max(physVBottom, pb.Bottom);
                            }
                            captureOptions.PhysicalVirtualScreenBoundsForCrop = Rectangle.FromLTRB(
                                (int)Math.Round(physVLeft),
                                (int)Math.Round(physVTop),
                                (int)Math.Round(physVRight),
                                (int)Math.Round(physVBottom));
                            captureOptions.PhysicalRectForCrop = Rectangle.FromLTRB(
                                selection.Left, selection.Top, selection.Right, selection.Bottom);
                            DebugHelper.WriteLine($"[RegionCapture] Physical virtual bounds: L={physVLeft:F0}, T={physVTop:F0}, R={physVRight:F0}, B={physVBottom:F0}, Size={physVRight - physVLeft:F0}x{physVBottom - physVTop:F0}");
                            DebugHelper.WriteLine($"[RegionCapture] PhysicalRectForCrop: L={selection.Left}, T={selection.Top}, R={selection.Right}, B={selection.Bottom}");

                            double cx = (selection.Left + selection.Right) / 2.0;
                            double cy = (selection.Top + selection.Bottom) / 2.0;
                            var center = new XerahS.RegionCapture.Models.PixelPoint((int)cx, (int)cy);
                            XerahS.RegionCapture.Models.MonitorInfo? monitorAtCenter = null;
                            int monitorIndex = -1;
                            for (int i = 0; i < monitors.Count; i++)
                            {
                                if (monitors[i].PhysicalBounds.Contains(center))
                                {
                                    monitorAtCenter = monitors[i];
                                    monitorIndex = i;
                                    break;
                                }
                            }
                            monitorAtCenter ??= monitors[0];
                            if (monitorIndex < 0) monitorIndex = 0;
                            var phys = monitorAtCenter.PhysicalBounds;
                            var over = monitorAtCenter.OverlayBounds;
                            double s = monitorAtCenter.ScaleFactor;
                            DebugHelper.WriteLine($"[RegionCapture] Monitor at selection center: index={monitorIndex}, DeviceName={monitorAtCenter.DeviceName}, ScaleFactor={s:F2}, PhysicalBounds=({phys.X:F0},{phys.Y:F0},{phys.Right:F0},{phys.Bottom:F0}), OverlayBounds=({over.X:F0},{over.Y:F0},{over.Right:F0},{over.Bottom:F0})");

                            double logLeft = over.X + (selection.Left - phys.X) / s;
                            double logTop = over.Y + (selection.Top - phys.Y) / s;
                            double logRight = over.X + (selection.Right - phys.X) / s;
                            double logBottom = over.Y + (selection.Bottom - phys.Y) / s;
                            rectForCapture = new SKRect((float)logLeft, (float)logTop, (float)logRight, (float)logBottom);
                            DebugHelper.WriteLine($"[RegionCapture] Selection converted to logical: L={logLeft:F1}, T={logTop:F1}, R={logRight:F1}, B={logBottom:F1}, Size={logRight - logLeft:F1}x{logBottom - logTop:F1}");
                        }
                        else
                        {
                            // Windows / macOS: selection and virtual bounds stay in physical pixels; platform capture uses same space.
                            var b = monitors[0].PhysicalBounds;
                            double left = b.X, top = b.Y, right = b.Right, bottom = b.Bottom;
                            for (int i = 1; i < monitors.Count; i++)
                            {
                                var m = monitors[i].PhysicalBounds;
                                left = Math.Min(left, m.X);
                                top = Math.Min(top, m.Y);
                                right = Math.Max(right, m.Right);
                                bottom = Math.Max(bottom, m.Bottom);
                            }
                            captureOptions.VirtualScreenBoundsForCrop = Rectangle.FromLTRB(
                                (int)Math.Round(left),
                                (int)Math.Round(top),
                                (int)Math.Round(right),
                                (int)Math.Round(bottom));
                            DebugHelper.WriteLine($"[RegionCapture] Virtual bounds (physical, PhysicalBounds union): L={left:F0}, T={top:F0}, R={right:F0}, B={bottom:F0}, Size={right - left:F0}x{bottom - top:F0}");
                        }
                    }
                    else
                    {
                        DebugHelper.WriteLine("[RegionCapture] No monitors; rect passed as-is, no VirtualScreenBoundsForCrop");
                    }
                }
                catch (Exception ex)
                {
                    DebugHelper.WriteLine($"[RegionCapture] Exception computing virtual bounds / logical rect: {ex.Message}");
                    // Ignore; platform may use rect as-is or fall back to platform screen bounds
                }

                DebugHelper.WriteLine($"[RegionCapture] Milestone: CaptureRectAsync called (+{(DateTime.UtcNow - sessionStartUtc).TotalMilliseconds:F0} ms)");
                DebugHelper.WriteLine($"[RegionCapture] Calling CaptureRectAsync: rect L={rectForCapture.Left:F1}, T={rectForCapture.Top:F1}, R={rectForCapture.Right:F1}, B={rectForCapture.Bottom:F1}, VirtualScreenBoundsForCrop={captureOptions.VirtualScreenBoundsForCrop?.ToString() ?? "null"}");
                bitmap = await _platformImpl.CaptureRectAsync(rectForCapture, captureOptions);
                DebugHelper.WriteLine($"[RegionCapture] Milestone: CaptureRectAsync returned (+{(DateTime.UtcNow - sessionStartUtc).TotalMilliseconds:F0} ms)");
            }

            // 6. Draw ghost cursor onto captured bitmap if available
            // We use the INITIAL ghost cursor (captured at start) to match ShareX behavior ("original location").
            // The Live cursor is hidden from the DXGI capture by the platform service.
            if (bitmap != null && ghostCursor?.Image != null && showCursor)
            {
                try
                {
                    // Calculate cursor position relative to the captured region
                    int cursorX = ghostCursor.Position.X - selection.Left - ghostCursor.Hotspot.X;
                    int cursorY = ghostCursor.Position.Y - selection.Top - ghostCursor.Hotspot.Y;

                    // Draw cursor onto bitmap using SkiaSharp
                    using var canvas = new SKCanvas(bitmap);
                    using var paint = new SKPaint { BlendMode = SKBlendMode.SrcOver };
                    canvas.DrawBitmap(ghostCursor.Image, cursorX, cursorY, paint);
                }
                catch
                {
                    // Ignore cursor drawing errors
                }
            }

            // 7. Composite annotation layer onto captured bitmap if available
            // XIP-0023: Annotations drawn during region capture are rendered onto the final image
            if (bitmap != null && annotationLayer != null)
            {
                try
                {
                    using var canvas = new SKCanvas(bitmap);
                    using var paint = new SKPaint { BlendMode = SKBlendMode.SrcOver };

                    // The annotation layer is monitor-sized, but selection is in absolute screen coords.
                    // Subtract the monitor origin to get coordinates relative to the annotation layer.
                    var srcRect = new SKRect(
                        selection.Left - (float)annotationMonitorOrigin.X,
                        selection.Top - (float)annotationMonitorOrigin.Y,
                        selection.Right - (float)annotationMonitorOrigin.X,
                        selection.Bottom - (float)annotationMonitorOrigin.Y);
                    var dstRect = new SKRect(0, 0, bitmap.Width, bitmap.Height);

                    canvas.DrawBitmap(annotationLayer, srcRect, dstRect, paint);
                }
                catch
                {
                    // Ignore annotation compositing errors
                }
                finally
                {
                    annotationLayer.Dispose();
                }
            }

            // Flush so milestone and debug lines appear in Debug Log (and log file) when user copies
            DebugHelper.Flush();

            return bitmap;
        }

        public Task<SKBitmap?> CaptureWindowAsync(IntPtr windowHandle, IWindowService windowService, CaptureOptions? options = null)
        {
            return _platformImpl.CaptureWindowAsync(windowHandle, windowService, options);
        }

        private static bool IsLinuxWayland()
        {
            return OperatingSystem.IsLinux() &&
                   Environment.GetEnvironmentVariable("XDG_SESSION_TYPE")?.Equals("wayland", StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}
