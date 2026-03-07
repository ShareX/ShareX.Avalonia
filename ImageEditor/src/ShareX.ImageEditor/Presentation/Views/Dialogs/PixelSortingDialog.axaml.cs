using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Filters;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class PixelSortingDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public PixelSortingDialog()
    {
        AvaloniaXamlLoader.Load(this);
        AttachedToVisualTree += (s, e) => RequestPreview();
    }

    private float GetValue(string controlName, double fallback)
    {
        return (float)(this.FindControl<Slider>(controlName)?.Value ?? fallback);
    }

    private PixelSortDirection GetDirection()
    {
        int index = this.FindControl<ComboBox>("DirectionComboBox")?.SelectedIndex ?? 1;
        return index == 0 ? PixelSortDirection.Horizontal : PixelSortDirection.Vertical;
    }

    private PixelSortMetric GetMetric()
    {
        int index = this.FindControl<ComboBox>("MetricComboBox")?.SelectedIndex ?? 0;
        return index == 1 ? PixelSortMetric.Hue : PixelSortMetric.Brightness;
    }

    private PixelSortingImageEffect CreateEffect()
    {
        return new PixelSortingImageEffect
        {
            Direction = GetDirection(),
            Metric = GetMetric(),
            ThresholdLow = GetValue("ThresholdLowSlider", 12d),
            ThresholdHigh = GetValue("ThresholdHighSlider", 85d),
            MinSpanLength = (int)Math.Round(GetValue("MinSpanSlider", 8d)),
            MaxSpanLength = (int)Math.Round(GetValue("MaxSpanSlider", 120d)),
            SortProbability = GetValue("ProbabilitySlider", 85d)
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

    private void RequestPreview()
    {
        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => CreateEffect().Apply(img),
            "Pixel sorting"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => CreateEffect().Apply(img),
            "Applied Pixel sorting"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
