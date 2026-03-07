using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Helpers;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class ReflectionDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler? CancelRequested;

    private bool _isLoaded = false;

    // Control references
    private Slider? _percentageSlider;
    private Slider? _maxAlphaSlider;
    private Slider? _minAlphaSlider;
    private Slider? _offsetSlider;
    private CheckBox? _skewCheckBox;
    private Slider? _skewSizeSlider;

    public ReflectionDialog()
    {
        InitializeComponent();

        // Find controls after XAML is loaded
        _percentageSlider = this.FindControl<Slider>("PercentageSlider");
        _maxAlphaSlider = this.FindControl<Slider>("MaxAlphaSlider");
        _minAlphaSlider = this.FindControl<Slider>("MinAlphaSlider");
        _offsetSlider = this.FindControl<Slider>("OffsetSlider");
        _skewCheckBox = this.FindControl<CheckBox>("SkewCheckBox");
        _skewSizeSlider = this.FindControl<Slider>("SkewSizeSlider");

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

    private int GetPercentage() => (int)(_percentageSlider?.Value ?? 50);
    private int GetMaxAlpha() => (int)(_maxAlphaSlider?.Value ?? 200);
    private int GetMinAlpha() => (int)(_minAlphaSlider?.Value ?? 0);
    private int GetOffset() => (int)(_offsetSlider?.Value ?? 0);
    private bool GetSkew() => _skewCheckBox?.IsChecked ?? false;
    private int GetSkewSize() => (int)(_skewSizeSlider?.Value ?? 25);

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isLoaded) RaisePreview();
    }

    private void OnCheckChanged(object? sender, RoutedEventArgs e)
    {
        if (_isLoaded) RaisePreview();
    }

    private void RaisePreview()
    {
        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => ImageHelpers.ApplyReflection(img, GetPercentage(), GetMaxAlpha(), GetMinAlpha(), GetOffset(), GetSkew(), GetSkewSize()),
            "Reflection applied"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => ImageHelpers.ApplyReflection(img, GetPercentage(), GetMaxAlpha(), GetMinAlpha(), GetOffset(), GetSkew(), GetSkewSize()),
            "Reflection applied"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
