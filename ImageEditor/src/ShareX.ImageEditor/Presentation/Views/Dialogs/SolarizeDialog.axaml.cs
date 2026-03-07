using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Adjustments;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class SolarizeDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public SolarizeDialog()
    {
        AvaloniaXamlLoader.Load(this);
        AttachedToVisualTree += (s, e) => RequestPreview();
    }

    private int GetThreshold()
    {
        double value = this.FindControl<Slider>("ThresholdSlider")?.Value ?? 128d;
        return (int)Math.Round(value);
    }

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void RequestPreview()
    {
        int threshold = GetThreshold();
        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => new SolarizeImageEffect { Threshold = threshold }.Apply(img),
            $"Solarize: t={threshold}"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        int threshold = GetThreshold();
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => new SolarizeImageEffect { Threshold = threshold }.Apply(img),
            $"Applied Solarize (t={threshold})"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}

