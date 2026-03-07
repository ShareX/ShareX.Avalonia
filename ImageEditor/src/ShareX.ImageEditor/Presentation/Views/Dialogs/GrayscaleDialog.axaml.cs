using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Adjustments;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class GrayscaleDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        public GrayscaleDialog()
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
            float strength = (float)(this.FindControl<Slider>("StrengthSlider")?.Value ?? 100);
            PreviewRequested?.Invoke(this, new EffectEventArgs(img => new GrayscaleImageEffect { Strength = strength }.Apply(img), "Grayscale"));
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            float strength = (float)(this.FindControl<Slider>("StrengthSlider")?.Value ?? 100);
            ApplyRequested?.Invoke(this, new EffectEventArgs(img => new GrayscaleImageEffect { Strength = strength }.Apply(img), "Applied Grayscale"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

