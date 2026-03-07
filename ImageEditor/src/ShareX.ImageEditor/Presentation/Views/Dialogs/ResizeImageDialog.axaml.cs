using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SkiaSharp;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class ResizeImageDialog : UserControl
    {
        public static readonly StyledProperty<int> ResizeWidthProperty =
            AvaloniaProperty.Register<ResizeImageDialog, int>(nameof(ResizeWidth), 800);

        public static readonly StyledProperty<int> ResizeHeightProperty =
            AvaloniaProperty.Register<ResizeImageDialog, int>(nameof(ResizeHeight), 600);

        public static readonly StyledProperty<bool> MaintainAspectRatioProperty =
            AvaloniaProperty.Register<ResizeImageDialog, bool>(nameof(MaintainAspectRatio), false);

        public int ResizeWidth
        {
            get => GetValue(ResizeWidthProperty);
            set => SetValue(ResizeWidthProperty, value);
        }

        public int ResizeHeight
        {
            get => GetValue(ResizeHeightProperty);
            set => SetValue(ResizeHeightProperty, value);
        }

        public bool MaintainAspectRatio
        {
            get => GetValue(MaintainAspectRatioProperty);
            set => SetValue(MaintainAspectRatioProperty, value);
        }

        private double _aspectRatio = 1.0;
        private bool _isUpdating = false;

        public event EventHandler<ResizeImageEventArgs>? ApplyRequested;
        public event EventHandler? CancelRequested;

        public ResizeImageDialog()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void Initialize(int currentWidth, int currentHeight)
        {
            ResizeWidth = currentWidth;
            ResizeHeight = currentHeight;
            _aspectRatio = (double)currentWidth / currentHeight;
        }

        private void OnWidthChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            if (_isUpdating || !MaintainAspectRatio) return;
            if (e.NewValue == null) return;

            _isUpdating = true;
            ResizeHeight = (int)Math.Round((double)e.NewValue / _aspectRatio);
            _isUpdating = false;
        }

        private void OnHeightChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            if (_isUpdating || !MaintainAspectRatio) return;
            if (e.NewValue == null) return;

            _isUpdating = true;
            ResizeWidth = (int)Math.Round((double)e.NewValue * _aspectRatio);
            _isUpdating = false;
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            var combo = this.FindControl<ComboBox>("ResampleModeCombo");
            var quality = combo?.SelectedIndex switch
            {
                0 => SKFilterQuality.None,
                1 => SKFilterQuality.Low,
                2 => SKFilterQuality.Medium,
                3 => SKFilterQuality.High,
                _ => SKFilterQuality.High
            };

            ApplyRequested?.Invoke(this, new ResizeImageEventArgs(ResizeWidth, ResizeHeight, quality));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public class ResizeImageEventArgs : EventArgs
    {
        public int NewWidth { get; }
        public int NewHeight { get; }
        public SKFilterQuality Quality { get; }

        public ResizeImageEventArgs(int width, int height, SKFilterQuality quality)
        {
            NewWidth = width;
            NewHeight = height;
            Quality = quality;
        }
    }
}
