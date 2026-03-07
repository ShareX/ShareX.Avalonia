using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Filters;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class CartoonStickerCutoutDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public CartoonStickerCutoutDialog()
    {
        AvaloniaXamlLoader.Load(this);
        AttachedToVisualTree += (s, e) => RequestPreview();
    }

    private float GetValue(string controlName, double fallback)
    {
        return (float)(this.FindControl<Slider>(controlName)?.Value ?? fallback);
    }

    private CartoonStickerCutoutImageEffect CreateEffect()
    {
        return new CartoonStickerCutoutImageEffect
        {
            ColorLevels = (int)Math.Round(GetValue("ColorLevelsSlider", 8d)),
            EdgeThreshold = (int)Math.Round(GetValue("EdgeThresholdSlider", 32d)),
            InkStrength = GetValue("InkStrengthSlider", 85d),
            StickerBorder = (int)Math.Round(GetValue("BorderSizeSlider", 3d)),
            BorderStrength = GetValue("BorderStrengthSlider", 90d)
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
            "Cartoon sticker cutout"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => CreateEffect().Apply(img),
            "Applied Cartoon sticker cutout"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}

