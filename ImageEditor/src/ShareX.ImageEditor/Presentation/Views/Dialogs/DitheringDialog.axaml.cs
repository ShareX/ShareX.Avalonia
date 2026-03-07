using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Filters;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class DitheringDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public DitheringDialog()
    {
        AvaloniaXamlLoader.Load(this);
        AttachedToVisualTree += (s, e) => RequestPreview();
    }

    private float GetValue(string controlName, double fallback)
    {
        return (float)(this.FindControl<Slider>(controlName)?.Value ?? fallback);
    }

    private bool GetBool(string controlName, bool fallback = false)
    {
        return this.FindControl<CheckBox>(controlName)?.IsChecked ?? fallback;
    }

    private DitheringMethod GetMethod()
    {
        int index = this.FindControl<ComboBox>("MethodComboBox")?.SelectedIndex ?? 0;
        return index == 1 ? DitheringMethod.Bayer4x4 : DitheringMethod.FloydSteinberg;
    }

    private DitheringPalette GetPalette()
    {
        int index = this.FindControl<ComboBox>("PaletteComboBox")?.SelectedIndex ?? 0;
        return index switch
        {
            1 => DitheringPalette.WebSafe216,
            2 => DitheringPalette.RGB332,
            3 => DitheringPalette.Grayscale4,
            _ => DitheringPalette.OneBitBW
        };
    }

    private DitheringImageEffect CreateEffect()
    {
        return new DitheringImageEffect
        {
            Method = GetMethod(),
            Palette = GetPalette(),
            Serpentine = GetBool("SerpentineCheckBox", true),
            Strength = GetValue("StrengthSlider", 100d)
        };
    }

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void OnSettingChanged(object? sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void RequestPreview()
    {
        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => CreateEffect().Apply(img),
            "Dithering"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => CreateEffect().Apply(img),
            "Applied Dithering"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
