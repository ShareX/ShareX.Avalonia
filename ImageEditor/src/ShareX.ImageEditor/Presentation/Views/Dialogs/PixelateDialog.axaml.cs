using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Filters;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class PixelateDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        public PixelateDialog()
        {
            AvaloniaXamlLoader.Load(this);
            this.AttachedToVisualTree += (s, e) => RequestPreview();
        }

        private void OnValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            RequestPreview();
        }

        private void RequestPreview()
        {
            int size = (int)(this.FindControl<Slider>("SizeSlider")?.Value ?? 10);
            PreviewRequested?.Invoke(this, new EffectEventArgs(img => new PixelateImageEffect { Size = size }.Apply(img), "Pixelate"));
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            int size = (int)(this.FindControl<Slider>("SizeSlider")?.Value ?? 10);
            ApplyRequested?.Invoke(this, new EffectEventArgs(img => new PixelateImageEffect { Size = size }.Apply(img), "Applied Pixelate"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

