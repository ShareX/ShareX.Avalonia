using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Filters;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class ASCIIArtDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public ASCIIArtDialog()
    {
        AvaloniaXamlLoader.Load(this);
        AttachedToVisualTree += (s, e) => RequestPreview();
    }

    private float GetSliderValue(string controlName, double fallback)
    {
        return (float)(this.FindControl<Slider>(controlName)?.Value ?? fallback);
    }

    private bool GetCheckValue(string controlName, bool fallback = false)
    {
        return this.FindControl<CheckBox>(controlName)?.IsChecked ?? fallback;
    }

    private ASCIIArtImageEffect CreateEffect()
    {
        return new ASCIIArtImageEffect
        {
            CellSize = (int)Math.Round(GetSliderValue("CellSizeSlider", 8d)),
            Contrast = GetSliderValue("ContrastSlider", 110d),
            CharacterSet = this.FindControl<TextBox>("CharacterSetTextBox")?.Text ?? "@%#*+=-:. ",
            Invert = GetCheckValue("InvertCheckBox"),
            DarkBackground = GetCheckValue("DarkBackgroundCheckBox", true),
            UseSourceColor = GetCheckValue("UseSourceColorCheckBox", true)
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

    private void OnSettingChanged(object? sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void RequestPreview()
    {
        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => CreateEffect().Apply(img),
            "ASCII art"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => CreateEffect().Apply(img),
            "Applied ASCII art"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
