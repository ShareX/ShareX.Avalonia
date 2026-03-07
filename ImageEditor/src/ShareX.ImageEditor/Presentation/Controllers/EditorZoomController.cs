using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using ShareX.ImageEditor.Presentation.ViewModels;
using ShareX.ImageEditor.Presentation.Views;

namespace ShareX.ImageEditor.Presentation.Controllers;

public class EditorZoomController
{
    private readonly EditorView _view;
    private bool _isPointerZooming;
    private double _lastZoom = 1.0;
    private bool _isPanning;
    private Point _panStart;
    private Vector _panOrigin;

    // Throttle zoom changes to prevent rapid consecutive events
    private DateTime _lastZoomChangeTime = DateTime.MinValue;
    private const int ZoomThrottleMilliseconds = 150; // Minimum time between zoom changes

    private const double MinZoom = 0.25;
    private const double MaxZoom = 4.0;
    private const double ZoomStep = 0.1;

    // Predefined zoom levels matching the dropdown
    private static readonly double[] ZoomLevels = { 0.25, 0.5, 0.75, 1.0, 1.25, 1.5, 2.0, 3.0, 4.0 };

    public EditorZoomController(EditorView view)
    {
        _view = view;
    }

    public void InitLastZoom(double zoom)
    {
        _lastZoom = zoom;
    }

