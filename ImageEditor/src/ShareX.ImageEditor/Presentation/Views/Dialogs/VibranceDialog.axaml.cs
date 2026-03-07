using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Adjustments;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class VibranceDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public VibranceDialog()
    {
        AvaloniaXamlLoader.Load(this);
        AttachedToVisualTree += (s, e) => RequestPreview();
    }

    private float GetAmount() => (float)(this.FindControl<Slider>("AmountSlider")?.Value ?? 25d);

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void RequestPreview()
    {
        float amount = GetAmount();
        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => new VibranceImageEffect { Amount = amount }.Apply(img),
            $"Vibrance: {amount:0}"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        float amount = GetAmount();
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => new VibranceImageEffect { Amount = amount }.Apply(img),
            $"Applied Vibrance {amount:0}"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}

