using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class ShadowDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler? CancelRequested;

    public static readonly StyledProperty<IBrush> ShadowColorBrushProperty =
        AvaloniaProperty.Register<ShadowDialog, IBrush>(nameof(ShadowColorBrush), Brushes.Black);

    public static readonly StyledProperty<Color> ShadowColorValueProperty =
        AvaloniaProperty.Register<ShadowDialog, Color>(nameof(ShadowColorValue), Colors.Black);

    public static readonly StyledProperty<string> ShadowColorTextProperty =
        AvaloniaProperty.Register<ShadowDialog, string>(nameof(ShadowColorText), "#FF000000");

    public IBrush ShadowColorBrush
    {
        get => GetValue(ShadowColorBrushProperty);
        set => SetValue(ShadowColorBrushProperty, value);
    }

    public Color ShadowColorValue
    {
        get => GetValue(ShadowColorValueProperty);
        set => SetValue(ShadowColorValueProperty, value);
    }

    public string ShadowColorText
    {
        get => GetValue(ShadowColorTextProperty);
        set => SetValue(ShadowColorTextProperty, value);
    }

    private SKColor _color = SKColors.Black;
    private bool _isLoaded = false;

    // Control references
    private Slider? _opacitySlider;
    private Slider? _sizeSlider;
    private Slider? _offsetXSlider;
    private Slider? _offsetYSlider;
    private CheckBox? _autoResizeCheckBox;

    static ShadowDialog()
    {
        ShadowColorValueProperty.Changed.AddClassHandler<ShadowDialog>((s, e) =>
        {
            s.OnShadowColorValueChanged();
        });
    }

    public ShadowDialog()
    {
        InitializeComponent();

        _opacitySlider = this.FindControl<Slider>("OpacitySlider");
        _sizeSlider = this.FindControl<Slider>("SizeSlider");
        _offsetXSlider = this.FindControl<Slider>("OffsetXSlider");
        _offsetYSlider = this.FindControl<Slider>("OffsetYSlider");
        _autoResizeCheckBox = this.FindControl<CheckBox>("AutoResizeCheckBox");

        UpdateColorBrush();
        UpdateColorText();

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

    private float GetOpacity() => (float)(_opacitySlider?.Value ?? 80);
    private int GetSize() => (int)(_sizeSlider?.Value ?? 20);
    private int GetOffsetX() => (int)(_offsetXSlider?.Value ?? 5);
    private int GetOffsetY() => (int)(_offsetYSlider?.Value ?? 5);
    private bool GetAutoResize() => _autoResizeCheckBox?.IsChecked ?? true;

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isLoaded) RaisePreview();
    }

    private void OnCheckChanged(object? sender, RoutedEventArgs e)
    {
        if (_isLoaded) RaisePreview();
    }

    private void OnShadowColorValueChanged()
    {
        var color = ShadowColorValue;
        _color = new SKColor(color.R, color.G, color.B, color.A);
        UpdateColorBrush();
        UpdateColorText();
        if (_isLoaded) RaisePreview();
    }

    private void OnColorButtonClick(object? sender, RoutedEventArgs e)
    {
        var popup = this.FindControl<Popup>("ColorPopup");
        if (popup != null) popup.IsOpen = !popup.IsOpen;
    }

    private void UpdateColorBrush()
    {
        ShadowColorBrush = new SolidColorBrush(
            Color.FromArgb(_color.Alpha, _color.Red, _color.Green, _color.Blue));
    }

    private void UpdateColorText()
    {
        ShadowColorText = _color.Alpha == 0
            ? "Transparent"
            : $"#{_color.Alpha:X2}{_color.Red:X2}{_color.Green:X2}{_color.Blue:X2}";
    }

    private void RaisePreview()
    {
        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => ImageHelpers.ApplyShadow(img, GetOpacity(), GetSize(), _color, GetOffsetX(), GetOffsetY(), GetAutoResize()),
            "Shadow applied"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => ImageHelpers.ApplyShadow(img, GetOpacity(), GetSize(), _color, GetOffsetX(), GetOffsetY(), GetAutoResize()),
            "Shadow applied"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
