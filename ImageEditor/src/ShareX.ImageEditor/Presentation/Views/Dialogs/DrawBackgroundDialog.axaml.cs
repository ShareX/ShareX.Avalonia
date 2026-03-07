using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Presentation.Controls;
using ShareX.ImageEditor.Core.ImageEffects.Drawings;
using SkiaSharp;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class DrawBackgroundDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        private ColorPickerDropdown? _colorPicker;

        public DrawBackgroundDialog()
        {
            AvaloniaXamlLoader.Load(this);

            _colorPicker = this.FindControl<ColorPickerDropdown>("BackgroundColorPicker");

            if (_colorPicker != null)
            {
                _colorPicker.PropertyChanged += OnColorPickerPropertyChanged;
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

        private DrawBackgroundEffect CreateEffect()
        {
            Avalonia.Media.Color selectedColor = _colorPicker?.SelectedColorValue ?? Avalonia.Media.Colors.Black;

            return new DrawBackgroundEffect
            {
                Color = new SKColor(selectedColor.R, selectedColor.G, selectedColor.B, selectedColor.A)
            };
        }

        private void RequestPreview()
        {
            PreviewRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Background"));
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            ApplyRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Applied background"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
