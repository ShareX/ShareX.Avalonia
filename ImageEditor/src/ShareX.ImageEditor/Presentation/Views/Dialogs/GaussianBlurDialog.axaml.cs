using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Filters;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class GaussianBlurDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        public GaussianBlurDialog()
        {
            AvaloniaXamlLoader.Load(this);
            AttachedToVisualTree += (s, e) => RequestPreview();
        }

        private void OnValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!IsLoaded) return;
            RequestPreview();
        }

        private int GetRadius()
        {
            var slider = this.FindControl<Slider>("RadiusSlider");
            return (int)(slider?.Value ?? 15);
        }

        private void RequestPreview()
        {
            int radius = GetRadius();
            PreviewRequested?.Invoke(this, new EffectEventArgs(
                img => new GaussianBlurImageEffect { Radius = radius }.Apply(img),
                $"Gaussian blur: {radius}"));
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            int radius = GetRadius();
            ApplyRequested?.Invoke(this, new EffectEventArgs(
                img => new GaussianBlurImageEffect { Radius = radius }.Apply(img),
                $"Applied Gaussian blur: {radius}"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
