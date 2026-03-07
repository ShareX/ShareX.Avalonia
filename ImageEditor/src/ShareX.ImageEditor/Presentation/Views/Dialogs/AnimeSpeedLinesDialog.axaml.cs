using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Filters;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class AnimeSpeedLinesDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public AnimeSpeedLinesDialog()
    {
        AvaloniaXamlLoader.Load(this);
        AttachedToVisualTree += (s, e) => RequestPreview();
    }

    private float GetValue(string controlName, double fallback)
    {
        return (float)(this.FindControl<Slider>(controlName)?.Value ?? fallback);
    }

    private AnimeSpeedLinesImageEffect CreateEffect()
    {
        return new AnimeSpeedLinesImageEffect
        {
            Density = GetValue("DensitySlider", 70d),
            Strength = GetValue("StrengthSlider", 65d),
            FocusRadius = GetValue("FocusSlider", 18d),
            CenterX = GetValue("CenterXSlider", 50d),
            CenterY = GetValue("CenterYSlider", 50d),
            Contrast = GetValue("ContrastSlider", 35d)
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
            "Anime speed lines"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => CreateEffect().Apply(img),
            "Applied Anime speed lines"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}

