using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Filters;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class SharpenDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        public SharpenDialog()
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
            int strength = (int)(this.FindControl<Slider>("StrengthSlider")?.Value ?? 50);
            PreviewRequested?.Invoke(this, new EffectEventArgs(img => new SharpenImageEffect { Strength = strength }.Apply(img), "Sharpen"));
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            int strength = (int)(this.FindControl<Slider>("StrengthSlider")?.Value ?? 50);
            ApplyRequested?.Invoke(this, new EffectEventArgs(img => new SharpenImageEffect { Strength = strength }.Apply(img), "Applied Sharpen"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

