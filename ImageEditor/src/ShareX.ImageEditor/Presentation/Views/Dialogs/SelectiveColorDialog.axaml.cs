using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Adjustments;
using SkiaSharp;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class SelectiveColorDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        private bool _suppressPreview = false;

        // Store values for each range in memory so users can adjust multiple ranges?
        // Usually Selective Color applies adjustments cumulatively or user switches tabs.
        // For simplicity: One range at a time? Or store state?
        // Standard behavior: The dialog builds a "recipe" of adjustments. 
        // But PreviewEffect usually takes ONE function that generates the bitmap from source.
        // If we want multiple ranges, we need to apply them sequentially in one pass.
        // To do that, we need to store the settings for ALL ranges.

        private Dictionary<SelectiveColorRange, SelectiveColorAdjustment> _adjustments = new();

        public SelectiveColorDialog()
        {
            AvaloniaXamlLoader.Load(this);

            // Initialize dictionary
            foreach (SelectiveColorRange r in Enum.GetValues(typeof(SelectiveColorRange)))
            {
                _adjustments[r] = new SelectiveColorAdjustment(0, 0, 0);
            }
        }

        private void OnRangeChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded || _suppressPreview) return;

            // Safe guard if FindControl fails during init
            var combo = this.FindControl<ComboBox>("RangeComboBox");
            if (combo == null) return;

            // When range changes, update sliders to reflect stored values for that range
            if (combo.SelectedIndex >= 0)
            {
                var range = (SelectiveColorRange)combo.SelectedIndex;
                var adj = _adjustments[range];

                var hueSlider = this.FindControl<Slider>("HueSlider");
                var satSlider = this.FindControl<Slider>("SatSlider");
                var lightSlider = this.FindControl<Slider>("LightSlider");
                if (hueSlider == null || satSlider == null || lightSlider == null) return;

                _suppressPreview = true;
                hueSlider.Value = adj.Hue;
                satSlider.Value = adj.Saturation;
                lightSlider.Value = adj.Lightness;
                _suppressPreview = false;
            }
        }

        private void OnValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_suppressPreview) return;

            // Update stored value for current range
            var combo = this.FindControl<ComboBox>("RangeComboBox");
            if (combo != null && combo.SelectedIndex >= 0)
            {
                var range = (SelectiveColorRange)combo.SelectedIndex;
                float h = (float)(this.FindControl<Slider>("HueSlider")?.Value ?? 0);
                float s = (float)(this.FindControl<Slider>("SatSlider")?.Value ?? 0);
                float l = (float)(this.FindControl<Slider>("LightSlider")?.Value ?? 0);

                _adjustments[range] = new SelectiveColorAdjustment(h, s, l);

                RequestPreview();
            }
        }

        private void RequestPreview()
        {
            PreviewRequested?.Invoke(this, new EffectEventArgs(img => ApplyAllAdjustments(img), "Selective Color Adjustment"));
        }

        private SKBitmap ApplyAllAdjustments(SKBitmap source)
        {
            // Create the effect and apply it
            var effect = new SelectiveColorImageEffect
            {
                Adjustments = new Dictionary<SelectiveColorRange, SelectiveColorAdjustment>(_adjustments)
            };
            return effect.Apply(source);
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            ApplyRequested?.Invoke(this, new EffectEventArgs(img => ApplyAllAdjustments(img), "Applied Selective Color"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
