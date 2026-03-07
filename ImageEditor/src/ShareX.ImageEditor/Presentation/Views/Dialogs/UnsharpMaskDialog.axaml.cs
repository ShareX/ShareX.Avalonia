using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Filters;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class UnsharpMaskDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public UnsharpMaskDialog()
    {
        AvaloniaXamlLoader.Load(this);
        AttachedToVisualTree += (s, e) => RequestPreview();
    }

    private float GetFloat(string controlName, float fallback)
    {
        double value = this.FindControl<Slider>(controlName)?.Value ?? fallback;
        return (float)value;
    }

    private int GetInt(string controlName, int fallback)
    {
        double value = this.FindControl<Slider>(controlName)?.Value ?? fallback;
        return (int)Math.Round(value);
    }

    private UnsharpMaskImageEffect CreateEffect()
    {
        return new UnsharpMaskImageEffect
        {
            Radius = GetFloat("RadiusSlider", 5f),
            Amount = GetFloat("AmountSlider", 150f),
            Threshold = GetInt("ThresholdSlider", 0)
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
            "Unsharp mask"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => CreateEffect().Apply(img),
            "Applied Unsharp mask"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
