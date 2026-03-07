using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Adjustments;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class PosterizeDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public PosterizeDialog()
    {
        AvaloniaXamlLoader.Load(this);
        AttachedToVisualTree += (s, e) => RequestPreview();
    }

    private int GetLevels()
    {
        double value = this.FindControl<Slider>("LevelsSlider")?.Value ?? 8d;
        return (int)Math.Round(value);
    }

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void RequestPreview()
    {
        int levels = GetLevels();
        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => new PosterizeImageEffect { Levels = levels }.Apply(img),
            $"Posterize: {levels} levels"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        int levels = GetLevels();
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => new PosterizeImageEffect { Levels = levels }.Apply(img),
            $"Applied Posterize ({levels} levels)"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}

