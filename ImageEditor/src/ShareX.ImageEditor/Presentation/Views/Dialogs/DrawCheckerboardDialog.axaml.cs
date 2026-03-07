using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Presentation.Controls;
using ShareX.ImageEditor.Core.ImageEffects.Drawings;
using SkiaSharp;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class DrawCheckerboardDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        private ColorPickerDropdown? _color1Picker;
        private ColorPickerDropdown? _color2Picker;
        private Slider? _sizeSlider;

        public DrawCheckerboardDialog()
        {
            AvaloniaXamlLoader.Load(this);

            _color1Picker = this.FindControl<ColorPickerDropdown>("Color1Picker");
            _color2Picker = this.FindControl<ColorPickerDropdown>("Color2Picker");
            _sizeSlider = this.FindControl<Slider>("SizeSlider");

            if (_color1Picker != null)
            {
                _color1Picker.PropertyChanged += OnColorPickerPropertyChanged;
            }

            if (_color2Picker != null)
            {
                _color2Picker.PropertyChanged += OnColorPickerPropertyChanged;
            }

            AttachedToVisualTree += (s, e) => RequestPreview();
        }

        private void OnColorPickerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ColorPickerDropdown.SelectedColorValueProperty && IsLoaded)
            {
                RequestPreview();
            }
        }

        private DrawCheckerboardEffect CreateEffect()
        {
            Avalonia.Media.Color color1 = _color1Picker?.SelectedColorValue ?? Avalonia.Media.Color.FromArgb(255, 211, 211, 211);
            Avalonia.Media.Color color2 = _color2Picker?.SelectedColorValue ?? Avalonia.Media.Colors.White;

            return new DrawCheckerboardEffect
            {
                Size = (int)Math.Round(_sizeSlider?.Value ?? 10),
                Color = new SKColor(color1.R, color1.G, color1.B, color1.A),
                Color2 = new SKColor(color2.R, color2.G, color2.B, color2.A)
            };
        }

        private void RequestPreview()
        {
            PreviewRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Checkerboard"));
        }

        private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            if (IsLoaded) RequestPreview();
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            ApplyRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Applied checkerboard"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

