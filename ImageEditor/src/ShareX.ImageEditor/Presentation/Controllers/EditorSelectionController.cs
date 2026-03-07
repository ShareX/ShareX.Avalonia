using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ShareX.ImageEditor.Core.Annotations;
using ShareX.ImageEditor.Presentation.Controls;
using ShareX.ImageEditor.Presentation.Rendering;
using ShareX.ImageEditor.Presentation.ViewModels;
using ShareX.ImageEditor.Presentation.Views;
using SkiaSharp;

namespace ShareX.ImageEditor.Presentation.Controllers;

public class EditorSelectionController
{
    private readonly EditorView _view;
    private Control? _selectedShape;
    private List<Control> _selectionHandles = new();
    private bool _isDraggingHandle;
    private Control? _draggedHandle;
    private Point _lastDragPoint;
    private Point _startPoint; // Used for resizing deltas
    private bool _isDraggingShape;
    private global::Avalonia.Controls.Shapes.Line? _rotationLine; // Dotted line connecting top-center to rotation handle

    // Reactive bounds tracking for text annotations
    private Control? _observedShape;
    private EventHandler<AvaloniaPropertyChangedEventArgs>? _boundsHandler;

    // Hover tracking for ant lines
    private Control? _hoveredShape;
    private global::Avalonia.Controls.Shapes.Rectangle? _hoverOutlineBlack;
    private global::Avalonia.Controls.Shapes.Rectangle? _hoverOutlineWhite;
    private global::Avalonia.Controls.Shapes.Polyline? _hoverPolylineBlack;
    private global::Avalonia.Controls.Shapes.Polyline? _hoverPolylineWhite;
    private global::Avalonia.Controls.Shapes.Ellipse? _hoverEllipseBlack;
    private global::Avalonia.Controls.Shapes.Ellipse? _hoverEllipseWhite;

    // Store arrow/line endpoints for editing
    private Dictionary<Control, (Point Start, Point End)> _shapeEndpoints = new();
    private Dictionary<Control, Point> _speechBalloonTailPoints = new();
    private TextBox? _balloonTextEditor;

    public Control? SelectedShape => _selectedShape;

    // Event invoked when visual needs update (for Effects)
    public event Action<Control>? RequestUpdateEffect;

    // Event invoked when selection state changes (shape selected or deselected)
    public event Action<bool>? SelectionChanged;

    public EditorSelectionController(EditorView view)
    {
        _view = view;
    }

    public void ClearSelection()
    {
        var wasSelected = _selectedShape != null;
        _selectedShape = null;
        _isDraggingHandle = false;
        _draggedHandle = null;
        _isDraggingShape = false;
        UpdateBoundsObserver(); // Clear observer
        ClearHoverOutline();
        UpdateSelectionHandles();
        if (wasSelected)
        {
            SelectionChanged?.Invoke(false);
        }
    }

