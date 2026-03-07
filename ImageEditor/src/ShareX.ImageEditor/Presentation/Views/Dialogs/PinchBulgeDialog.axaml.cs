using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Manipulations;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class PinchBulgeDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        public PinchBulgeDialog()
        {
            AvaloniaXamlLoader.Load(this);
            AttachedToVisualTree += (s, e) => RequestPreview();
        }

        private float GetSliderValue(string name, float fallback)
        {
            Slider? slider = this.FindControl<Slider>(name);
            return (float)(slider?.Value ?? fallback);
        }

        private PinchBulgeImageEffect CreateEffect()
        {
            return new PinchBulgeImageEffect
            {
                Strength = GetSliderValue("StrengthSlider", 35f),
                RadiusPercentage = GetSliderValue("RadiusSlider", 50f),
                CenterXPercentage = GetSliderValue("CenterXSlider", 50f),
                CenterYPercentage = GetSliderValue("CenterYSlider", 50f)
            };
        }

        private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            if (!IsLoaded) return;
            RequestPreview();
        }

        private void RequestPreview()
        {
            PreviewRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Pinch / bulge"));
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            ApplyRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Applied Pinch / bulge"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
