using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Adjustments;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class FilmEmulationDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public FilmEmulationDialog()
    {
        AvaloniaXamlLoader.Load(this);
        AttachedToVisualTree += (s, e) => RequestPreview();
    }

    private float GetValue(string controlName, double fallback)
    {
        return (float)(this.FindControl<Slider>(controlName)?.Value ?? fallback);
    }

    private FilmEmulationImageEffect.FilmEmulationPreset GetPreset()
    {
        int index = this.FindControl<ComboBox>("PresetComboBox")?.SelectedIndex ?? 0;
        return index switch
        {
            1 => FilmEmulationImageEffect.FilmEmulationPreset.Warm,
            2 => FilmEmulationImageEffect.FilmEmulationPreset.Cool,
            3 => FilmEmulationImageEffect.FilmEmulationPreset.Faded,
            4 => FilmEmulationImageEffect.FilmEmulationPreset.CrossProcessed,
            _ => FilmEmulationImageEffect.FilmEmulationPreset.Classic
        };
    }

    private FilmEmulationImageEffect CreateEffect()
    {
        return new FilmEmulationImageEffect
        {
            Preset = GetPreset(),
            ToneStrength = GetValue("ToneSlider", 65d),
            GrainAmount = GetValue("GrainSlider", 12d),
            FadeAmount = GetValue("FadeSlider", 10d),
            ContrastAmount = GetValue("ContrastSlider", 110d)
        };
    }

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void OnPresetChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void RequestPreview()
    {
        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => CreateEffect().Apply(img),
            "Film emulation"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => CreateEffect().Apply(img),
            "Applied Film emulation"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
