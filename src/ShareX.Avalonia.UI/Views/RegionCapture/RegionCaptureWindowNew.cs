// This file contains the NEW backend integration code for RegionCaptureWindow
// It will be merged into RegionCaptureWindow.axaml.cs once tested

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ShareX.Avalonia.UI.Services;
using ShareX.Avalonia.Platform.Abstractions.Capture;
using SkiaSharp;

namespace XerahS.UI.Views.RegionCapture
{
    /// <summary>
    /// New backend integration methods for RegionCaptureWindow.
    /// These methods will replace the old DPI handling code.
    /// </summary>
    public partial class RegionCaptureWindow
    {
        // NEW: Fields for new backend
        private RegionCaptureService? _newCaptureService;
        private MonitorInfo[] _newMonitors = Array.Empty<MonitorInfo>();
        private LogicalRectangle _newVirtualDesktopLogical;
        private SKBitmap? _capturedBitmap;

        // Feature flag to enable new backend
        private const bool USE_NEW_BACKEND = true; // New backend enabled - platform backends now compile successfully

        // Public property to check if new backend is initialized
        public bool IsNewBackendInitialized => _newCaptureService != null;

        // Public property to get the captured bitmap (will be set after selection)
        public SKBitmap? GetCapturedBitmap() => _capturedBitmap;

