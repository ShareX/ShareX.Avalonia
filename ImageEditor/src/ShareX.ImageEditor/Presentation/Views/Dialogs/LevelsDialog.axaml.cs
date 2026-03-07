using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Adjustments;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class LevelsDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public LevelsDialog()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private int GetInt(string controlName, int fallback)
    {
        decimal value = this.FindControl<NumericUpDown>(controlName)?.Value ?? fallback;
        return (int)Math.Round((double)value);
    }

    private float GetFloat(string controlName, float fallback)
    {
        decimal value = this.FindControl<NumericUpDown>(controlName)?.Value ?? (decimal)fallback;
        return (float)value;
    }

    private LevelsImageEffect CreateEffect()
    {
        return new LevelsImageEffect
        {
            InputBlack = GetInt("InputBlackInput", 0),
            InputWhite = GetInt("InputWhiteInput", 255),
            Gamma = GetFloat("GammaInput", 1f),
            OutputBlack = GetInt("OutputBlackInput", 0),
            OutputWhite = GetInt("OutputWhiteInput", 255)
        };
    }

    private void OnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void RequestPreview()
    {
        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => CreateEffect().Apply(img),
            "Levels"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => CreateEffect().Apply(img),
            "Applied Levels"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
