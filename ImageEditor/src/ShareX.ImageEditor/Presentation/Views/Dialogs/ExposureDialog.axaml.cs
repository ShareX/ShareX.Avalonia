using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Adjustments;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class ExposureDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public ExposureDialog()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private float GetAmount() => (float)(this.FindControl<Slider>("AmountSlider")?.Value ?? 0d);

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void RequestPreview()
    {
        float amount = GetAmount();
        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => new ExposureImageEffect { Amount = amount }.Apply(img),
            $"Exposure: {amount:0.0}"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        float amount = GetAmount();
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => new ExposureImageEffect { Amount = amount }.Apply(img),
            $"Applied Exposure {amount:0.0}"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
