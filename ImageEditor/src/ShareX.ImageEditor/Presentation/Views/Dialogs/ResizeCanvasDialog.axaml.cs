using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using SkiaSharp;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class ResizeCanvasDialog : UserControl
    {
        public static readonly StyledProperty<int> TopPaddingProperty =
            AvaloniaProperty.Register<ResizeCanvasDialog, int>(nameof(TopPadding), 0);

        public static readonly StyledProperty<int> RightPaddingProperty =
            AvaloniaProperty.Register<ResizeCanvasDialog, int>(nameof(RightPadding), 0);

        public static readonly StyledProperty<int> BottomPaddingProperty =
            AvaloniaProperty.Register<ResizeCanvasDialog, int>(nameof(BottomPadding), 0);

        public static readonly StyledProperty<int> LeftPaddingProperty =
            AvaloniaProperty.Register<ResizeCanvasDialog, int>(nameof(LeftPadding), 0);

        public static readonly StyledProperty<IBrush> CanvasColorBrushProperty =
            AvaloniaProperty.Register<ResizeCanvasDialog, IBrush>(nameof(CanvasColorBrush), Brushes.Transparent);

        public static readonly StyledProperty<Color> CanvasColorValueProperty =
            AvaloniaProperty.Register<ResizeCanvasDialog, Color>(nameof(CanvasColorValue), Colors.Transparent);

        public static readonly StyledProperty<string> CanvasColorTextProperty =
            AvaloniaProperty.Register<ResizeCanvasDialog, string>(nameof(CanvasColorText), "Transparent");

        public int TopPadding
        {
            get => GetValue(TopPaddingProperty);
            set => SetValue(TopPaddingProperty, value);
        }

        public int RightPadding
        {
            get => GetValue(RightPaddingProperty);
            set => SetValue(RightPaddingProperty, value);
        }

        public int BottomPadding
        {
            get => GetValue(BottomPaddingProperty);
            set => SetValue(BottomPaddingProperty, value);
        }

        public int LeftPadding
        {
            get => GetValue(LeftPaddingProperty);
            set => SetValue(LeftPaddingProperty, value);
        }

        public IBrush CanvasColorBrush
        {
            get => GetValue(CanvasColorBrushProperty);
            set => SetValue(CanvasColorBrushProperty, value);
        }

        public Color CanvasColorValue
        {
            get => GetValue(CanvasColorValueProperty);
            set => SetValue(CanvasColorValueProperty, value);
        }

        public string CanvasColorText
        {
            get => GetValue(CanvasColorTextProperty);
            set => SetValue(CanvasColorTextProperty, value);
        }

        private SKColor _canvasColor = SKColors.Transparent;

        public event EventHandler<ResizeCanvasEventArgs>? ApplyRequested;
        public event EventHandler? CancelRequested;

        static ResizeCanvasDialog()
        {
            CanvasColorValueProperty.Changed.AddClassHandler<ResizeCanvasDialog>((s, e) =>
            {
                s.OnCanvasColorValueChanged();
            });
        }

        public ResizeCanvasDialog()
        {
            AvaloniaXamlLoader.Load(this);
            UpdateColorBrush();
            UpdateColorText();
        }

        private void OnCanvasColorValueChanged()
        {
            var color = CanvasColorValue;
            _canvasColor = new SKColor(color.R, color.G, color.B, color.A);
            UpdateColorBrush();
            UpdateColorText();
        }

        private void OnColorButtonClick(object? sender, RoutedEventArgs e)
        {
            var popup = this.FindControl<Popup>("ColorPopup");
            if (popup != null)
            {
                popup.IsOpen = !popup.IsOpen;
            }
        }

        private void UpdateColorBrush()
        {
            CanvasColorBrush = new SolidColorBrush(
                Color.FromArgb(_canvasColor.Alpha, _canvasColor.Red, _canvasColor.Green, _canvasColor.Blue));
        }

        private void UpdateColorText()
        {
            if (_canvasColor.Alpha == 0)
            {
                CanvasColorText = "Transparent";
            }
            else
            {
                CanvasColorText = $"#{_canvasColor.Alpha:X2}{_canvasColor.Red:X2}{_canvasColor.Green:X2}{_canvasColor.Blue:X2}";
            }
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            ApplyRequested?.Invoke(this, new ResizeCanvasEventArgs(
                TopPadding, RightPadding, BottomPadding, LeftPadding, _canvasColor));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public class ResizeCanvasEventArgs : EventArgs
    {
        public int Top { get; }
        public int Right { get; }
        public int Bottom { get; }
        public int Left { get; }
        public SKColor BackgroundColor { get; }

        public ResizeCanvasEventArgs(int top, int right, int bottom, int left, SKColor backgroundColor)
        {
            Top = top;
            Right = right;
            Bottom = bottom;
            Left = left;
            BackgroundColor = backgroundColor;
        }
    }
}
