using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Adjustments;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class AutoContrastDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public AutoContrastDialog()
    {
        AvaloniaXamlLoader.Load(this);
        AttachedToVisualTree += (s, e) => RequestPreview();
    }

    private float GetClipPercent()
    {
        double value = this.FindControl<Slider>("ClipSlider")?.Value ?? 0.5d;
        return (float)value;
    }

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void RequestPreview()
    {
        float clip = GetClipPercent();
        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => new AutoContrastImageEffect { ClipPercent = clip }.Apply(img),
            $"Auto contrast: {clip:0.##}% clip"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        float clip = GetClipPercent();
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => new AutoContrastImageEffect { ClipPercent = clip }.Apply(img),
            $"Applied Auto contrast ({clip:0.##}% clip)"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}

