using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Filters;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class AddNoiseDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public AddNoiseDialog()
    {
        AvaloniaXamlLoader.Load(this);
        AttachedToVisualTree += (s, e) => RequestPreview();
    }

    private float GetAmount()
    {
        double value = this.FindControl<Slider>("AmountSlider")?.Value ?? 8d;
        return (float)value;
    }

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void RequestPreview()
    {
        float amount = GetAmount();
        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => new AddNoiseImageEffect { Amount = amount }.Apply(img),
            $"Add noise: {amount:0}%"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        float amount = GetAmount();
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => new AddNoiseImageEffect { Amount = amount }.Apply(img),
            $"Applied Add noise ({amount:0}%)"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}

