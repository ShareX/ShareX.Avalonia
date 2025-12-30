using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ShareX.Avalonia.UI.ViewModels;

namespace ShareX.Avalonia.UI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            KeyDown += OnKeyDown;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;
            
            // Skip if typing in a text input
            if (e.Source is TextBox) return;

            // Handle Ctrl key combinations
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                switch (e.Key)
                {
                    case Key.Z:
                        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                            vm.RedoCommand.Execute(null);
                        else
                            vm.UndoCommand.Execute(null);
                        e.Handled = true;
                        return;
                    case Key.Y:
                        vm.RedoCommand.Execute(null);
                        e.Handled = true;
                        return;
                    case Key.C:
                        vm.CopyCommand.Execute(null);
                        e.Handled = true;
                        return;
                    case Key.S:
                        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                            vm.SaveAsCommand.Execute(null);
                        else
                            vm.QuickSaveCommand.Execute(null);
                        e.Handled = true;
                        return;
                }
            }

            // Tool selection shortcuts (single keys without modifiers)
            if (e.KeyModifiers == KeyModifiers.None)
            {
                switch (e.Key)
                {
                    case Key.V:
                        vm.SelectToolCommand.Execute(EditorTool.Select);
                        e.Handled = true;
                        break;
                    case Key.R:
                        vm.SelectToolCommand.Execute(EditorTool.Rectangle);
                        e.Handled = true;
                        break;
                    case Key.E:
                        vm.SelectToolCommand.Execute(EditorTool.Ellipse);
                        e.Handled = true;
                        break;
                    case Key.A:
                        vm.SelectToolCommand.Execute(EditorTool.Arrow);
                        e.Handled = true;
                        break;
                    case Key.L:
                        vm.SelectToolCommand.Execute(EditorTool.Line);
                        e.Handled = true;
                        break;
                    case Key.T:
                        vm.SelectToolCommand.Execute(EditorTool.Text);
                        e.Handled = true;
                        break;
                    case Key.N:
                        vm.SelectToolCommand.Execute(EditorTool.Number);
                        e.Handled = true;
                        break;
                    case Key.S:
                        vm.SelectToolCommand.Execute(EditorTool.Spotlight);
                        e.Handled = true;
                        break;
                    case Key.C:
                        vm.SelectToolCommand.Execute(EditorTool.Crop);
                        e.Handled = true;
                        break;
                    case Key.Delete:
                    case Key.Back:
                        vm.DeleteSelectedCommand.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.Escape:
                        vm.SelectToolCommand.Execute(EditorTool.Select);
                        e.Handled = true;
                        break;
                }
            }
        }

        private Point _startPoint;
        private bool _isDrawing;
        private Control? _currentShape;

        private void OnCanvasPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;
            if (vm.ActiveTool == EditorTool.Select) return;

            var canvas = sender as Canvas;
            if (canvas == null) return;

            _startPoint = e.GetPosition(canvas);
            _isDrawing = true;

            var brush = new SolidColorBrush(Color.Parse(vm.SelectedColor));
            
            switch (vm.ActiveTool)
            {
                case EditorTool.Rectangle:
                    _currentShape = new global::Avalonia.Controls.Shapes.Rectangle
                    {
                        Stroke = brush,
                        StrokeThickness = vm.StrokeWidth,
                        Fill = Brushes.Transparent
                    };
                    break;
                case EditorTool.Ellipse:
                    _currentShape = new global::Avalonia.Controls.Shapes.Ellipse
                    {
                        Stroke = brush,
                        StrokeThickness = vm.StrokeWidth,
                        Fill = Brushes.Transparent
                    };
                    break;
                case EditorTool.Line:
                    _currentShape = new global::Avalonia.Controls.Shapes.Line
                    {
                        Stroke = brush,
                        StrokeThickness = vm.StrokeWidth,
                        StartPoint = _startPoint,
                        EndPoint = _startPoint
                    };
                    break;
                case EditorTool.Arrow:
                    _currentShape = new global::Avalonia.Controls.Shapes.Path
                    {
                        Stroke = brush,
                        StrokeThickness = vm.StrokeWidth,
                        Fill = brush, // Fill arrowhead
                        Data = new PathGeometry()
                    };
                    break;
                case EditorTool.Text:
                    // For text, we create a TextBox directly
                    var textBox = new TextBox
                    {
                        Foreground = brush,
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(1), // Visible border while editing
                        BorderBrush = Brushes.White, 
                        FontSize = Math.Max(12, vm.StrokeWidth * 4),
                        Text = "Text",
                        Padding = new Thickness(4),
                        MinWidth = 50
                    };
                    
                    // Position it
                    Canvas.SetLeft(textBox, _startPoint.X);
                    Canvas.SetTop(textBox, _startPoint.Y);
                    
                    // Add logic to remove border when lost focus?
                    textBox.LostFocus += (s, args) => 
                    {
                        if (s is TextBox tb)
                        {
                            tb.BorderThickness = new Thickness(0);
                            if (string.IsNullOrWhiteSpace(tb.Text))
                            {
                                // Remove empty text boxes
                                var parentKey = tb.Parent as Panel;
                                parentKey?.Children.Remove(tb);
                            }
                        }
                    };

                    canvas.Children.Add(textBox);
                    textBox.Focus();
                    
                    // We don't set _currentShape because we don't resize text via drag creation typically
                    // But if we wanted drag-rect creation for text box size, we could.
                    // For now, click-to-type.
                    _isDrawing = false; 
                    return; 
            }

            if (_currentShape != null)
            {
                if (vm.ActiveTool != EditorTool.Line && vm.ActiveTool != EditorTool.Arrow)
                {
                    Canvas.SetLeft(_currentShape, _startPoint.X);
                    Canvas.SetTop(_currentShape, _startPoint.Y);
                }
                canvas.Children.Add(_currentShape);
            }
        }

        private void OnCanvasPointerMoved(object sender, PointerEventArgs e)
        {
            if (!_isDrawing || _currentShape == null) return;
            
            var canvas = sender as Canvas;
            if (canvas == null) return;

            var currentPoint = e.GetPosition(canvas);

            if (_currentShape is global::Avalonia.Controls.Shapes.Line line)
            {
                line.EndPoint = currentPoint;
            }
            else if (_currentShape is global::Avalonia.Controls.Shapes.Path arrowPath && DataContext is MainViewModel vm)
            {
                // Update Arrow Geometry
                arrowPath.Data = CreateArrowGeometry(_startPoint, currentPoint, vm.StrokeWidth * 3);
            }
            else
            {
                var x = Math.Min(_startPoint.X, currentPoint.X);
                var y = Math.Min(_startPoint.Y, currentPoint.Y);
                var width = Math.Abs(_startPoint.X - currentPoint.X);
                var height = Math.Abs(_startPoint.Y - currentPoint.Y);

                if (_currentShape is global::Avalonia.Controls.Shapes.Rectangle rect)
                {
                    rect.Width = width;
                    rect.Height = height;
                    Canvas.SetLeft(rect, x);
                    Canvas.SetTop(rect, y);
                }
                else if (_currentShape is global::Avalonia.Controls.Shapes.Ellipse ellipse)
                {
                    ellipse.Width = width;
                    ellipse.Height = height;
                    Canvas.SetLeft(ellipse, x);
                    Canvas.SetTop(ellipse, y);
                }
            }
        }

        private Geometry CreateArrowGeometry(Point start, Point end, double headSize)
        {
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                // Draw line
                ctx.BeginFigure(start, false);
                ctx.LineTo(end);

                // Calculate arrow head
                var d = end - start;
                var length = Math.Sqrt(d.X * d.X + d.Y * d.Y);
                
                if (length > 0)
                {
                    var ux = d.X / length;
                    var uy = d.Y / length;

                    // Arrow head points
                    var p1 = new Point(end.X - headSize * ux + headSize * 0.5 * uy, end.Y - headSize * uy - headSize * 0.5 * ux);
                    var p2 = new Point(end.X - headSize * ux - headSize * 0.5 * uy, end.Y - headSize * uy + headSize * 0.5 * ux);

                    // Draw head as a separate figure or continuation?
                    // Let's draw it as a filled triangle at the end
                    ctx.EndFigure(false);
                    
                    ctx.BeginFigure(end, true); // Filled
                    ctx.LineTo(p1);
                    ctx.LineTo(p2);
                    ctx.LineTo(end);
                }
            }
            return geometry;
        }

        private void OnCanvasPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (_isDrawing)
            {
                _isDrawing = false;
                _currentShape = null;
                // TODO: Add to ViewModel collection for Undo/Redo
            }
        }

        private void OnMinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnMaximizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized 
                ? WindowState.Normal 
                : WindowState.Maximized;
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
