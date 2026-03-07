using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Adjustments;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class ShadowsHighlightsDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public ShadowsHighlightsDialog()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private float GetShadows() => (float)(this.FindControl<Slider>("ShadowsSlider")?.Value ?? 0d);
    private float GetHighlights() => (float)(this.FindControl<Slider>("HighlightsSlider")?.Value ?? 0d);

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void RequestPreview()
    {
        float shadows = GetShadows();
        float highlights = GetHighlights();

        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => new ShadowsHighlightsImageEffect { Shadows = shadows, Highlights = highlights }.Apply(img),
            $"Shadows/Highlights: {shadows:0}, {highlights:0}"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        float shadows = GetShadows();
        float highlights = GetHighlights();

        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => new ShadowsHighlightsImageEffect { Shadows = shadows, Highlights = highlights }.Apply(img),
            "Applied Shadows / Highlights"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
