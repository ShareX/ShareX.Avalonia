using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Manipulations;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class ScaleDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        public ScaleDialog()
        {
            AvaloniaXamlLoader.Load(this);
            AttachedToVisualTree += (s, e) => RequestPreview();
        }

        private float GetFloat(string name, float fallback)
        {
            NumericUpDown? control = this.FindControl<NumericUpDown>(name);
            return (float)(control?.Value ?? (decimal)fallback);
        }

        private ScaleImageEffect CreateEffect()
        {
            return new ScaleImageEffect
            {
                WidthPercentage = GetFloat("WidthPercentageInput", 100f),
                HeightPercentage = GetFloat("HeightPercentageInput", 0f)
            };
        }

        private void OnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            if (!IsLoaded) return;
            RequestPreview();
        }

        private void RequestPreview()
        {
            PreviewRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Scale"));
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            ApplyRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Applied Scale"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
