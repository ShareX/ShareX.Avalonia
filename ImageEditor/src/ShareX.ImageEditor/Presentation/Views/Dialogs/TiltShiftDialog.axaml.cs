using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Filters;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class TiltShiftDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public TiltShiftDialog()
    {
        AvaloniaXamlLoader.Load(this);
        AttachedToVisualTree += (s, e) => RequestPreview();
    }

    private float GetValue(string controlName, double fallback)
    {
        return (float)(this.FindControl<Slider>(controlName)?.Value ?? fallback);
    }

    private TiltShiftMode GetMode()
    {
        int index = this.FindControl<ComboBox>("ModeComboBox")?.SelectedIndex ?? 0;
        return index == 1 ? TiltShiftMode.Radial : TiltShiftMode.Linear;
    }

    private TiltShiftImageEffect CreateEffect()
    {
        return new TiltShiftImageEffect
        {
            Mode = GetMode(),
            BlurRadius = GetValue("BlurRadiusSlider", 12d),
            FocusSize = GetValue("FocusSizeSlider", 30d),
            FocusPositionX = GetValue("FocusXSlider", 50d),
            FocusPositionY = GetValue("FocusYSlider", 50d),
            Falloff = GetValue("FalloffSlider", 24d),
            SaturationBoost = GetValue("SaturationSlider", 35d)
        };
    }

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void OnModeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void RequestPreview()
    {
        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => CreateEffect().Apply(img),
            "Tilt-shift"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => CreateEffect().Apply(img),
            "Applied Tilt-shift"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
