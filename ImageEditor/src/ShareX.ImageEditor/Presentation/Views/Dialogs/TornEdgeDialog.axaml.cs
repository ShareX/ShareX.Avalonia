using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Helpers;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class TornEdgeDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler? CancelRequested;

    private bool _isLoaded = false;

    // Control references
    private Slider? _depthSlider;
    private Slider? _rangeSlider;
    private CheckBox? _topCheckBox;
    private CheckBox? _rightCheckBox;
    private CheckBox? _bottomCheckBox;
    private CheckBox? _leftCheckBox;
    private CheckBox? _curvedCheckBox;

    public TornEdgeDialog()
    {
        InitializeComponent();

        // Find controls after XAML is loaded
        _depthSlider = this.FindControl<Slider>("DepthSlider");
        _rangeSlider = this.FindControl<Slider>("RangeSlider");
        _topCheckBox = this.FindControl<CheckBox>("TopCheckBox");
        _rightCheckBox = this.FindControl<CheckBox>("RightCheckBox");
        _bottomCheckBox = this.FindControl<CheckBox>("BottomCheckBox");
        _leftCheckBox = this.FindControl<CheckBox>("LeftCheckBox");
        _curvedCheckBox = this.FindControl<CheckBox>("CurvedCheckBox");

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

    private int GetDepth() => (int)(_depthSlider?.Value ?? 20);
    private int GetRange() => (int)(_rangeSlider?.Value ?? 20);
    private bool GetTop() => _topCheckBox?.IsChecked ?? true;
    private bool GetRight() => _rightCheckBox?.IsChecked ?? false;
    private bool GetBottom() => _bottomCheckBox?.IsChecked ?? true;
    private bool GetLeft() => _leftCheckBox?.IsChecked ?? false;
    private bool GetCurved() => _curvedCheckBox?.IsChecked ?? false;

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
            img => ImageHelpers.ApplyTornEdge(img, GetDepth(), GetRange(), GetTop(), GetRight(), GetBottom(), GetLeft(), GetCurved()),
            "Torn edge applied"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => ImageHelpers.ApplyTornEdge(img, GetDepth(), GetRange(), GetTop(), GetRight(), GetBottom(), GetLeft(), GetCurved()),
            "Torn edge applied"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
