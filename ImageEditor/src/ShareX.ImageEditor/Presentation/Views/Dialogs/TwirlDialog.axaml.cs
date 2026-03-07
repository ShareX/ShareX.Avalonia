using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Manipulations;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class TwirlDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        public TwirlDialog()
        {
            AvaloniaXamlLoader.Load(this);
            AttachedToVisualTree += (s, e) => RequestPreview();
        }

        private float GetSliderValue(string name, float fallback)
        {
            Slider? slider = this.FindControl<Slider>(name);
            return (float)(slider?.Value ?? fallback);
        }

        private TwirlImageEffect CreateEffect()
        {
            return new TwirlImageEffect
            {
                Angle = GetSliderValue("AngleSlider", 90f),
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
                "Twirl"));
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            ApplyRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Applied Twirl"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
