using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Filters;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class MedianFilterDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public MedianFilterDialog()
    {
        AvaloniaXamlLoader.Load(this);
        AttachedToVisualTree += (s, e) => RequestPreview();
    }

    private int GetRadius()
    {
        double value = this.FindControl<Slider>("RadiusSlider")?.Value ?? 1d;
        return (int)Math.Round(value);
    }

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void RequestPreview()
    {
        int radius = GetRadius();
        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => new MedianFilterImageEffect { Radius = radius }.Apply(img),
            $"Median filter: r={radius}"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        int radius = GetRadius();
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => new MedianFilterImageEffect { Radius = radius }.Apply(img),
            $"Applied Median filter r={radius}"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
