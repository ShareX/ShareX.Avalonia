using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ShareX.ImageEditor.Core.ImageEffects.Adjustments;
using SkiaSharp;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class ColorizeDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        private bool _suppressPreview = false;

        public ColorizeDialog()
        {
            AvaloniaXamlLoader.Load(this);
            AvaloniaXamlLoader.Load(this);

            // Re-find the brush if it's set in XAML resources or use direct binding
            // Actually, we can just set the property of the Border or a named brush.
            // XAML defines: <SolidColorBrush x:Name="PreviewColorBrush"/>

            // Initial update
            UpdateColorPreview();
            RequestPreview();
        }

        private void OnValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_suppressPreview) return;
            UpdateColorPreview();
            RequestPreview();
        }

        private SKColor GetCurrentColor()
        {
            var hueSlider = this.FindControl<Slider>("HueSlider");
            var satSlider = this.FindControl<Slider>("SaturationSlider");

            float h = (float)(hueSlider?.Value ?? 0);
            float s = (float)(satSlider?.Value ?? 50);

            // Default Lightness to 50% for standard color representation
            return SKColor.FromHsl(h, s, 50);
        }

        private float GetStrength()
        {
            var slider = this.FindControl<Slider>("StrengthSlider");
            return (float)(slider?.Value ?? 100);
        }

        private void UpdateColorPreview()
        {
            var border = this.FindControl<Border>("PreviewColorBox");
            if (border != null)
            {
                var c = GetCurrentColor();
                var color = Color.FromRgb(c.Red, c.Green, c.Blue);

                if (border.Background is SolidColorBrush brush)
                {
                    brush.Color = color;
                }
                else
                {
                    border.Background = new SolidColorBrush(color);
                }
            }
        }

        private void RequestPreview()
        {
            var color = GetCurrentColor();
            float strength = GetStrength();

            PreviewRequested?.Invoke(this, new EffectEventArgs(img => new ColorizeImageEffect { Color = color, Strength = strength }.Apply(img), $"Colorize: Hue {GetHue():0}, Strength {strength:0}%"));
        }

        private float GetHue()
        {
            var hueSlider = this.FindControl<Slider>("HueSlider");
            return (float)(hueSlider?.Value ?? 0);
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            var color = GetCurrentColor();
            float strength = GetStrength();

            ApplyRequested?.Invoke(this, new EffectEventArgs(img => new ColorizeImageEffect { Color = color, Strength = strength }.Apply(img), "Applied Colorize"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

