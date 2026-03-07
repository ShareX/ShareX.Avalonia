using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Presentation.Controls;
using ShareX.ImageEditor.Core.ImageEffects.Adjustments;
using SkiaSharp;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class DuotoneGradientMapDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public DuotoneGradientMapDialog()
    {
        AvaloniaXamlLoader.Load(this);

        SubscribeColorPicker("ShadowColorPicker");
        SubscribeColorPicker("MidtoneColorPicker");
        SubscribeColorPicker("HighlightColorPicker");

        AttachedToVisualTree += (s, e) => RequestPreview();
    }

    private void SubscribeColorPicker(string controlName)
    {
        ColorPickerDropdown? picker = this.FindControl<ColorPickerDropdown>(controlName);
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

    private float GetValue(string controlName, double fallback)
    {
        return (float)(this.FindControl<Slider>(controlName)?.Value ?? fallback);
    }

    private static SKColor ToSkColor(Avalonia.Media.Color color)
    {
        return new SKColor(color.R, color.G, color.B, color.A);
    }

    private DuotoneGradientMapImageEffect CreateEffect()
    {
        Avalonia.Media.Color shadow = this.FindControl<ColorPickerDropdown>("ShadowColorPicker")?.SelectedColorValue
            ?? Avalonia.Media.Color.FromArgb(255, 24, 28, 78);
        Avalonia.Media.Color midtone = this.FindControl<ColorPickerDropdown>("MidtoneColorPicker")?.SelectedColorValue
            ?? Avalonia.Media.Color.FromArgb(255, 182, 60, 132);
        Avalonia.Media.Color highlight = this.FindControl<ColorPickerDropdown>("HighlightColorPicker")?.SelectedColorValue
            ?? Avalonia.Media.Color.FromArgb(255, 255, 224, 132);

        return new DuotoneGradientMapImageEffect
        {
            ShadowColor = ToSkColor(shadow),
            MidtoneColor = ToSkColor(midtone),
            HighlightColor = ToSkColor(highlight),
            Contrast = GetValue("ContrastSlider", 110d),
            Gamma = GetValue("GammaSlider", 1d),
            Blend = GetValue("BlendSlider", 100d)
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
            "Duotone / Gradient map"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => CreateEffect().Apply(img),
            "Applied Duotone / Gradient map"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
