using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Filters;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class BlurDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        public BlurDialog()
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
            int radius = (int)(this.FindControl<Slider>("RadiusSlider")?.Value ?? 5);
            PreviewRequested?.Invoke(this, new EffectEventArgs(img => new BlurImageEffect { Radius = radius }.Apply(img), "Blur"));
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            int radius = (int)(this.FindControl<Slider>("RadiusSlider")?.Value ?? 5);
            ApplyRequested?.Invoke(this, new EffectEventArgs(img => new BlurImageEffect { Radius = radius }.Apply(img), "Applied Blur"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

