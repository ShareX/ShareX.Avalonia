using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Filters;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class MatrixDigitalRainDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public MatrixDigitalRainDialog()
    {
        AvaloniaXamlLoader.Load(this);
        AttachedToVisualTree += (s, e) => RequestPreview();
    }

    private float GetValue(string controlName, double fallback)
    {
        return (float)(this.FindControl<Slider>(controlName)?.Value ?? fallback);
    }

    private MatrixDigitalRainImageEffect CreateEffect()
    {
        return new MatrixDigitalRainImageEffect
        {
            CellSize = (int)Math.Round(GetValue("CellSizeSlider", 12d)),
            Density = GetValue("DensitySlider", 85d),
            TrailLength = (int)Math.Round(GetValue("TrailSlider", 12d)),
            GlowAmount = GetValue("GlowSlider", 40d),
            SourceBlend = GetValue("SourceBlendSlider", 22d),
            RainOffset = GetValue("OffsetSlider", 0d),
            LuminanceInfluence = GetValue("LuminanceSlider", 65d),
            CharacterSet = this.FindControl<TextBox>("CharacterSetTextBox")?.Text ?? "01<>[]{}*+-/\\=#$%&"
        };
    }

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void RequestPreview()
    {
        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => CreateEffect().Apply(img),
            "Matrix digital rain"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => CreateEffect().Apply(img),
            "Applied Matrix digital rain"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
