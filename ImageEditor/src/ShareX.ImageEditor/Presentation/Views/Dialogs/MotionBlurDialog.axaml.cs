using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Filters;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class MotionBlurDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public MotionBlurDialog()
    {
        AvaloniaXamlLoader.Load(this);
        AttachedToVisualTree += (s, e) => RequestPreview();
    }

    private int GetDistance()
    {
        double value = this.FindControl<Slider>("DistanceSlider")?.Value ?? 12d;
        return (int)Math.Round(value);
    }

    private float GetAngle()
    {
        double value = this.FindControl<Slider>("AngleSlider")?.Value ?? 0d;
        return (float)value;
    }

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void RequestPreview()
    {
        int distance = GetDistance();
        float angle = GetAngle();
        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => new MotionBlurImageEffect { Distance = distance, Angle = angle }.Apply(img),
            $"Motion blur: d={distance}, a={angle:0.##}"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        int distance = GetDistance();
        float angle = GetAngle();
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => new MotionBlurImageEffect { Distance = distance, Angle = angle }.Apply(img),
            $"Applied Motion blur d={distance}, a={angle:0.##}"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