    public void SetSelectedShape(Control shape)
    {
        var wasNull = _selectedShape == null;
        _selectedShape = shape;
        UpdateBoundsObserver();
        // Set the hovered shape to the selected shape so ant lines appear
        _hoveredShape = shape;
        UpdateHoverOutline();
        UpdateSelectionHandles();

        // Notify selection changed
        if (wasNull && shape != null)
        {
            SelectionChanged?.Invoke(true);
        }

        // Auto-enter text edit mode for speech balloon
        if (shape is SpeechBalloonControl balloonControl)
        {
            var canvas = _view.FindControl<Canvas>("AnnotationCanvas");
            if (canvas != null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    ShowSpeechBalloonTextEditor(balloonControl, canvas);
                }, DispatcherPriority.Normal);
            }
        }
    }

    public bool OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        var canvas = _view.FindControl<Canvas>("AnnotationCanvas");
        if (canvas == null) return false;

        var point = e.GetPosition(canvas);
        var props = e.GetCurrentPoint(canvas).Properties;

        // Check if clicked on a handle
        var overlay = _view.FindControl<Canvas>("OverlayCanvas");
        if (overlay != null)
        {
            var handleSource = e.Source as Control;
            if (handleSource != null && overlay.Children.Contains(handleSource) && handleSource is Border)
            {
                _isDraggingHandle = true;
                _draggedHandle = handleSource;
                _startPoint = point; // Capture start for resize delta
                e.Pointer.Capture(handleSource);
                e.Handled = true;
                return true;
            }
        }

        // Check if clicked on an existing shape (Select or Drag)
        // If we are in Select tool OR user holds Ctrl (multi-select not impl yet) or just clicking existing shapes to move
        // BUT: If active tool is a drawing tool, we usually prioritize drawing NEW shapes unless we click ON a selected shape?
        // ShareX logic: If in Select Tool, selecting works. If in other tools, usually drawing takes precedence unless we click a handle.
        // But logic in EditorView.axaml.cs had: 
        // if (vm.ActiveTool == EditorTool.Select ...) -> Try select.

        if (_view.DataContext is MainViewModel vm)
        {
            // Allow dragging selected shapes even when not in Select tool mode
            // This enables immediate repositioning after creating an annotation
            if (_selectedShape != null && vm.ActiveTool != EditorTool.Select)
            {
                // Hit test - find the direct child of the canvas
                var hitSource = e.Source as global::Avalonia.Visual;
                Control? hitTarget = null;
                while (hitSource != null && hitSource != canvas)
                {
                    var candidate = hitSource as Control;
                    if (candidate != null && canvas.Children.Contains(candidate))
                    {
                        hitTarget = candidate;
                        break;
                    }
                    hitSource = hitSource.GetVisualParent();
                }

                // Fallback: Use manual hit test if strict visual hit test failed (e.g. thin lines)
                if (hitTarget != _selectedShape)
                {
                    var manualHit = HitTestShape(canvas, point);
                    if (manualHit == _selectedShape)
                    {
                        hitTarget = manualHit;
                    }
                }

                if (hitTarget == _selectedShape)
                {
                    if (hitTarget is OutlinedTextControl otc && e.ClickCount == 2)
                    {
                        ShowTextEditor(otc, canvas);
                        e.Handled = true;
                        return true;
                    }

                    if (hitTarget is SpeechBalloonControl balloon && e.ClickCount == 2)
                    {
                        ShowSpeechBalloonTextEditor(balloon, canvas);
                        e.Handled = true;
                        return true;
                    }

                    _isDraggingShape = true;
                    _lastDragPoint = point;
                    UpdateSelectionHandles();
                    e.Pointer.Capture(hitTarget);
                    e.Handled = true;
                    return true;
                }
            }

            if (vm.ActiveTool == EditorTool.Select || vm.ActiveTool == EditorTool.Spotlight)
            {
                // Hit test
                var hitSource = e.Source as global::Avalonia.Visual;
                Control? hitTarget = null;
                while (hitSource != null && hitSource != canvas)
                {
                    var candidate = hitSource as Control;
                    if (candidate != null && canvas.Children.Contains(candidate))
                    {
                        hitTarget = candidate;
                        break;
                    }
                    hitSource = hitSource.GetVisualParent();
                }

                var manualHit = HitTestShape(canvas, point);
                if (manualHit is SpotlightControl || manualHit is OutlinedTextControl)
                {
                    hitTarget = manualHit;
                }

                // Integrity Check: Ensure we didn't mistakenly hit the Spotlight overlay (visual pass-through)
                if (hitTarget is SpotlightControl sc && manualHit != sc)
                {
                    hitTarget = null;
                }

                if (vm.ActiveTool == EditorTool.Spotlight)
                {
                    if (!(hitTarget is SpotlightControl)) hitTarget = null;
                    if (!(manualHit is SpotlightControl)) manualHit = null;
                }

                if (hitTarget != null)
                {
                    if (hitTarget is OutlinedTextControl otc && e.ClickCount == 2)
                    {
                        ShowTextEditor(otc, canvas);
                        e.Handled = true;
                        return true;
                    }

                    if (hitTarget is SpeechBalloonControl balloon && e.ClickCount == 2)
                    {
                        ShowSpeechBalloonTextEditor(balloon, canvas);
                        e.Handled = true;
                        return true;
                    }

                    _selectedShape = hitTarget;
                    UpdateBoundsObserver();
                    _isDraggingShape = true;
                    _lastDragPoint = point;
                    UpdateSelectionHandles();
                    SelectionChanged?.Invoke(true);
                    e.Pointer.Capture(hitTarget);
                    e.Handled = true;
                    return true;
                }
                else
                {
                    if (manualHit != null)
                    {
                        if (manualHit is OutlinedTextControl otc && e.ClickCount == 2)
                        {
                            ShowTextEditor(otc, canvas);
                            e.Handled = true;
                            return true;
                        }

                        if (manualHit is SpeechBalloonControl balloon && e.ClickCount == 2)
                        {
                            ShowSpeechBalloonTextEditor(balloon, canvas);
                            e.Handled = true;
                            return true;
                        }

                        _selectedShape = manualHit;
                        UpdateBoundsObserver();
                        _isDraggingShape = true;
                        _lastDragPoint = point;
                        UpdateSelectionHandles();
                        SelectionChanged?.Invoke(true);
                        e.Pointer.Capture(manualHit);
                        e.Handled = true;
                        return true;
                    }

                    ClearSelection();
                    // Don't return true, allowing rubber band selection (if implemented) or just clearing
                    return false;
                }
            }
        }

        return false;
    }

    public bool OnPointerMoved(object sender, PointerEventArgs e)
    {
        var canvas = _view.FindControl<Canvas>("AnnotationCanvas");
        if (canvas == null) return false;

        var currentPoint = e.GetPosition(canvas);

        if (_isDraggingHandle && _draggedHandle != null && _selectedShape != null)
        {
            HandleResize(currentPoint, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
            e.Handled = true;
            return true;
        }

        if (_isDraggingShape && _selectedShape != null)
        {
            HandleMove(currentPoint);
            e.Handled = true;
            return true;
        }

        // Update hover state when not dragging
        UpdateHoverState(canvas, currentPoint);

        return false;
    }

    public bool OnPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (_isDraggingHandle)
        {
            _isDraggingHandle = false;
            _draggedHandle = null;
            e.Pointer.Capture(null);

            if (_selectedShape?.Tag is BaseEffectAnnotation)
            {
                RequestUpdateEffect?.Invoke(_selectedShape);
            }
            return true;
        }

        if (_isDraggingShape)
        {
            _isDraggingShape = false;
            e.Pointer.Capture(null);

            if (_selectedShape?.Tag is BaseEffectAnnotation)
            {
                RequestUpdateEffect?.Invoke(_selectedShape);
            }
            return true;
        }

        return false;
    }

    private void HandleResize(Point currentPoint, bool isShiftHeld = false)
    {
        if (_selectedShape == null || _draggedHandle == null)
        {
            return;
        }

        var handleTag = _draggedHandle.Tag?.ToString();
        if (string.IsNullOrEmpty(handleTag))
        {
            return;
        }

        var deltaX = currentPoint.X - _startPoint.X;
        var deltaY = currentPoint.Y - _startPoint.Y;

        // Special handling for Line endpoints
        if (_selectedShape is global::Avalonia.Controls.Shapes.Line targetLine)
        {
            if (handleTag == "LineStart")
            {
                var snappedStart = isShiftHeld
                    ? EditorInputController.SnapTo45Degrees(targetLine.EndPoint, currentPoint)
                    : currentPoint;
                targetLine.StartPoint = snappedStart;
            }
            else if (handleTag == "LineEnd")
            {
                var snappedEnd = isShiftHeld
                    ? EditorInputController.SnapTo45Degrees(targetLine.StartPoint, currentPoint)
                    : currentPoint;
                targetLine.EndPoint = snappedEnd;
            }

            // Sync annotation points for hit testing
            if (targetLine.Tag is LineAnnotation lineAnnotation)
            {
                lineAnnotation.StartPoint = new SKPoint((float)targetLine.StartPoint.X, (float)targetLine.StartPoint.Y);
                lineAnnotation.EndPoint = new SKPoint((float)targetLine.EndPoint.X, (float)targetLine.EndPoint.Y);
            }

            _startPoint = currentPoint;
            UpdateSelectionHandles();
            return;
        }

        // Special handling for Arrow endpoints
        if (_selectedShape is global::Avalonia.Controls.Shapes.Path arrowPath)
        {
            if (_shapeEndpoints.TryGetValue(arrowPath, out var endpoints))
            {
                Point arrowStart = endpoints.Start;
                Point arrowEnd = endpoints.End;

                if (handleTag == "ArrowStart")
                {
                    arrowStart = isShiftHeld
                        ? EditorInputController.SnapTo45Degrees(arrowEnd, currentPoint)
                        : currentPoint;
                }
                else if (handleTag == "ArrowEnd")
                {
                    arrowEnd = isShiftHeld
                        ? EditorInputController.SnapTo45Degrees(arrowStart, currentPoint)
                        : currentPoint;
                }

                _shapeEndpoints[arrowPath] = (arrowStart, arrowEnd);

                // Sync annotation points for hit testing
                if (arrowPath.Tag is ArrowAnnotation arrowAnnotation)
                {
                    arrowAnnotation.StartPoint = new SKPoint((float)arrowStart.X, (float)arrowStart.Y);
                    arrowAnnotation.EndPoint = new SKPoint((float)arrowEnd.X, (float)arrowEnd.Y);
                    AnnotationVisualFactory.UpdateVisualControl(arrowPath, arrowAnnotation);
                }
            }
            _startPoint = currentPoint;
            UpdateSelectionHandles();
            return;
        }

        // Special handling for SpeechBalloonControl tail dragging
        if (_selectedShape is SpeechBalloonControl balloonControl && balloonControl.Annotation is SpeechBalloonAnnotation balloon && handleTag == "BalloonTail")
        {
            balloon.TailPoint = new SKPoint((float)currentPoint.X, (float)currentPoint.Y);
            balloonControl.InvalidateVisual();
            _startPoint = currentPoint;
            UpdateSelectionHandles();
            return;
        }

        // Rotation handling for OutlinedTextControl (Text annotation)
        if (_selectedShape is OutlinedTextControl rotateTextBox && handleTag == "Rotate")
        {
            if (rotateTextBox.Tag is TextAnnotation rotateTextAnn)
            {
                var textRect = GetLogicalRect(rotateTextBox);
                var tbLeft = textRect.Left;
                var tbTop = textRect.Top;
                var tbWidth = textRect.Width;
                var tbHeight = textRect.Height;

                double cx = tbLeft + tbWidth / 2;
                double cy = tbTop + tbHeight / 2;

                double dx = currentPoint.X - cx;
                double dy = currentPoint.Y - cy;
                double angleRad = Math.Atan2(dx, -dy); // 0 = up, clockwise positive
                double angleDeg = angleRad * 180.0 / Math.PI;

                rotateTextAnn.RotationAngle = (float)angleDeg;

                rotateTextBox.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
                rotateTextBox.RenderTransform = new RotateTransform(angleDeg);
            }
            _startPoint = currentPoint;
            UpdateSelectionHandles();
            return;
        }

        // Regular shapes
        var shapeRect = GetLogicalRect(_selectedShape);
        var left = shapeRect.Left;
        var top = shapeRect.Top;
        var width = shapeRect.Width;
        var height = shapeRect.Height;

        // Special handling for SpeechBalloonControl resizing
        if (_selectedShape is SpeechBalloonControl resizeBalloonControl && resizeBalloonControl.Annotation is SpeechBalloonAnnotation resizeBalloon)
        {
            double newLeft = left;
            double newTop = top;
            double newWidth = width;
            double newHeight = height;

            if (handleTag.Contains("Right")) newWidth = Math.Max(20, width + deltaX);
            else if (handleTag.Contains("Left")) { var change = Math.Min(width - 20, deltaX); newLeft += change; newWidth -= change; }

            if (handleTag.Contains("Bottom")) newHeight = Math.Max(20, height + deltaY);
            else if (handleTag.Contains("Top")) { var change = Math.Min(height - 20, deltaY); newTop += change; newHeight -= change; }

            resizeBalloon.StartPoint = ToSKPoint(new Point(newLeft, newTop));
            resizeBalloon.EndPoint = ToSKPoint(new Point(newLeft + newWidth, newTop + newHeight));

            Canvas.SetLeft(resizeBalloonControl, newLeft);
            Canvas.SetTop(resizeBalloonControl, newTop);
            resizeBalloonControl.Width = newWidth;
            resizeBalloonControl.Height = newHeight;
            resizeBalloonControl.InvalidateVisual();

            _startPoint = currentPoint;
            UpdateSelectionHandles();
            return;
        }

        if (_selectedShape is ShareX.ImageEditor.Presentation.Controls.SpotlightControl spotlight && spotlight.Annotation is SpotlightAnnotation sa)
        {
            var bounds = sa.GetBounds();
            var newLeft = bounds.Left;
            var newTop = bounds.Top;
            var newWidth = bounds.Width;
            var newHeight = bounds.Height;

            float dx = (float)deltaX;
            float dy = (float)deltaY;

            if (handleTag.Contains("Right")) newWidth = Math.Max(10, newWidth + dx);
            else if (handleTag.Contains("Left")) { var change = Math.Min(newWidth - 10, dx); newLeft += change; newWidth -= change; }

            if (handleTag.Contains("Bottom")) newHeight = Math.Max(10, newHeight + dy);
            else if (handleTag.Contains("Top")) { var change = Math.Min(newHeight - 10, dy); newTop += change; newHeight -= change; }

            sa.StartPoint = ToSKPoint(new Point(newLeft, newTop));
            sa.EndPoint = ToSKPoint(new Point(newLeft + newWidth, newTop + newHeight));
            spotlight.InvalidateVisual();

            _startPoint = currentPoint;
            UpdateSelectionHandles();
            return;
        }

        if (_selectedShape is global::Avalonia.Controls.Shapes.Rectangle || _selectedShape is global::Avalonia.Controls.Shapes.Ellipse || _selectedShape is Grid || _selectedShape is OutlinedTextControl)
        {
            double newLeft = left;
            double newTop = top;
            double newWidth = width;
            double newHeight = height;

            if (handleTag.Contains("Right")) newWidth = Math.Max(1, width + deltaX);
            else if (handleTag.Contains("Left")) { var change = Math.Min(width - 1, deltaX); newLeft += change; newWidth -= change; }

            if (handleTag.Contains("Bottom")) newHeight = Math.Max(1, height + deltaY);
            else if (handleTag.Contains("Top")) { var change = Math.Min(height - 1, deltaY); newTop += change; newHeight -= change; }

            Canvas.SetLeft(_selectedShape, newLeft);
            Canvas.SetTop(_selectedShape, newTop);
            _selectedShape.Width = newWidth;
            _selectedShape.Height = newHeight;

            // Sync annotation points for hit testing
            if (_selectedShape.Tag is Annotation annotation)
            {
                annotation.StartPoint = new SKPoint((float)newLeft, (float)newTop);
                annotation.EndPoint = new SKPoint((float)(newLeft + newWidth), (float)(newTop + newHeight));
            }
        }

        _startPoint = currentPoint;
        UpdateSelectionHandles();

        // Real-time effect update during resize
        if (_selectedShape?.Tag is BaseEffectAnnotation)
        {
            RequestUpdateEffect?.Invoke(_selectedShape);
        }
    }

    public void MoveSelectedShape(double deltaX, double deltaY)
    {
        if (_selectedShape == null) return;

        _lastDragPoint = new Point(0, 0);
        HandleMove(new Point(deltaX, deltaY));

        if (_selectedShape?.Tag is BaseEffectAnnotation)
        {
            RequestUpdateEffect?.Invoke(_selectedShape);
        }
    }

    private void HandleMove(Point currentPoint)
    {
        if (_selectedShape == null)
        {
            return;
        }

        var deltaX = currentPoint.X - _lastDragPoint.X;
        var deltaY = currentPoint.Y - _lastDragPoint.Y;

        if (_selectedShape is global::Avalonia.Controls.Shapes.Line targetLine)
        {
            targetLine.StartPoint = new Point(targetLine.StartPoint.X + deltaX, targetLine.StartPoint.Y + deltaY);
            targetLine.EndPoint = new Point(targetLine.EndPoint.X + deltaX, targetLine.EndPoint.Y + deltaY);

            // Sync annotation points for hit testing
            if (targetLine.Tag is LineAnnotation lineAnnotation)
            {
                lineAnnotation.StartPoint = new SKPoint((float)targetLine.StartPoint.X, (float)targetLine.StartPoint.Y);
                lineAnnotation.EndPoint = new SKPoint((float)targetLine.EndPoint.X, (float)targetLine.EndPoint.Y);
            }

            _lastDragPoint = currentPoint;
            UpdateSelectionHandles();
            return;
        }

        if (_selectedShape is global::Avalonia.Controls.Shapes.Path arrowPath && _shapeEndpoints.TryGetValue(arrowPath, out var endpoints))
        {
            var newStart = new Point(endpoints.Start.X + deltaX, endpoints.Start.Y + deltaY);
            var newEnd = new Point(endpoints.End.X + deltaX, endpoints.End.Y + deltaY);

            _shapeEndpoints[arrowPath] = (newStart, newEnd);

            // Sync annotation points for hit testing
            if (arrowPath.Tag is ArrowAnnotation arrowAnnotation)
            {
                arrowAnnotation.StartPoint = new SKPoint((float)newStart.X, (float)newStart.Y);
                arrowAnnotation.EndPoint = new SKPoint((float)newEnd.X, (float)newEnd.Y);
                AnnotationVisualFactory.UpdateVisualControl(arrowPath, arrowAnnotation);
            }

            _lastDragPoint = currentPoint;
            UpdateSelectionHandles();
            return;
        }

        if (_selectedShape is SpeechBalloonControl balloonControl && balloonControl.Annotation is SpeechBalloonAnnotation balloon)
        {
            var currentStart = balloon.StartPoint;
            var currentEnd = balloon.EndPoint;

            var newStartPoint = new SKPoint(currentStart.X + (float)deltaX, currentStart.Y + (float)deltaY);
            var newEndPoint = new SKPoint(currentEnd.X + (float)deltaX, currentEnd.Y + (float)deltaY);

            balloon.StartPoint = newStartPoint;
            balloon.EndPoint = newEndPoint;

            balloon.TailPoint = new SKPoint(balloon.TailPoint.X + (float)deltaX, balloon.TailPoint.Y + (float)deltaY);

            var newLeft = Canvas.GetLeft(balloonControl) + deltaX;
            var newTop = Canvas.GetTop(balloonControl) + deltaY;
            Canvas.SetLeft(balloonControl, newLeft);
            Canvas.SetTop(balloonControl, newTop);

            balloonControl.InvalidateVisual();
            _lastDragPoint = currentPoint;
            UpdateSelectionHandles();
            return;
        }

        // Handle Path (Freehand/SmartEraser) movement by translating all points
        if (_selectedShape is global::Avalonia.Controls.Shapes.Path path && path.Tag is FreehandAnnotation freehand)
        {
            // Update all points in the annotation
            for (int i = 0; i < freehand.Points.Count; i++)
            {
                var oldPt = freehand.Points[i];
                freehand.Points[i] = new SKPoint(oldPt.X + (float)deltaX, oldPt.Y + (float)deltaY);
            }

            // Regenerate the path geometry using the new points
            path.Data = freehand.CreateSmoothedGeometry();

            // Force visual update
            path.InvalidateVisual();
            _lastDragPoint = currentPoint;
            UpdateSelectionHandles();
            return;
        }

        if (_selectedShape is SpotlightControl spotlight && spotlight.Annotation is SpotlightAnnotation sa)
        {
            var currentStart = sa.StartPoint;
            var currentEnd = sa.EndPoint;
            sa.StartPoint = new SKPoint(currentStart.X + (float)deltaX, currentStart.Y + (float)deltaY);
            sa.EndPoint = new SKPoint(currentEnd.X + (float)deltaX, currentEnd.Y + (float)deltaY);
            spotlight.InvalidateVisual();

            _lastDragPoint = currentPoint;
            UpdateSelectionHandles();
            UpdateHoverOutline();
            return;
        }

        var left = Canvas.GetLeft(_selectedShape);
        var top = Canvas.GetTop(_selectedShape);
        Canvas.SetLeft(_selectedShape, left + deltaX);
        Canvas.SetTop(_selectedShape, top + deltaY);

        // Sync annotation points for hit testing (Rectangle, Ellipse, Highlight, etc.)
        if (_selectedShape.Tag is Annotation annotation)
        {
            annotation.StartPoint = new SKPoint(annotation.StartPoint.X + (float)deltaX, annotation.StartPoint.Y + (float)deltaY);
            annotation.EndPoint = new SKPoint(annotation.EndPoint.X + (float)deltaX, annotation.EndPoint.Y + (float)deltaY);
        }

        _lastDragPoint = currentPoint;
        UpdateSelectionHandles();

        // Real-time effect update during move
        if (_selectedShape?.Tag is BaseEffectAnnotation)
        {
            RequestUpdateEffect?.Invoke(_selectedShape);
        }
    }

    public void UpdateSelectionHandles()
    {
        var overlay = _view.FindControl<Canvas>("OverlayCanvas");
        if (overlay == null) return;

        foreach (var handle in _selectionHandles)
        {
            overlay.Children.Remove(handle);
        }
        _selectionHandles.Clear();

        if (_selectedShape == null) return;

        if (_selectedShape is global::Avalonia.Controls.Shapes.Line line)
        {
            CreateHandle(line.StartPoint.X, line.StartPoint.Y, "LineStart");
            CreateHandle(line.EndPoint.X, line.EndPoint.Y, "LineEnd");
            UpdateHoverOutline();
            return;
        }

        if (_selectedShape is global::Avalonia.Controls.Shapes.Path arrowPath)
        {
            if (_shapeEndpoints.TryGetValue(arrowPath, out var endpoints))
            {
                CreateHandle(endpoints.Start.X, endpoints.Start.Y, "ArrowStart");
                CreateHandle(endpoints.End.X, endpoints.End.Y, "ArrowEnd");
            }
            UpdateHoverOutline();
            return;
        }

        if (_selectedShape is SpeechBalloonControl balloonControl && balloonControl.Annotation is SpeechBalloonAnnotation balloon)
        {
            var balloonRect = GetLogicalRect(_selectedShape);
            var balloonLeft = balloonRect.Left;
            var balloonTop = balloonRect.Top;
            var balloonWidth = balloonRect.Width;
            var balloonHeight = balloonRect.Height;

            CreateHandle(balloonLeft, balloonTop, "TopLeft");
            CreateHandle(balloonLeft + balloonWidth / 2, balloonTop, "TopCenter");
            CreateHandle(balloonLeft + balloonWidth, balloonTop, "TopRight");
            CreateHandle(balloonLeft + balloonWidth, balloonTop + balloonHeight / 2, "RightCenter");
            CreateHandle(balloonLeft + balloonWidth, balloonTop + balloonHeight, "BottomRight");
            CreateHandle(balloonLeft + balloonWidth / 2, balloonTop + balloonHeight, "BottomCenter");
            CreateHandle(balloonLeft, balloonTop + balloonHeight, "BottomLeft");
            CreateHandle(balloonLeft, balloonTop + balloonHeight / 2, "LeftCenter");

            if (balloon.TailPoint.X == 0 && balloon.TailPoint.Y == 0)
            {
                balloon.TailPoint = new SKPoint(
                    balloon.StartPoint.X + (balloon.EndPoint.X - balloon.StartPoint.X) / 2,
                    balloon.EndPoint.Y + 30
                );
                balloonControl.InvalidateVisual();
            }

            var tailX = (double)balloon.TailPoint.X;
            var tailY = (double)balloon.TailPoint.Y;
            CreateHandle(tailX, tailY, "BalloonTail");
            UpdateHoverOutline();
            return;
        }

        if (_selectedShape is Polyline)
        {
            UpdateHoverOutline();
            return;
        }

        // OutlinedTextControl (Text annotation): resize handles + rotation handle
        if (_selectedShape is OutlinedTextControl textBox)
        {
            var textRect = GetLogicalRect(textBox);
            var tbLeft = textRect.Left;
            var tbTop = textRect.Top;
            var tbWidth = textRect.Width;
            var tbHeight = textRect.Height;

            if (double.IsNaN(tbWidth) || tbWidth <= 0) tbWidth = 20;
            if (double.IsNaN(tbHeight) || tbHeight <= 0) tbHeight = 20;

            // Get rotation angle from the annotation
            double rotAngle = 0;
            if (textBox.Tag is TextAnnotation textAnn)
            {
                rotAngle = textAnn.RotationAngle;
            }

            double cx = tbLeft + tbWidth / 2;
            double cy = tbTop + tbHeight / 2;

            // Helper to rotate a point around center
            Point RotPt(double x, double y)
            {
                if (rotAngle == 0) return new Point(x, y);
                double rad = rotAngle * Math.PI / 180.0;
                double cos = Math.Cos(rad);
                double sin = Math.Sin(rad);
                double dx = x - cx;
                double dy = y - cy;
                return new Point(cx + dx * cos - dy * sin, cy + dx * sin + dy * cos);
            }

            // 8 resize handles at rotated positions
            var tl = RotPt(tbLeft, tbTop);
            var tc = RotPt(tbLeft + tbWidth / 2, tbTop);
            var tr = RotPt(tbLeft + tbWidth, tbTop);
            var rc = RotPt(tbLeft + tbWidth, tbTop + tbHeight / 2);
            var br = RotPt(tbLeft + tbWidth, tbTop + tbHeight);
            var bc = RotPt(tbLeft + tbWidth / 2, tbTop + tbHeight);
            var bl = RotPt(tbLeft, tbTop + tbHeight);
            var lc = RotPt(tbLeft, tbTop + tbHeight / 2);

            CreateHandle(tl.X, tl.Y, "TopLeft");
            CreateHandle(tc.X, tc.Y, "TopCenter");
            CreateHandle(tr.X, tr.Y, "TopRight");
            CreateHandle(rc.X, rc.Y, "RightCenter");
            CreateHandle(br.X, br.Y, "BottomRight");
            CreateHandle(bc.X, bc.Y, "BottomCenter");
            CreateHandle(bl.X, bl.Y, "BottomLeft");
            CreateHandle(lc.X, lc.Y, "LeftCenter");

            // Rotation handle: dotted line from top-center going up, then a rotation node
            var rotLineStart = tc;
            var rotHandlePos = RotPt(tbLeft + tbWidth / 2, tbTop - 30);

            // Draw dotted line connector at rotated positions
            if (overlay != null)
            {
                _rotationLine = new global::Avalonia.Controls.Shapes.Line
                {
                    StartPoint = rotLineStart,
                    EndPoint = rotHandlePos,
                    Stroke = new SolidColorBrush(Color.FromRgb(30, 144, 255)), // DodgerBlue
                    StrokeThickness = 1.5,
                    StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 4, 4 },
                    IsHitTestVisible = false
                };
                overlay.Children.Add(_rotationLine);
                _selectionHandles.Add(_rotationLine);
            }

            CreateHandle(rotHandlePos.X, rotHandlePos.Y, "Rotate");

            UpdateHoverOutline();
            return;
        }

        if (_selectedShape is ShareX.ImageEditor.Presentation.Controls.SpotlightControl spotlightControl && spotlightControl.Annotation is SpotlightAnnotation spotlightAnn)
        {
            var bounds = spotlightAnn.GetBounds();
            CreateHandle(bounds.Left, bounds.Top, "TopLeft");
            CreateHandle(bounds.Left + bounds.Width / 2, bounds.Top, "TopCenter");
            CreateHandle(bounds.Right, bounds.Top, "TopRight");
            CreateHandle(bounds.Right, bounds.Top + bounds.Height / 2, "RightCenter");
            CreateHandle(bounds.Right, bounds.Bottom, "BottomRight");
            CreateHandle(bounds.Left + bounds.Width / 2, bounds.Bottom, "BottomCenter");
            CreateHandle(bounds.Left, bounds.Bottom, "BottomLeft");
            CreateHandle(bounds.Left, bounds.Top + bounds.Height / 2, "LeftCenter");

            UpdateHoverOutline();
            return;
        }

        if (_selectedShape is Grid)
        {
            UpdateHoverOutline();
            return;
        }

        var boundsRect = GetLogicalRect(_selectedShape);
        var width = boundsRect.Width;
        var height = boundsRect.Height;

        if (width <= 0 || height <= 0) return;

        var shapeLeft = boundsRect.Left;
        var shapeTop = boundsRect.Top;

        CreateHandle(shapeLeft, shapeTop, "TopLeft");
        CreateHandle(shapeLeft + width / 2, shapeTop, "TopCenter");
        CreateHandle(shapeLeft + width, shapeTop, "TopRight");
        CreateHandle(shapeLeft + width, shapeTop + height / 2, "RightCenter");
        CreateHandle(shapeLeft + width, shapeTop + height, "BottomRight");
        CreateHandle(shapeLeft + width / 2, shapeTop + height, "BottomCenter");
        CreateHandle(shapeLeft, shapeTop + height, "BottomLeft");
        CreateHandle(shapeLeft, shapeTop + height / 2, "LeftCenter");
        UpdateHoverOutline();
    }

    private void CreateHandle(double x, double y, string tag)
    {
        var overlay = _view.FindControl<Canvas>("OverlayCanvas");
        if (overlay == null) return;

        Cursor cursor = Cursor.Parse("Hand");
        if (tag.Contains("TopLeft") || tag.Contains("BottomRight")) cursor = new Cursor(StandardCursorType.TopLeftCorner);
        else if (tag.Contains("TopRight") || tag.Contains("BottomLeft")) cursor = new Cursor(StandardCursorType.TopRightCorner);
        else if (tag.Contains("Top") || tag.Contains("Bottom")) cursor = new Cursor(StandardCursorType.SizeNorthSouth);
        else if (tag.Contains("Left") || tag.Contains("Right")) cursor = new Cursor(StandardCursorType.SizeWestEast);

        var handleBorder = new Border
        {
            Width = 15,
            Height = 15,
            CornerRadius = new CornerRadius(10),
            Background = Brushes.White,
            Tag = tag,
            Cursor = cursor,
            // Keep handle centers stable while dragging; layout rounding can cause
            // half-pixel positions to snap left/right on consecutive frames.
            UseLayoutRounding = false,
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 0,
                OffsetY = 0,
                Blur = 8,
                Spread = 0,
                Color = Color.FromArgb(100, 0, 0, 0)
            })
        };

        Canvas.SetLeft(handleBorder, x - handleBorder.Width / 2);
        Canvas.SetTop(handleBorder, y - handleBorder.Height / 2);

        overlay.Children.Add(handleBorder);
        _selectionHandles.Add(handleBorder);
    }

    private void ShowSpeechBalloonTextEditor(SpeechBalloonControl balloonControl, Canvas unusedCanvas)
    {
        if (balloonControl.Annotation == null) return;

        // Use OverlayCanvas to ensure TextBox is on top of everything
        var overlay = _view.FindControl<Canvas>("OverlayCanvas");
        if (overlay == null) return;

        if (_balloonTextEditor != null)
        {
            // Remove from whichever parent it has
            (_balloonTextEditor.Parent as Panel)?.Children.Remove(_balloonTextEditor);
            _balloonTextEditor = null;
        }



        var annotation = balloonControl.Annotation;
        var balloonLeft = Canvas.GetLeft(balloonControl);
        var balloonTop = Canvas.GetTop(balloonControl);
        var balloonWidth = balloonControl.Width;
        var balloonHeight = balloonControl.Height;

        // Check if balloon is too small (e.g. user just clicked without dragging)
        if (balloonWidth < 50 || balloonHeight < 30)
        {
            balloonWidth = Math.Max(balloonWidth, 200);
            balloonHeight = Math.Max(balloonHeight, 100);

            balloonControl.Width = balloonWidth;
            balloonControl.Height = balloonHeight;

            annotation.EndPoint = new SKPoint(
                annotation.StartPoint.X + (float)balloonWidth,
                annotation.StartPoint.Y + (float)balloonHeight
            );

            // Fix Tail Point if it was at 0,0 or default
            if (annotation.TailPoint.X == 0 && annotation.TailPoint.Y == 0 ||
               (Math.Abs(annotation.TailPoint.X - annotation.StartPoint.X) < 1 && Math.Abs(annotation.TailPoint.Y - annotation.StartPoint.Y) < 1))
            {
                annotation.TailPoint = new SKPoint(
                    annotation.StartPoint.X + (float)balloonWidth / 2,
                    annotation.StartPoint.Y + (float)balloonHeight + 30
                );
            }

            balloonControl.InvalidateVisual();
            UpdateSelectionHandles();
        }

        // Ensure text is visible during editing
        IBrush foregroundBrush = new SolidColorBrush(Avalonia.Media.Color.Parse(annotation.TextColor));
        try
        {
            var foreColor = Avalonia.Media.Color.Parse(annotation.TextColor);
            var backColor = Avalonia.Media.Color.Parse(annotation.FillColor);

            // Calculate relative luminance
            double foreLum = (0.299 * foreColor.R + 0.587 * foreColor.G + 0.114 * foreColor.B) / 255.0;
            double backLum = (0.299 * backColor.R + 0.587 * backColor.G + 0.114 * backColor.B) / 255.0;
            double backAlpha = backColor.A / 255.0;

            // If background is transparent (showing image/canvas behind), we can't guarantee contrast.

            // If background is visible and contrast is low, pick black or white
            if (backAlpha > 0.1)
            {
                if (Math.Abs(foreLum - backLum) < 0.3)
                {
                    foregroundBrush = backLum > 0.5 ? Avalonia.Media.Brushes.Black : Avalonia.Media.Brushes.White;
                }
            }
        }
        catch { /* Fallback to stroke color */ }

        // Determine a safe background for the TextBox to ensure visibility
        // Use the fill color of the balloon as the base background for the editor
        IBrush editorBackground;
        var fillColor = Avalonia.Media.Color.Parse(annotation.FillColor);

        if (fillColor.A < 20)
        {
            // If balloon is transparent, continue to use the high-contrast semi-transparent background
            var fgBrush = foregroundBrush as SolidColorBrush;
            var fgColor = fgBrush?.Color ?? Avalonia.Media.Colors.Black;
            editorBackground = fgColor.R > 127
                ? new SolidColorBrush(Avalonia.Media.Color.Parse("#AA000000"))
                : new SolidColorBrush(Avalonia.Media.Color.Parse("#AAFFFFFF"));
        }
        else
        {
            // Use the actual fill color
            // We use the exact fill color so it looks seamless
            editorBackground = new SolidColorBrush(fillColor);
        }

        var textBox = new TextBox
        {
            Text = annotation.Text,
            Background = editorBackground,
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(0), // No corner radius needed if no border/background match
            Foreground = foregroundBrush,
            CaretBrush = foregroundBrush,
            FontSize = annotation.FontSize,
            Padding = new Thickness(12),
            TextAlignment = TextAlignment.Center,
            VerticalContentAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
            AcceptsReturn = false,
            TextWrapping = TextWrapping.Wrap
        };

        // ISSUE-FIX: Override theme resources to ensure Focus state doesn't revert to White background
        textBox.Resources["TextControlBackground"] = editorBackground;
        textBox.Resources["TextControlBackgroundFocused"] = editorBackground;
        textBox.Resources["TextControlBackgroundPointerOver"] = editorBackground;

        // ISSUE-FIX: Override border resources to remove them completely in all states
        textBox.Resources["TextControlBorderThemeThickness"] = new Thickness(0);
        textBox.Resources["TextControlBorderThemeThicknessFocused"] = new Thickness(0);
        textBox.Resources["TextControlBorderThemeThicknessPointerOver"] = new Thickness(0);
        textBox.Resources["TextControlBorderBrush"] = Avalonia.Media.Brushes.Transparent;
        textBox.Resources["TextControlBorderBrushFocused"] = Avalonia.Media.Brushes.Transparent;
        textBox.Resources["TextControlBorderBrushPointerOver"] = Avalonia.Media.Brushes.Transparent;

        // Ensure TextBox is above the balloon geometry (ZIndex 100 might not be enough if Overlay is higher)
        // But AnnotationCanvas is usually below Overlay. 
        // We can't put TextBox in Overlay because Overlay is for handles.
        textBox.SetValue(Panel.ZIndexProperty, 9999);

        Canvas.SetLeft(textBox, balloonLeft);
        Canvas.SetTop(textBox, balloonTop);
        textBox.Width = balloonWidth;
        textBox.Height = balloonHeight;

        textBox.LostFocus += (s, args) =>
        {
            if (s is TextBox tb)
            {
                annotation.Text = tb.Text ?? string.Empty;
                balloonControl.InvalidateVisual();
                (tb.Parent as Panel)?.Children.Remove(tb); // Remove from OverlayCanvas
                _balloonTextEditor = null;
            }
        };

        textBox.KeyDown += (s, args) =>
        {
            if (args.Key == Key.Enter || args.Key == Key.Escape)
            {
                args.Handled = true;
                _view.Focus();
            }
        };

        overlay.Children.Add(textBox); // Add to OverlayCanvas
        _balloonTextEditor = textBox;
        textBox.Focus();
        textBox.CaretIndex = textBox.Text.Length; // Place caret at end

        // Attach extended handlers for live update if needed, or rely on LostFocus
        AttachTextBoxEditHandlers(textBox);
    }

    public void PerformDelete()
    {
        if (_selectedShape != null)
        {
            var canvas = _view.FindControl<Canvas>("AnnotationCanvas");
            if (canvas != null && canvas.Children.Contains(_selectedShape))
            {
                canvas.Children.Remove(_selectedShape);

                if (_selectedShape is SpeechBalloonControl && _balloonTextEditor != null)
                {
                    canvas.Children.Remove(_balloonTextEditor);
                    _balloonTextEditor = null;
                }

                // Note: Undo stack logic not fully integrated here since EditorView owned it.
                // For now we assume EditorView handles undo/redo wrapping or we rely on events.
                // BUT: PerformDelete in EditorView used to push to redo stack?
                // Wait, EditorView.PerformDelete handles undo stack logic.
                // To avoid breaking Undo, we should perhaps keep PerformDelete in EditorView
                // OR EditorSelectionController delegates back to EditorView/UndoService?
                // Given constraints to NO logic changes, I should probably expose the inner delete logic
                // or call back to EditorView to delete.
                // But cleaning up handles is definitely SelectionController job.
            }
            ClearSelection();
        }
    }

    public void RegisterArrowEndpoint(Control path, Point start, Point end)
    {
        _shapeEndpoints[path] = (start, end);
    }


    private void UpdateHoverState(Canvas canvas, Point currentPoint)
    {
        // Only show hover outlines when Select tool is active
        if (_view.DataContext is MainViewModel vm)
        {
            // If Select/Spotlight tool inactive, we generally clear hover.
            // BUT: If there is an active selection (e.g. just created shape), we KEEP it highlighted.
            if (vm.ActiveTool != EditorTool.Select && vm.ActiveTool != EditorTool.Spotlight)
            {
                if (_selectedShape == null)
                {
                    ClearHoverOutline();
                    return;
                }
                // Fall through to hit testing to check if we are hovering the selected shape
            }
        }

        // Find shape under cursor (hit test)
        Control? hitShape = HitTestShape(canvas, currentPoint);

        // Filter: If using Spotlight tool, only highlight existing Spotlights
        if (_view.DataContext is MainViewModel vm2 && vm2.ActiveTool == EditorTool.Spotlight)
        {
            if (!(hitShape is SpotlightControl)) hitShape = null;
        }

        // Filter: If using other tools (Rect, etc), only allow hovering the ACTIVE selection
        if (_view.DataContext is MainViewModel vm3 &&
            vm3.ActiveTool != EditorTool.Select &&
            vm3.ActiveTool != EditorTool.Spotlight)
        {
            if (hitShape != _selectedShape) hitShape = null;
        }

        // If we're hovering over the selected shape, keep showing ant lines on it
        // Otherwise, show ant lines on the hovered (unselected) shape
        if (hitShape == _selectedShape && _selectedShape != null)
        {
            // Keep showing ant lines on selected shape
            if (_hoveredShape != _selectedShape)
            {
                ClearHoverOutline();
                _hoveredShape = _selectedShape;
            }
            UpdateHoverOutline();
        }
        else if (hitShape != _hoveredShape)
        {
            // Hovering over a different shape (or no shape)
            ClearHoverOutline();
            _hoveredShape = hitShape;

            if (_hoveredShape != null)
            {
                UpdateHoverOutline();
            }
        }
        else if (_hoveredShape != null)
        {
            // Shape is still hovered, update outline position (in case shape moved)
            UpdateHoverOutline();
        }
    }

    public Control? HitTestShape(Canvas canvas, Point currentPoint)
    {
        // Iterate through canvas children in reverse (top-most first)
        for (int i = canvas.Children.Count - 1; i >= 0; i--)
        {
            var child = canvas.Children[i] as Control;
            if (child == null) continue;

            // Skip non-moveable overlays and text editors
            if (child.Name == "CropOverlay" || child.Name == "CutOutOverlay") continue;
            // TextBox excluded? No, we want to select it now.
            // if (child is TextBox) continue;

            if (child is SpotlightControl sc && sc.Annotation is SpotlightAnnotation sa)
            {
                if (sa.GetBounds().Contains(ToSKPoint(currentPoint))) return sc;
                continue;
            }

            // Check if point is within the bounds of this control
            var shapeBounds = GetLogicalRect(child);

            // Special handling for Line
            if (child is global::Avalonia.Controls.Shapes.Line line)
            {
                var minX = Math.Min(line.StartPoint.X, line.EndPoint.X) - 5;
                var minY = Math.Min(line.StartPoint.Y, line.EndPoint.Y) - 5;
                var maxX = Math.Max(line.StartPoint.X, line.EndPoint.X) + 5;
                var maxY = Math.Max(line.StartPoint.Y, line.EndPoint.Y) + 5;
                shapeBounds = new Rect(minX, minY, maxX - minX, maxY - minY);
            }
            // Special handling for Path (Arrow)
            else if (child is global::Avalonia.Controls.Shapes.Path && _shapeEndpoints.TryGetValue(child, out var endpoints))
            {
                var minX = Math.Min(endpoints.Start.X, endpoints.End.X) - 10;
                var minY = Math.Min(endpoints.Start.Y, endpoints.End.Y) - 10;
                var maxX = Math.Max(endpoints.Start.X, endpoints.End.X) + 10;
                var maxY = Math.Max(endpoints.Start.Y, endpoints.End.Y) + 10;
                shapeBounds = new Rect(minX, minY, maxX - minX, maxY - minY);
            }
            // Special handling for Path (Freehand/SmartEraser) - use annotation bounds
            else if (child is global::Avalonia.Controls.Shapes.Path && child.Tag is IPointBasedAnnotation pointAnnotation)
            {
                var annBounds = ((Annotation)pointAnnotation).GetBounds();
                shapeBounds = new Rect(annBounds.Left - 5, annBounds.Top - 5, annBounds.Width + 10, annBounds.Height + 10);
            }
            // Special handling for Polyline (Freehand)
            else if (child is global::Avalonia.Controls.Shapes.Polyline polyline && polyline.Points != null)
            {
                double minX = double.MaxValue, minY = double.MaxValue;
                double maxX = double.MinValue, maxY = double.MinValue;
                foreach (var p in polyline.Points)
                {
                    if (p.X < minX) minX = p.X;
                    if (p.Y < minY) minY = p.Y;
                    if (p.X > maxX) maxX = p.X;
                    if (p.Y > maxY) maxY = p.Y;
                }
                if (minX == double.MaxValue)
                {
                    minX = 0; minY = 0; maxX = 0; maxY = 0;
                }
                else
                {
                    minX -= 5; minY -= 5; maxX += 5; maxY += 5;
                }
                shapeBounds = new Rect(minX, minY, maxX - minX, maxY - minY);
            }

            // Start with rough bounds check
            bool isRotated = false;
            if (child.Tag is Annotation tagAnn && tagAnn.RotationAngle != 0)
            {
                isRotated = true;
            }

            // If rotated, the axis-aligned 'shapeBounds' is invalid/insufficient.
            // We skip the Contains check and rely on the annotation's own HitTest (which handles rotation).
            if (isRotated || shapeBounds.Contains(currentPoint))
            {
                // Use the annotation's HitTest method if available
                if (child.Tag is Annotation annotation)
                {
                    var skPoint = ToSKPoint(currentPoint);
                    if (annotation.HitTest(skPoint))
                    {
                        return child;
                    }
                }
                else
                {
                    return child;
                }
            }
        }
        return null;
    }

    private void ClearHoverOutline()
    {
        var overlay = _view.FindControl<Canvas>("OverlayCanvas");
        if (_hoverOutlineBlack != null)
        {
            overlay?.Children.Remove(_hoverOutlineBlack);
            _hoverOutlineBlack = null;
        }
        if (_hoverOutlineWhite != null)
        {
            overlay?.Children.Remove(_hoverOutlineWhite);
            _hoverOutlineWhite = null;
        }
        if (_hoverPolylineBlack != null)
        {
            overlay?.Children.Remove(_hoverPolylineBlack);
            _hoverPolylineBlack = null;
        }
        if (_hoverPolylineWhite != null)
        {
            overlay?.Children.Remove(_hoverPolylineWhite);
            _hoverPolylineWhite = null;
        }
        if (_hoverEllipseBlack != null)
        {
            overlay?.Children.Remove(_hoverEllipseBlack);
            _hoverEllipseBlack = null;
        }
        if (_hoverEllipseWhite != null)
        {
            overlay?.Children.Remove(_hoverEllipseWhite);
            _hoverEllipseWhite = null;
        }
        _hoveredShape = null;
    }

    private void UpdateHoverOutline()
    {
        if (_hoveredShape == null) return;

        var overlay = _view.FindControl<Canvas>("OverlayCanvas");
        if (overlay == null) return;

        // 1. Path-based Outline (Polyline)
        // Used for Line, Arrow (Path), and Freehand (Polyline) to show outline along the stroke
        IList<Point>? outlinePoints = null;

        if (_hoveredShape is global::Avalonia.Controls.Shapes.Line line)
        {
            outlinePoints = new List<Point> { line.StartPoint, line.EndPoint };
        }
        else if (_hoveredShape is global::Avalonia.Controls.Shapes.Path arrowPath && _shapeEndpoints.TryGetValue(arrowPath, out var endpoints))
        {
            outlinePoints = new List<Point> { endpoints.Start, endpoints.End };
        }
        else if (_hoveredShape is global::Avalonia.Controls.Shapes.Path path && path.Tag is IPointBasedAnnotation pointAnnotation)
        {
            // Convert SKPoints to Avalonia Points for the outline
            outlinePoints = new List<Point>();
            foreach (var pt in pointAnnotation.Points)
            {
                outlinePoints.Add(new Point(pt.X, pt.Y));
            }
        }
        else if (_hoveredShape is Polyline polyline)
        {
            outlinePoints = polyline.Points;
        }

        if (outlinePoints != null)
        {
            if (_hoverPolylineBlack == null)
            {
                _hoverPolylineBlack = new Polyline
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    StrokeDashArray = new global::Avalonia.Collections.AvaloniaList<double> { 3, 3 },
                    IsHitTestVisible = false
                };
                overlay.Children.Add(_hoverPolylineBlack);
            }
            if (_hoverPolylineWhite == null)
            {
                _hoverPolylineWhite = new Polyline
                {
                    Stroke = Brushes.White,
                    StrokeThickness = 1,
                    StrokeDashArray = new global::Avalonia.Collections.AvaloniaList<double> { 3, 3 },
                    StrokeDashOffset = 3,
                    IsHitTestVisible = false
                };
                overlay.Children.Add(_hoverPolylineWhite);
            }

            _hoverPolylineBlack.Points = outlinePoints;
            _hoverPolylineWhite.Points = outlinePoints;
            return;
        }

        // 2. Bounds-based Calculation
        double left, top, width, height;

        if (_hoveredShape is SpotlightControl sc && sc.Annotation is SpotlightAnnotation sa)
        {
            var b = sa.GetBounds();
            left = b.Left; top = b.Top; width = b.Width; height = b.Height;
        }
        else
        {
            var hoveredRect = GetLogicalRect(_hoveredShape);
            left = hoveredRect.Left;
            top = hoveredRect.Top;
            width = hoveredRect.Width;
            height = hoveredRect.Height;
        }

        if (width <= 0 || height <= 0) return;

        // 3. Ellipse Outline (for Ellipse and Step/Number)
        if (_hoveredShape is Ellipse || (_hoveredShape is Grid && _hoveredShape.Tag is NumberAnnotation))
        {
            if (_hoverEllipseBlack == null)
            {
                _hoverEllipseBlack = new Ellipse
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    StrokeDashArray = new global::Avalonia.Collections.AvaloniaList<double> { 3, 3 },
                    IsHitTestVisible = false
                };
                overlay.Children.Add(_hoverEllipseBlack);
            }
            if (_hoverEllipseWhite == null)
            {
                _hoverEllipseWhite = new Ellipse
                {
                    Stroke = Brushes.White,
                    StrokeThickness = 1,
                    StrokeDashArray = new global::Avalonia.Collections.AvaloniaList<double> { 3, 3 },
                    StrokeDashOffset = 3,
                    IsHitTestVisible = false
                };
                overlay.Children.Add(_hoverEllipseWhite);
            }

            Canvas.SetLeft(_hoverEllipseBlack, left - 2);
            Canvas.SetTop(_hoverEllipseBlack, top - 2);
            _hoverEllipseBlack.Width = width + 4;
            _hoverEllipseBlack.Height = height + 4;

            Canvas.SetLeft(_hoverEllipseWhite, left - 2);
            Canvas.SetTop(_hoverEllipseWhite, top - 2);
            _hoverEllipseWhite.Width = width + 4;
            _hoverEllipseWhite.Height = height + 4;
            return;
        }

        // 4. Rectangle Outline (Default)
        if (_hoverOutlineBlack == null)
        {
            _hoverOutlineBlack = new global::Avalonia.Controls.Shapes.Rectangle
            {
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                StrokeDashArray = new global::Avalonia.Collections.AvaloniaList<double> { 3, 3 },
                Fill = null,
                IsHitTestVisible = false
            };
            overlay.Children.Add(_hoverOutlineBlack);
        }

        if (_hoverOutlineWhite == null)
        {
            _hoverOutlineWhite = new global::Avalonia.Controls.Shapes.Rectangle
            {
                Stroke = Brushes.White,
                StrokeThickness = 1,
                StrokeDashArray = new global::Avalonia.Collections.AvaloniaList<double> { 3, 3 },
                StrokeDashOffset = 3,
                Fill = null,
                IsHitTestVisible = false
            };
            overlay.Children.Add(_hoverOutlineWhite);
        }

        Canvas.SetLeft(_hoverOutlineBlack, left - 2);
        Canvas.SetTop(_hoverOutlineBlack, top - 2);
        _hoverOutlineBlack.Width = width + 4;
        _hoverOutlineBlack.Height = height + 4;

        Canvas.SetLeft(_hoverOutlineWhite, left - 2);
        Canvas.SetTop(_hoverOutlineWhite, top - 2);
        _hoverOutlineWhite.Width = width + 4;
        _hoverOutlineWhite.Height = height + 4;

        // Apply rotation to hover outline for rotated shapes (e.g. TextBox)
        if (_hoveredShape is TextBox hoveredTb && hoveredTb.Tag is TextAnnotation hoveredTextAnn && hoveredTextAnn.RotationAngle != 0)
        {
            var rotTransform = new RotateTransform(hoveredTextAnn.RotationAngle);
            // Rotate around the center of the outline (which matches the shape center)
            var originX = (width + 4) > 0 ? ((left + width / 2) - (left - 2)) / (width + 4) : 0.5;
            var originY = (height + 4) > 0 ? ((top + height / 2) - (top - 2)) / (height + 4) : 0.5;
            var origin = new RelativePoint(originX, originY, RelativeUnit.Relative);

            _hoverOutlineBlack.RenderTransformOrigin = origin;
            _hoverOutlineBlack.RenderTransform = rotTransform;
            _hoverOutlineWhite.RenderTransformOrigin = origin;
            _hoverOutlineWhite.RenderTransform = rotTransform;
        }
        else
        {
            _hoverOutlineBlack.RenderTransform = null;
            _hoverOutlineWhite.RenderTransform = null;
        }
    }

    private void AttachTextBoxEditHandlers(TextBox tb)
    {
        EventHandler<global::Avalonia.Interactivity.RoutedEventArgs>? lostFocusHandler = null;
        EventHandler<KeyEventArgs>? keyDownHandler = null;

        lostFocusHandler = (s, args) =>
        {
            if (lostFocusHandler != null) tb.LostFocus -= lostFocusHandler;
            if (keyDownHandler != null) tb.KeyDown -= keyDownHandler;

            tb.IsHitTestVisible = false;

            if (tb.Tag is Annotation annotation)
            {
                // Sync Text
                if (annotation is TextAnnotation textAnn)
                {
                    textAnn.Text = tb.Text ?? string.Empty;

                    // Sync Bounds
                    var textBoxRect = GetLogicalRect(tb);
                    textAnn.EndPoint = new SKPoint(
                        (float)(textBoxRect.Left + textBoxRect.Width),
                        (float)(textBoxRect.Top + textBoxRect.Height)
                    );

                    UpdateSelectionHandles();
                    UpdateHoverOutline();
                }
            }
        };

        keyDownHandler = (s, args) =>
        {
            if (args.Key == Key.Enter)
            {
                args.Handled = true;
                _view.Focus();
            }
        };

        tb.LostFocus += lostFocusHandler;
        tb.KeyDown += keyDownHandler;
    }

    private static SKPoint ToSKPoint(Point point) => new((float)point.X, (float)point.Y);
    public void UpdateActiveTextEditorProperties()
    {
        if (_balloonTextEditor == null || !(_selectedShape is SpeechBalloonControl balloonControl)) return;
        if (balloonControl.Annotation is not SpeechBalloonAnnotation annotation) return;

        // Update Font Size
        _balloonTextEditor.FontSize = annotation.FontSize;

        // Re-execute color logic (matching ShowSpeechBalloonTextEditor logic)
        IBrush foregroundBrush = new SolidColorBrush(Avalonia.Media.Color.Parse(annotation.StrokeColor));

        try
        {
            var foreColor = Avalonia.Media.Color.Parse(annotation.StrokeColor);
            var backColor = Avalonia.Media.Color.Parse(annotation.FillColor);
            double foreLum = (0.299 * foreColor.R + 0.587 * foreColor.G + 0.114 * foreColor.B) / 255.0;
            double backLum = (0.299 * backColor.R + 0.587 * backColor.G + 0.114 * backColor.B) / 255.0;
            double backAlpha = backColor.A / 255.0;

            if (backAlpha > 0.1)
            {
                if (Math.Abs(foreLum - backLum) < 0.3)
                {
                    foregroundBrush = backLum > 0.5 ? Avalonia.Media.Brushes.Black : Avalonia.Media.Brushes.White;
                }
            }
        }
        catch { }

        IBrush editorBackground;
        var fillColor = Avalonia.Media.Color.Parse(annotation.FillColor);
        if (fillColor.A < 20)
        {
            var fgBrush = foregroundBrush as SolidColorBrush;
            var fgColor = fgBrush?.Color ?? Avalonia.Media.Colors.Black;
            editorBackground = fgColor.R > 127
                ? new SolidColorBrush(Avalonia.Media.Color.Parse("#AA000000"))
                : new SolidColorBrush(Avalonia.Media.Color.Parse("#AAFFFFFF"));
        }
        else
        {
            editorBackground = new SolidColorBrush(fillColor);
        }

        _balloonTextEditor.Background = editorBackground;
        _balloonTextEditor.Foreground = foregroundBrush;
        _balloonTextEditor.CaretBrush = foregroundBrush;

        // Update resource overrides for Focus state
        _balloonTextEditor.Resources["TextControlBackground"] = editorBackground;
        _balloonTextEditor.Resources["TextControlBackgroundFocused"] = editorBackground;
        _balloonTextEditor.Resources["TextControlBackgroundPointerOver"] = editorBackground;

        _balloonTextEditor.Resources["TextControlBorderThemeThickness"] = new Thickness(0);
        _balloonTextEditor.Resources["TextControlBorderThemeThicknessFocused"] = new Thickness(0);
        _balloonTextEditor.Resources["TextControlBorderThemeThicknessPointerOver"] = new Thickness(0);
        _balloonTextEditor.Resources["TextControlBorderBrush"] = Avalonia.Media.Brushes.Transparent;
        _balloonTextEditor.Resources["TextControlBorderBrushFocused"] = Avalonia.Media.Brushes.Transparent;
        _balloonTextEditor.Resources["TextControlBorderBrushPointerOver"] = Avalonia.Media.Brushes.Transparent;
    }

    private void ShowTextEditor(OutlinedTextControl textControl, Canvas canvas)
    {
        if (textControl.Annotation is not TextAnnotation annotation) return;

        var overlay = _view.FindControl<Canvas>("OverlayCanvas");
        if (overlay == null) return;

        // Hide the original control while editing
        textControl.IsVisible = false;

        // Create a temporary TextBox for editing
        string textColor = annotation.TextColor;
        if (string.IsNullOrEmpty(textColor) || textColor == "#00000000")
        {
            // Fallback to black for editing if text is transparent
            // Only if stroke isn't set. Actually if TextColor is transparent but StrokeColor is set, text is stroke-only.
            // Using StrokeColor for the editor text if TextColor is transparent makes sense so it's visible.
            string strokeColor = annotation.StrokeColor;
            if (string.IsNullOrEmpty(strokeColor) || strokeColor == "#00000000")
            {
                textColor = "#FF000000";
            }
            else
            {
                textColor = strokeColor;
            }
        }

        var textBox = new TextBox
        {
            Text = annotation.Text,
            Foreground = new SolidColorBrush(Avalonia.Media.Color.Parse(textColor)),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gray,
            FontSize = annotation.FontSize,
            FontWeight = annotation.IsBold ? FontWeight.Bold : FontWeight.Normal,
            FontStyle = annotation.IsItalic ? FontStyle.Italic : FontStyle.Normal,
            Padding = new Thickness(4),
            AcceptsReturn = false,
            TextWrapping = TextWrapping.NoWrap,
            MinWidth = 20
        };

        // Force Avalonia's internal text box states to be transparent
        textBox.Resources["TextControlBackground"] = Brushes.Transparent;
        textBox.Resources["TextControlBackgroundFocused"] = Brushes.Transparent;
        textBox.Resources["TextControlBackgroundPointerOver"] = Brushes.Transparent;

        // Apply rotation to make editing match display
        if (annotation.RotationAngle != 0)
        {
            textBox.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            textBox.RenderTransform = new RotateTransform(annotation.RotationAngle);
        }

        var annotationBounds = annotation.GetBounds();
        Canvas.SetLeft(textBox, annotationBounds.Left);
        Canvas.SetTop(textBox, annotationBounds.Top);

        EventHandler<global::Avalonia.Interactivity.RoutedEventArgs>? lostFocusHandler = null;
        EventHandler<KeyEventArgs>? keyDownHandler = null;

        void CompleteEditing()
        {
            if (lostFocusHandler != null) textBox.LostFocus -= lostFocusHandler;
            if (keyDownHandler != null) textBox.KeyDown -= keyDownHandler;

            annotation.Text = textBox.Text ?? string.Empty;

            // Remove from overlay
            overlay.Children.Remove(textBox);

            // Unhide the original control and measure it to compute correct sizes
            textControl.IsVisible = true;
            textControl.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            textControl.InvalidateVisual();

            // Fire RequestUpdateEffect to save new state if needed
            RequestUpdateEffect?.Invoke(textControl);

            // Sync Bounds based on new measured dimension
            var newWidth = textControl.DesiredSize.Width > 0 ? textControl.DesiredSize.Width : 20;
            var newHeight = textControl.DesiredSize.Height > 0 ? textControl.DesiredSize.Height : 20;

            annotation.EndPoint = new SKPoint(
                (float)(Canvas.GetLeft(textControl) + newWidth),
                (float)(Canvas.GetTop(textControl) + newHeight)
            );

            UpdateSelectionHandles();
            UpdateHoverOutline();
        }

        lostFocusHandler = (s, args) => CompleteEditing();

        keyDownHandler = (s, args) =>
        {
            if (args.Key == Key.Enter)
            {
                args.Handled = true;
                _view.Focus();
            }
        };

        textBox.LostFocus += lostFocusHandler;
        textBox.KeyDown += keyDownHandler;

        overlay.Children.Add(textBox);
        textBox.Focus();
    }

    private void UpdateBoundsObserver()
    {
        if (_observedShape != null && _boundsHandler != null)
        {
            _observedShape.PropertyChanged -= _boundsHandler;
            _observedShape = null;
            _boundsHandler = null;
        }

        if (_selectedShape is OutlinedTextControl tb && tb.Tag is TextAnnotation textAnn)
        {
            _observedShape = tb;
            _boundsHandler = (s, args) =>
            {
                if (args.Property == Visual.BoundsProperty)
                {
                    var textRect = GetLogicalRect(tb);
                    // Handle valid size (ignore 0x0 or uninitialized)
                    if (textRect.Width > 0 && textRect.Height > 0)
                    {
                        var left = textRect.Left;
                        var top = textRect.Top;

                        // Sync visual bounds to annotation model
                        // This ensures hit tests (which rely on annotation start/end points) are accurate
                        textAnn.StartPoint = new SKPoint((float)left, (float)top);
                        textAnn.EndPoint = new SKPoint((float)(left + textRect.Width), (float)(top + textRect.Height));

                        // Refresh selection handles to match new bounds
                        UpdateSelectionHandles();
                    }
                }
            };
            _observedShape.PropertyChanged += _boundsHandler;
        }
    }

    private static Rect GetLogicalRect(Control control)
    {
        if (control is SpotlightControl spotlight && spotlight.Annotation is SpotlightAnnotation spotlightAnnotation)
        {
            return ToRect(spotlightAnnotation.GetBounds());
        }

        if (control is SpeechBalloonControl balloonControl && balloonControl.Annotation is SpeechBalloonAnnotation balloonAnnotation)
        {
            return ToRect(balloonAnnotation.GetBounds());
        }

        if (control.Tag is Annotation annotation)
        {
            var annotationRect = ToRect(annotation.GetBounds());
            if (annotationRect.Width > 0 && annotationRect.Height > 0)
            {
                return annotationRect;
            }
        }

        var left = Canvas.GetLeft(control);
        var top = Canvas.GetTop(control);
        if (double.IsNaN(left)) left = 0;
        if (double.IsNaN(top)) top = 0;

        var width = control.Width;
        var height = control.Height;

        if (double.IsNaN(width) || width <= 0) width = control.DesiredSize.Width;
        if (double.IsNaN(height) || height <= 0) height = control.DesiredSize.Height;

        // Last-resort fallback for controls that have not been initialized with explicit size.
        if (width <= 0 || height <= 0)
        {
            width = control.Bounds.Width;
            height = control.Bounds.Height;
        }

        if (double.IsNaN(width) || width < 0) width = 0;
        if (double.IsNaN(height) || height < 0) height = 0;

        return new Rect(left, top, width, height);
    }

    private static Rect ToRect(SKRect rect)
        => new Rect(rect.Left, rect.Top, rect.Width, rect.Height);
}

