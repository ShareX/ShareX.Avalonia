using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Filters;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class BlockGlitchDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public BlockGlitchDialog()
    {
        AvaloniaXamlLoader.Load(this);
        AttachedToVisualTree += (s, e) => RequestPreview();
    }

    private float GetValue(string controlName, double fallback)
    {
        return (float)(this.FindControl<Slider>(controlName)?.Value ?? fallback);
    }

    private BlockGlitchImageEffect CreateEffect()
    {
        return new BlockGlitchImageEffect
        {
            BlockCount = (int)Math.Round(GetValue("BlockCountSlider", 36d)),
            MinBlockWidth = (int)Math.Round(GetValue("MinWidthSlider", 24d)),
            MaxBlockWidth = (int)Math.Round(GetValue("MaxWidthSlider", 200d)),
            MinBlockHeight = (int)Math.Round(GetValue("MinHeightSlider", 6d)),
            MaxBlockHeight = (int)Math.Round(GetValue("MaxHeightSlider", 50d)),
            MaxDisplacement = (int)Math.Round(GetValue("DisplacementSlider", 50d)),
            ChannelShift = (int)Math.Round(GetValue("ChannelShiftSlider", 4d)),
            NoiseAmount = GetValue("NoiseSlider", 10d)
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
            "Block glitch / Databending"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => CreateEffect().Apply(img),
            "Applied Block glitch / Databending"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
