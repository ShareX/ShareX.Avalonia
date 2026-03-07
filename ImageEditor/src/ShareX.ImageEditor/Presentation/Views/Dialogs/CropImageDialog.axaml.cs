using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class CropImageDialog : UserControl
    {
        public static readonly StyledProperty<int> CropXProperty =
            AvaloniaProperty.Register<CropImageDialog, int>(nameof(CropX), 0);

        public static readonly StyledProperty<int> CropYProperty =
            AvaloniaProperty.Register<CropImageDialog, int>(nameof(CropY), 0);

        public static readonly StyledProperty<int> CropWidthProperty =
            AvaloniaProperty.Register<CropImageDialog, int>(nameof(CropWidth), 100);

        public static readonly StyledProperty<int> CropHeightProperty =
            AvaloniaProperty.Register<CropImageDialog, int>(nameof(CropHeight), 100);

        public int CropX
        {
            get => GetValue(CropXProperty);
            set => SetValue(CropXProperty, value);
        }

        public int CropY
        {
            get => GetValue(CropYProperty);
            set => SetValue(CropYProperty, value);
        }

        public int CropWidth
        {
            get => GetValue(CropWidthProperty);
            set => SetValue(CropWidthProperty, value);
        }

        public int CropHeight
        {
            get => GetValue(CropHeightProperty);
            set => SetValue(CropHeightProperty, value);
        }

        private int _imageWidth;
        private int _imageHeight;

        public event EventHandler<CropImageEventArgs>? ApplyRequested;
        public event EventHandler? CancelRequested;

        public CropImageDialog()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void Initialize(int imageWidth, int imageHeight)
        {
            _imageWidth = imageWidth;
            _imageHeight = imageHeight;

            // Set initial crop to full image
            CropX = 0;
            CropY = 0;
            CropWidth = imageWidth;
            CropHeight = imageHeight;

            UpdateInfoText();
        }

        private void UpdateInfoText()
        {
            var infoText = this.FindControl<TextBlock>("InfoText");
            if (infoText != null)
            {
                infoText.Text = $"Image size: {_imageWidth} × {_imageHeight} px";
            }
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            // Validate bounds
            int x = Math.Max(0, Math.Min(CropX, _imageWidth - 1));
            int y = Math.Max(0, Math.Min(CropY, _imageHeight - 1));
            int width = Math.Max(1, Math.Min(CropWidth, _imageWidth - x));
            int height = Math.Max(1, Math.Min(CropHeight, _imageHeight - y));

            ApplyRequested?.Invoke(this, new CropImageEventArgs(x, y, width, height));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public class CropImageEventArgs : EventArgs
    {
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }

        public CropImageEventArgs(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}