        /// <summary>
        /// NEW: Initialize the new capture service backend.
        /// Call this in the constructor.
        /// </summary>
        private bool TryInitializeNewBackend()
        {
            if (!USE_NEW_BACKEND)
                return false;

            try
            {
                DebugLog("INIT", "Initializing NEW capture backend");

                // Create platform-specific backend
                IRegionCaptureBackend backend;

#if WINDOWS
                if (OperatingSystem.IsWindows())
                {
                    backend = new ShareX.Avalonia.Platform.Windows.Capture.WindowsRegionCaptureBackend();
                }
                else
#elif MACCATALYST || MACOS
                if (OperatingSystem.IsMacOS())
                {
                    backend = new ShareX.Avalonia.Platform.macOS.Capture.MacOSRegionCaptureBackend();
                }
                else
#elif LINUX
                if (OperatingSystem.IsLinux())
                {
                    backend = new ShareX.Avalonia.Platform.Linux.Capture.LinuxRegionCaptureBackend();
                }
                else
#endif
                {
                    DebugLog("ERROR", "Unsupported platform for new backend");
                    return false;
                }

                _newCaptureService = new RegionCaptureService(backend);
                _newMonitors = _newCaptureService.GetMonitors();
                _newVirtualDesktopLogical = _newCaptureService.GetVirtualDesktopBoundsLogical();

                DebugLog("INIT", $"NEW backend initialized successfully with {_newMonitors.Length} monitors");

                var capabilities = _newCaptureService.GetCapabilities();
                DebugLog("INIT", $"Backend: {capabilities.BackendName} {capabilities.Version}");
                DebugLog("INIT", $"  HW Accel: {capabilities.SupportsHardwareAcceleration}");
                DebugLog("INIT", $"  Per-Mon DPI: {capabilities.SupportsPerMonitorDpi}");

                foreach (var monitor in _newMonitors)
                {
                    DebugLog("MONITOR", $"{monitor.Name}:");
                    DebugLog("MONITOR", $"  Physical: {monitor.Bounds}");
                    DebugLog("MONITOR", $"  Scale: {monitor.ScaleFactor}x ({monitor.PhysicalDpi:F0} DPI)");
                    DebugLog("MONITOR", $"  Primary: {monitor.IsPrimary}");
                }

                return true;
            }
            catch (Exception ex)
            {
                DebugLog("ERROR", $"Failed to initialize new backend: {ex.Message}");
                DebugLog("ERROR", $"Stack: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// NEW: Position window using the new backend.
        /// Replaces the old screen enumeration logic in OnOpened.
        /// </summary>
        private void PositionWindowWithNewBackend()
        {
            if (_newCaptureService == null)
            {
                DebugLog("ERROR", "PositionWindowWithNewBackend called but service is null");
                return;
            }

            DebugLog("WINDOW", "=== Using NEW backend for positioning ===");

            // Get virtual desktop bounds in logical coordinates
            var logicalBounds = _newVirtualDesktopLogical;
            DebugLog("WINDOW", $"Virtual desktop logical: {logicalBounds}");

            // Position window (Avalonia uses logical coordinates for Position)
            Position = new PixelPoint(
                (int)Math.Round(logicalBounds.X),
                (int)Math.Round(logicalBounds.Y));

            // Size window (Avalonia uses logical coordinates for Width/Height)
            Width = logicalBounds.Width;
            Height = logicalBounds.Height;

            DebugLog("WINDOW", $"Window positioned at: {Position}");
            DebugLog("WINDOW", $"Window size: {Width}x{Height}");
            DebugLog("WINDOW", $"RenderScaling: {RenderScaling}");

            // Log physical bounds for comparison
            var physicalBounds = _newCaptureService.GetVirtualDesktopBoundsPhysical();
            DebugLog("WINDOW", $"Virtual desktop physical: {physicalBounds}");

            DebugLog("WINDOW", "=== NEW backend positioning complete ===");
        }

        /// <summary>
        /// NEW: Convert logical point to physical using new backend.
        /// Replaces ConvertLogicalToScreen.
        /// </summary>
        private SKPointI ConvertLogicalToPhysicalNew(Point logicalWindowPos)
        {
            if (_newCaptureService == null)
                throw new InvalidOperationException("New capture service not initialized");

            // Convert window-local logical to absolute logical
            var absoluteLogical = new LogicalPoint(
                logicalWindowPos.X + _newVirtualDesktopLogical.X,
                logicalWindowPos.Y + _newVirtualDesktopLogical.Y);

            // Convert to physical
            var physical = _newCaptureService.LogicalToPhysical(absoluteLogical);

            return new SKPointI(physical.X, physical.Y);
        }

        /// <summary>
        /// NEW: Convert physical point to logical using new backend.
        /// For window-local coordinates, subtract virtual desktop origin.
        /// </summary>
        private Point ConvertPhysicalToLogicalNew(SKPointI physicalScreen)
        {
            if (_newCaptureService == null)
                throw new InvalidOperationException("New capture service not initialized");

            // Convert to logical absolute
            var absoluteLogical = _newCaptureService.PhysicalToLogical(
                new PhysicalPoint(physicalScreen.X, physicalScreen.Y));

            // Convert to window-local logical
            var windowLocalX = absoluteLogical.X - _newVirtualDesktopLogical.X;
            var windowLocalY = absoluteLogical.Y - _newVirtualDesktopLogical.Y;

            return new Point(windowLocalX, windowLocalY);
        }

        /// <summary>
        /// NEW: Handle pointer pressed with new backend.
        /// </summary>
        private void OnPointerPressedNew(PointerPressedEventArgs e)
        {
            if (_newCaptureService == null) return;

            var logicalPos = e.GetPosition(this);
            var physical = ConvertLogicalToPhysicalNew(logicalPos);

            _startPointPhysical = physical;
            _startPointLogical = logicalPos;

            DebugLog("INPUT", $"[NEW] Pressed: Window-local={logicalPos}, Physical={physical}");

            _isSelecting = true;
            _dragStarted = false;
            _hoveredWindow = null;
        }

        /// <summary>
        /// NEW: Handle pointer moved with new backend.
        /// </summary>
        private void OnPointerMovedNew(PointerEventArgs e)
        {
            if (_newCaptureService == null) return;

            var logicalPos = e.GetPosition(this);
            var currentPhysical = ConvertLogicalToPhysicalNew(logicalPos);

            if (_isSelecting)
            {
                // Check drag threshold
                if (!_dragStarted)
                {
                    var dragDistance = Math.Sqrt(
                        Math.Pow(currentPhysical.X - _startPointPhysical.X, 2) +
                        Math.Pow(currentPhysical.Y - _startPointPhysical.Y, 2));

                    if (dragDistance >= DragThreshold)
                    {
                        _dragStarted = true;
                        DebugLog("INPUT", $"[NEW] Drag started (distance: {dragDistance:F2}px)");
                    }
                }

                // Update selection rectangle
                UpdateSelectionRectangleNew(currentPhysical);
            }
            else if (!_dragStarted)
            {
                // Window detection (keep existing logic)
                UpdateWindowSelection(currentPhysical);
            }
        }

        /// <summary>
        /// NEW: Handle pointer released with new backend.
        /// </summary>
        private void OnPointerReleasedNew(PointerReleasedEventArgs e)
        {
            if (!_isSelecting || _newCaptureService == null) return;

            _isSelecting = false;

            // If we didn't drag and have a hovered window, use that
            if (!_dragStarted && _hoveredWindow != null)
            {
                var rect = _hoveredWindow.Bounds;
                DebugLog("RESULT", $"[NEW] Selected window: {rect}");

                // Capture the window using new backend
                try
                {
                    // Convert physical rect to logical for capture service
                    var physicalRect = new PhysicalRectangle(rect.X, rect.Y, rect.Width, rect.Height);
                    var logicalRect = _newCaptureService.PhysicalToLogical(physicalRect);

                    var captureOptions = new RegionCaptureOptions
                    {
                        IncludeCursor = false
                    };

                    DebugLog("CAPTURE", $"[NEW] Capturing window: Physical={physicalRect}, Logical={logicalRect}");
                    var captureTask = _newCaptureService.CaptureRegionAsync(logicalRect, captureOptions);
                    captureTask.Wait();

                    _capturedBitmap = captureTask.Result;
                    DebugLog("CAPTURE", $"[NEW] Captured window bitmap: {_capturedBitmap?.Width}x{_capturedBitmap?.Height}");
                }
                catch (Exception ex)
                {
                    DebugLog("ERROR", $"[NEW] Window capture failed: {ex.Message}");
                    _capturedBitmap = null;
                }

                _tcs.TrySetResult(new SKRectI(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height));
                Close();
                return;
            }

            // Get final position
            var logicalPos = e.GetPosition(this);
            var currentPhysical = ConvertLogicalToPhysicalNew(logicalPos);

            DebugLog("MOUSE", $"[NEW] Released: Logical={logicalPos}, Physical={currentPhysical}");

            // Calculate selection rectangle
            var x = Math.Min(_startPointPhysical.X, currentPhysical.X);
            var y = Math.Min(_startPointPhysical.Y, currentPhysical.Y);
            var width = Math.Abs(_startPointPhysical.X - currentPhysical.X);
            var height = Math.Abs(_startPointPhysical.Y - currentPhysical.Y);

            DebugLog("RESULT", $"[NEW] Final selection: X={x}, Y={y}, W={width}, H={height}");

            // Ensure non-zero size
            if (width <= 0) width = 1;
            if (height <= 0) height = 1;

            // Capture the selected region using new backend
            try
            {
                // Convert physical rect to logical for capture service
                var physicalRect = new PhysicalRectangle(x, y, width, height);
                var logicalRect = _newCaptureService.PhysicalToLogical(physicalRect);

                var captureOptions = new RegionCaptureOptions
                {
                    IncludeCursor = false
                };

                DebugLog("CAPTURE", $"[NEW] Capturing region: Physical={physicalRect}, Logical={logicalRect}");

                // Synchronous capture (we're already on UI thread and about to close)
                var captureTask = _newCaptureService.CaptureRegionAsync(logicalRect, captureOptions);
                captureTask.Wait(); // Wait for capture to complete before closing

                _capturedBitmap = captureTask.Result;
                DebugLog("CAPTURE", $"[NEW] Captured bitmap: {_capturedBitmap?.Width}x{_capturedBitmap?.Height}");
            }
            catch (Exception ex)
            {
                DebugLog("ERROR", $"[NEW] Capture failed: {ex.Message}");
                _capturedBitmap = null;
            }

            // Return physical coordinates for backwards compatibility
            var resultRect = new SKRectI(x, y, x + width, y + height);
            _tcs.TrySetResult(resultRect);
            Close();
        }

        /// <summary>
        /// NEW: Update selection rectangle rendering.
        /// </summary>
        private void UpdateSelectionRectangleNew(SKPointI currentPhysical)
        {
            if (_newCaptureService == null) return;

            // Calculate physical rectangle
            var x = Math.Min(_startPointPhysical.X, currentPhysical.X);
            var y = Math.Min(_startPointPhysical.Y, currentPhysical.Y);
            var width = Math.Abs(_startPointPhysical.X - currentPhysical.X);
            var height = Math.Abs(_startPointPhysical.Y - currentPhysical.Y);

            var physicalRect = new PhysicalRectangle(x, y, width, height);

            // Convert to logical for rendering
            var logicalRect = _newCaptureService.PhysicalToLogical(physicalRect);

            // Convert to window-local coordinates
            var localX = logicalRect.X - _newVirtualDesktopLogical.X;
            var localY = logicalRect.Y - _newVirtualDesktopLogical.Y;

            // Update UI elements (keep existing rendering logic)
            UpdateSelectionVisuals(localX, localY, logicalRect.Width, logicalRect.Height);

            // Log for debugging
            var intersectingMonitors = _newMonitors
                .Where(m => m.Bounds.Intersect(physicalRect) != null)
                .ToArray();

            if (intersectingMonitors.Length > 1)
            {
                DebugLog("SELECTION", $"[NEW] Spanning {intersectingMonitors.Length} monitors:");
                foreach (var mon in intersectingMonitors)
                {
                    DebugLog("SELECTION", $"  - {mon.Name} @ {mon.ScaleFactor}x");
                }
            }
        }

        /// <summary>
        /// Helper to update visual elements (existing code can stay the same).
        /// </summary>
        private void UpdateSelectionVisuals(double x, double y, double width, double height)
        {
            var selectionBorder = this.FindControl<Avalonia.Controls.Shapes.Rectangle>("SelectionBorder");
            var selectionBorderInner = this.FindControl<Avalonia.Controls.Shapes.Rectangle>("SelectionBorderInner");
            var darkeningOverlay = this.FindControl<Avalonia.Controls.Shapes.Path>("DarkeningOverlay");
            var infoText = this.FindControl<Avalonia.Controls.TextBlock>("InfoText");

            if (selectionBorder != null)
            {
                selectionBorder.Width = width;
                selectionBorder.Height = height;
                Avalonia.Controls.Canvas.SetLeft(selectionBorder, x);
                Avalonia.Controls.Canvas.SetTop(selectionBorder, y);
                selectionBorder.IsVisible = true;
            }

            if (selectionBorderInner != null)
            {
                selectionBorderInner.Width = width;
                selectionBorderInner.Height = height;
                Avalonia.Controls.Canvas.SetLeft(selectionBorderInner, x);
                Avalonia.Controls.Canvas.SetTop(selectionBorderInner, y);
                selectionBorderInner.IsVisible = true;
            }

            // Update darkening overlay (keep existing logic)
            // Update info text (keep existing logic)
        }

        /// <summary>
        /// NEW: Cleanup on disposal.
        /// </summary>
        private void DisposeNewBackend()
        {
            if (_newCaptureService != null)
            {
                DebugLog("LIFECYCLE", "[NEW] Disposing capture service");
                _newCaptureService.Dispose();
                _newCaptureService = null;
            }
        }
    }
}
