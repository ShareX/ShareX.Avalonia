using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Presentation.Controls;
using ShareX.ImageEditor.Core.ImageEffects.Drawings;
using SkiaSharp;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class DrawTextDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        public DrawTextDialog()
        {
            AvaloniaXamlLoader.Load(this);

            SubscribeColorPicker("FillColorPicker");
            SubscribeColorPicker("OutlineColorPicker");
            SubscribeColorPicker("ShadowColorPicker");

            AttachedToVisualTree += (s, e) => RequestPreview();
        }

        private void SubscribeColorPicker(string name)
        {
            ColorPickerDropdown? picker = this.FindControl<ColorPickerDropdown>(name);
            if (picker != null)
            {
                picker.PropertyChanged += OnColorPickerPropertyChanged;
            }
        }

        private void OnColorPickerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ColorPickerDropdown.SelectedColorValueProperty && IsLoaded)
            {
                RequestPreview();
            }
        }

        private int GetInt(string name, int fallback)
        {
            NumericUpDown? control = this.FindControl<NumericUpDown>(name);
            return (int)Math.Round(control?.Value ?? fallback);
        }

        private float GetFloat(string name, float fallback)
        {
            NumericUpDown? control = this.FindControl<NumericUpDown>(name);
            return control?.Value is decimal value ? (float)value : fallback;
        }

        private bool GetBool(string name, bool fallback = false)
        {
            return this.FindControl<CheckBox>(name)?.IsChecked ?? fallback;
        }

        private string GetText(string name, string fallback)
        {
            return this.FindControl<TextBox>(name)?.Text ?? fallback;
        }

        private DrawingPlacement GetPlacement()
        {
            return this.FindControl<ComboBox>("PlacementComboBox")?.SelectedIndex switch
            {
                1 => DrawingPlacement.TopCenter,
                2 => DrawingPlacement.TopRight,
                3 => DrawingPlacement.MiddleLeft,
                4 => DrawingPlacement.MiddleCenter,
                5 => DrawingPlacement.MiddleRight,
                6 => DrawingPlacement.BottomLeft,
                7 => DrawingPlacement.BottomCenter,
                8 => DrawingPlacement.BottomRight,
                _ => DrawingPlacement.TopLeft
            };
        }

        private static SKColor ToSkColor(Avalonia.Media.Color color)
        {
            return new SKColor(color.R, color.G, color.B, color.A);
        }

        private DrawTextEffect CreateEffect()
        {
            ColorPickerDropdown? fillColorPicker = this.FindControl<ColorPickerDropdown>("FillColorPicker");
            ColorPickerDropdown? outlineColorPicker = this.FindControl<ColorPickerDropdown>("OutlineColorPicker");
            ColorPickerDropdown? shadowColorPicker = this.FindControl<ColorPickerDropdown>("ShadowColorPicker");

            return new DrawTextEffect
            {
                Text = GetText("TextContentInput", "Text"),
                Placement = GetPlacement(),
                Offset = new SKPointI(GetInt("OffsetXInput", 0), GetInt("OffsetYInput", 0)),
                Angle = GetInt("AngleInput", 0),
                AutoHide = GetBool("AutoHideCheckBox"),
                FontFamily = GetText("FontFamilyTextBox", "Arial"),
                FontSize = GetFloat("FontSizeInput", 36f),
                Bold = GetBool("BoldCheckBox"),
                Italic = GetBool("ItalicCheckBox"),
                Color = ToSkColor(fillColorPicker?.SelectedColorValue ?? Avalonia.Media.Color.FromArgb(255, 235, 235, 235)),
                Outline = GetBool("OutlineCheckBox"),
                OutlineSize = GetInt("OutlineSizeInput", 5),
                OutlineColor = ToSkColor(outlineColorPicker?.SelectedColorValue ?? Avalonia.Media.Color.FromArgb(255, 235, 0, 0)),
                Shadow = GetBool("ShadowCheckBox"),
                ShadowOffset = new SKPointI(GetInt("ShadowOffsetXInput", 0), GetInt("ShadowOffsetYInput", 5)),
                ShadowColor = ToSkColor(shadowColorPicker?.SelectedColorValue ?? Avalonia.Media.Color.FromArgb(125, 0, 0, 0))
            };
        }

        private void RequestPreview()
        {
            PreviewRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Text"));
        }

        private void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (IsLoaded) RequestPreview();
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) RequestPreview();
        }

        private void OnNumericChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            if (IsLoaded) RequestPreview();
        }

        private void OnSettingChanged(object? sender, RoutedEventArgs e)
        {
            if (IsLoaded) RequestPreview();
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            ApplyRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Applied text"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
