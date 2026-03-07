using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Adjustments;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class ThresholdDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public ThresholdDialog()
    {
        AvaloniaXamlLoader.Load(this);
        AttachedToVisualTree += (s, e) => RequestPreview();
    }

    private int GetValueThreshold()
    {
        double value = this.FindControl<Slider>("ValueSlider")?.Value ?? 128d;
        return (int)Math.Round(value);
    }

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void RequestPreview()
    {
        int threshold = GetValueThreshold();
        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => new ThresholdImageEffect { Value = threshold }.Apply(img),
            $"Threshold: {threshold}"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        int threshold = GetValueThreshold();
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => new ThresholdImageEffect { Value = threshold }.Apply(img),
            $"Applied Threshold ({threshold})"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}