    public void OnPreviewPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_view.DataContext is not MainViewModel vm) return;
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;

        // Throttle: Ignore events that come too quickly after the last zoom change
        var now = DateTime.UtcNow;
        if ((now - _lastZoomChangeTime).TotalMilliseconds < ZoomThrottleMilliseconds)
        {
            e.Handled = true;
            return;
        }

        var oldZoom = vm.Zoom;

        // Use Sign to get only the direction (1 or -1), ignoring the magnitude
        // This ensures we only move one zoom level at a time regardless of scroll speed settings
        var direction = Math.Sign(e.Delta.Y);

        // If delta is 0, do nothing
        if (direction == 0) return;

        // Find the current zoom level index or nearest one
        int currentIndex = FindNearestZoomLevelIndex(oldZoom);

        // Move to next or previous zoom level (only by 1)
        int newIndex = Math.Clamp(currentIndex + direction, 0, ZoomLevels.Length - 1);

        // If we're already at the min/max, don't do anything
        if (newIndex == currentIndex) return;

        var newZoom = ZoomLevels[newIndex];
        if (Math.Abs(newZoom - oldZoom) < 0.0001) return;

        // Update the last zoom change time
        _lastZoomChangeTime = now;

        var scrollViewer = _view.FindControl<ScrollViewer>("CanvasScrollViewer");
        if (scrollViewer != null)
        {
            var pointerPosition = e.GetPosition(scrollViewer);
            var offsetBefore = scrollViewer.Offset;
            if (scrollViewer.Extent.Width <= scrollViewer.Viewport.Width)
                offsetBefore = offsetBefore.WithX(0);
            if (scrollViewer.Extent.Height <= scrollViewer.Viewport.Height)
                offsetBefore = offsetBefore.WithY(0);
            var logicalPoint = new Vector(
               (offsetBefore.X + pointerPosition.X) / oldZoom,
               (offsetBefore.Y + pointerPosition.Y) / oldZoom);

            _isPointerZooming = true;
            _lastZoom = oldZoom;
            vm.Zoom = newZoom;

            Dispatcher.UIThread.Post(() =>
            {
                var targetOffset = new Vector(
                    logicalPoint.X * newZoom - pointerPosition.X,
                    logicalPoint.Y * newZoom - pointerPosition.Y);

                var maxX = Math.Max(0, scrollViewer.Extent.Width - scrollViewer.Viewport.Width);
                var maxY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);

                if (scrollViewer.Extent.Width <= scrollViewer.Viewport.Width)
                    targetOffset = targetOffset.WithX(0);
                if (scrollViewer.Extent.Height <= scrollViewer.Viewport.Height)
                    targetOffset = targetOffset.WithY(0);

                scrollViewer.Offset = new Vector(
                    Math.Clamp(targetOffset.X, 0, maxX),
                    Math.Clamp(targetOffset.Y, 0, maxY));
            }, DispatcherPriority.Render);
        }
        else
        {
            _lastZoom = oldZoom;
            vm.Zoom = newZoom;
        }

        _isPointerZooming = false;
        _lastZoom = vm.Zoom;
        e.Handled = true;
    }

    private static int FindNearestZoomLevelIndex(double currentZoom)
    {
        // Find the index of the zoom level closest to the current zoom
        int nearestIndex = 0;
        double minDifference = Math.Abs(ZoomLevels[0] - currentZoom);

        for (int i = 1; i < ZoomLevels.Length; i++)
        {
            double difference = Math.Abs(ZoomLevels[i] - currentZoom);
            if (difference < minDifference)
            {
                minDifference = difference;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    public void AdjustZoomToAnchor(double oldZoom, double newZoom, Point anchor)
    {
        var scrollViewer = _view.FindControl<ScrollViewer>("CanvasScrollViewer");
        if (scrollViewer == null || oldZoom <= 0) return;

        var offsetBefore = scrollViewer.Offset;
        if (scrollViewer.Extent.Width <= scrollViewer.Viewport.Width)
            offsetBefore = offsetBefore.WithX(0);
        if (scrollViewer.Extent.Height <= scrollViewer.Viewport.Height)
            offsetBefore = offsetBefore.WithY(0);
        var logicalPoint = new Vector(
            (offsetBefore.X + anchor.X) / oldZoom,
            (offsetBefore.Y + anchor.Y) / oldZoom);

        Dispatcher.UIThread.Post(() =>
        {
            var targetOffset = new Vector(
                logicalPoint.X * newZoom - anchor.X,
                logicalPoint.Y * newZoom - anchor.Y);

            var maxX = Math.Max(0, scrollViewer.Extent.Width - scrollViewer.Viewport.Width);
            var maxY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);

            if (scrollViewer.Extent.Width <= scrollViewer.Viewport.Width)
                targetOffset = targetOffset.WithX(0);
            if (scrollViewer.Extent.Height <= scrollViewer.Viewport.Height)
                targetOffset = targetOffset.WithY(0);

            scrollViewer.Offset = new Vector(
                Math.Clamp(targetOffset.X, 0, maxX),
                Math.Clamp(targetOffset.Y, 0, maxY));
        }, DispatcherPriority.Render);
    }

    public void CenterCanvasOnZoomChange()
    {
        var scrollViewer = _view.FindControl<ScrollViewer>("CanvasScrollViewer");
        if (scrollViewer == null) return;

        Dispatcher.UIThread.Post(() =>
        {
            var extent = scrollViewer.Extent;
            var viewport = scrollViewer.Viewport;
            var targetOffset = new Vector(
                Math.Max(0, (extent.Width - viewport.Width) / 2),
                Math.Max(0, (extent.Height - viewport.Height) / 2));

            scrollViewer.Offset = targetOffset;
        }, DispatcherPriority.Render);
    }

    public void OnScrollViewerPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer) return;

        var properties = e.GetCurrentPoint(scrollViewer).Properties;
        if (!properties.IsMiddleButtonPressed) return;

        _isPanning = true;
        _panStart = e.GetPosition(scrollViewer);
        _panOrigin = scrollViewer.Offset;
        scrollViewer.Cursor = new Cursor(StandardCursorType.SizeAll);
        e.Pointer.Capture(scrollViewer);
        e.Handled = true;
    }

    public void OnScrollViewerPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPanning || sender is not ScrollViewer scrollViewer) return;

        var current = e.GetPosition(scrollViewer);
        var delta = current - _panStart;

        var target = new Vector(
            _panOrigin.X - delta.X,
            _panOrigin.Y - delta.Y);

        var maxX = Math.Max(0, scrollViewer.Extent.Width - scrollViewer.Viewport.Width);
        var maxY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);

        scrollViewer.Offset = new Vector(
            Math.Clamp(target.X, 0, maxX),
            Math.Clamp(target.Y, 0, maxY));

        e.Handled = true;
    }

    public void OnScrollViewerPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer) return;

        if (_isPanning)
        {
            _isPanning = false;
            scrollViewer.Cursor = null;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    public void ResetScrollViewerOffset()
    {
        var scrollViewer = _view.FindControl<ScrollViewer>("CanvasScrollViewer");
        if (scrollViewer == null) return;

        Dispatcher.UIThread.Post(() => scrollViewer.Offset = new Vector(0, 0), DispatcherPriority.Render);
    }

    public void HandleZoomPropertyChanged(MainViewModel vm)
    {
        if (!_isPointerZooming)
        {
            var scrollViewer = _view.FindControl<ScrollViewer>("CanvasScrollViewer");
            if (scrollViewer != null)
            {
                var anchor = new Point(scrollViewer.Viewport.Width / 2, scrollViewer.Viewport.Height / 2);
                AdjustZoomToAnchor(_lastZoom, vm.Zoom, anchor);
            }
            _lastZoom = vm.Zoom;
        }
        else
        {
            _lastZoom = vm.Zoom;
        }
    }

    public void ZoomToFit()
    {
        if (_view.DataContext is not MainViewModel vm || !vm.HasPreviewImage)
        {
            return;
        }

        var scrollViewer = _view.FindControl<ScrollViewer>("CanvasScrollViewer");
        var previewFrame = _view.FindControl<Border>("PreviewFrame");
        if (scrollViewer == null || previewFrame == null)
        {
            return;
        }

        // PreviewFrame includes canvas padding/smart padding, so this fits the visible output.
        double contentWidth = previewFrame.Bounds.Width;
        double contentHeight = previewFrame.Bounds.Height;

        if (contentWidth <= 0 || contentHeight <= 0)
        {
            contentWidth = vm.ImageWidth;
            contentHeight = vm.ImageHeight;
        }

        if (contentWidth <= 0 || contentHeight <= 0)
        {
            return;
        }

        const double margin = 24;
        double availableWidth = Math.Max(1, scrollViewer.Viewport.Width - margin);
        double availableHeight = Math.Max(1, scrollViewer.Viewport.Height - margin);

        double fitZoom = Math.Min(availableWidth / contentWidth, availableHeight / contentHeight);
        fitZoom = Math.Clamp(fitZoom, MinZoom, MaxZoom);

        _lastZoom = vm.Zoom;
        vm.Zoom = fitZoom;
        CenterCanvasOnZoomChange();
    }
}
