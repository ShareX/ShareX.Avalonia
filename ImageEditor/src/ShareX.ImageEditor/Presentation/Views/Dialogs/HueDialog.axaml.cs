using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Adjustments;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class HueDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        private bool _suppressPreview = false;

        public HueDialog()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_suppressPreview) return;
            RequestPreview();
        }

        private void RequestPreview()
        {
            var slider = this.FindControl<Slider>("AmountSlider");
            float amount = (float)(slider?.Value ?? 0);

            PreviewRequested?.Invoke(this, new EffectEventArgs(img => new HueImageEffect { Amount = amount }.Apply(img), $"Hue: {amount:0}°"));
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            var slider = this.FindControl<Slider>("AmountSlider");
            float amount = (float)(slider?.Value ?? 0);

            ApplyRequested?.Invoke(this, new EffectEventArgs(img => new HueImageEffect { Amount = amount }.Apply(img), $"Adjusted hue by {amount:0}°"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

