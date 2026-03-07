using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Helpers;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class SliceDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler? CancelRequested;

    private bool _isLoaded = false;

    // Control references
    private Slider? _minHeightSlider;
    private Slider? _maxHeightSlider;
    private Slider? _minShiftSlider;
    private Slider? _maxShiftSlider;

    public SliceDialog()
    {
        InitializeComponent();

        // Find controls after XAML is loaded
        _minHeightSlider = this.FindControl<Slider>("MinHeightSlider");
        _maxHeightSlider = this.FindControl<Slider>("MaxHeightSlider");
        _minShiftSlider = this.FindControl<Slider>("MinShiftSlider");
        _maxShiftSlider = this.FindControl<Slider>("MaxShiftSlider");

        Loaded += OnLoaded;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _isLoaded = true;
        RaisePreview();
    }

    private int GetMinHeight() => (int)(_minHeightSlider?.Value ?? 5);
    private int GetMaxHeight() => (int)(_maxHeightSlider?.Value ?? 20);
    private int GetMinShift() => (int)(_minShiftSlider?.Value ?? -10);
    private int GetMaxShift() => (int)(_maxShiftSlider?.Value ?? 10);

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isLoaded) RaisePreview();
    }

    private void RaisePreview()
    {
        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => ImageHelpers.ApplySlice(img, GetMinHeight(), GetMaxHeight(), GetMinShift(), GetMaxShift()),
            "Slice applied"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => ImageHelpers.ApplySlice(img, GetMinHeight(), GetMaxHeight(), GetMinShift(), GetMaxShift()),
            "Slice applied"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
