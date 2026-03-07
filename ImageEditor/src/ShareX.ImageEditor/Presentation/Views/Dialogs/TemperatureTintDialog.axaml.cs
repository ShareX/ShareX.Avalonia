using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Adjustments;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class TemperatureTintDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public TemperatureTintDialog()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private float GetTemperature() => (float)(this.FindControl<Slider>("TemperatureSlider")?.Value ?? 0d);
    private float GetTint() => (float)(this.FindControl<Slider>("TintSlider")?.Value ?? 0d);

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void RequestPreview()
    {
        float temperature = GetTemperature();
        float tint = GetTint();

        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => new TemperatureTintImageEffect { Temperature = temperature, Tint = tint }.Apply(img),
            $"Temperature/Tint: {temperature:0}, {tint:0}"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        float temperature = GetTemperature();
        float tint = GetTint();

        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => new TemperatureTintImageEffect { Temperature = temperature, Tint = tint }.Apply(img),
            "Applied Temperature / Tint"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
