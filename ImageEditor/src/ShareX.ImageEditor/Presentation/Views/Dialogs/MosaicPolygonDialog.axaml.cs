using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Filters;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class MosaicPolygonDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler? CancelRequested;

    public MosaicPolygonDialog()
    {
        AvaloniaXamlLoader.Load(this);
        AttachedToVisualTree += (s, e) => RequestPreview();
    }

    private float GetValue(string controlName, double fallback)
    {
        return (float)(this.FindControl<Slider>(controlName)?.Value ?? fallback);
    }

    private MosaicPolygonShape GetShape()
    {
        int index = this.FindControl<ComboBox>("ShapeComboBox")?.SelectedIndex ?? 0;
        return index == 1 ? MosaicPolygonShape.Triangle : MosaicPolygonShape.Hexagon;
    }

    private MosaicPolygonImageEffect CreateEffect()
    {
        return new MosaicPolygonImageEffect
        {
            Shape = GetShape(),
            CellSize = (int)Math.Round(GetValue("CellSizeSlider", 24d)),
            BorderWidth = GetValue("BorderWidthSlider", 1d),
            BorderOpacity = GetValue("BorderOpacitySlider", 45d),
            Randomness = GetValue("RandomnessSlider", 18d)
        };
    }

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void OnShapeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RequestPreview();
    }

    private void RequestPreview()
    {
        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => CreateEffect().Apply(img),
            "Mosaic polygon"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => CreateEffect().Apply(img),
            "Applied Mosaic polygon"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
