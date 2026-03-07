using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class GlowDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler? CancelRequested;

    public static readonly StyledProperty<IBrush> GlowColorBrushProperty =
        AvaloniaProperty.Register<GlowDialog, IBrush>(nameof(GlowColorBrush), Brushes.White);

    public static readonly StyledProperty<Color> GlowColorValueProperty =
        AvaloniaProperty.Register<GlowDialog, Color>(nameof(GlowColorValue), Colors.White);

    public static readonly StyledProperty<string> GlowColorTextProperty =
        AvaloniaProperty.Register<GlowDialog, string>(nameof(GlowColorText), "#FFFFFFFF");

    public IBrush GlowColorBrush
    {
        get => GetValue(GlowColorBrushProperty);
        set => SetValue(GlowColorBrushProperty, value);
    }

    public Color GlowColorValue
    {
        get => GetValue(GlowColorValueProperty);
        set => SetValue(GlowColorValueProperty, value);
    }

    public string GlowColorText
    {
        get => GetValue(GlowColorTextProperty);
        set => SetValue(GlowColorTextProperty, value);
    }

    private SKColor _color = SKColors.White;
    private bool _isLoaded = false;

    // Control references
    private Slider? _sizeSlider;
    private Slider? _strengthSlider;
    private Slider? _offsetXSlider;
    private Slider? _offsetYSlider;
    private CheckBox? _autoResizeCheckBox;

    static GlowDialog()
    {
        GlowColorValueProperty.Changed.AddClassHandler<GlowDialog>((s, e) =>
        {
            s.OnGlowColorValueChanged();
        });
    }

    public GlowDialog()
    {
        InitializeComponent();

        _sizeSlider = this.FindControl<Slider>("SizeSlider");
        _strengthSlider = this.FindControl<Slider>("StrengthSlider");
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

    private int GetSize() => (int)(_sizeSlider?.Value ?? 20);
    private float GetStrength() => (float)(_strengthSlider?.Value ?? 80);
    private int GetOffsetX() => (int)(_offsetXSlider?.Value ?? 0);
    private int GetOffsetY() => (int)(_offsetYSlider?.Value ?? 0);
    private bool GetAutoResize() => _autoResizeCheckBox?.IsChecked ?? true;

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isLoaded) RaisePreview();
    }

    private void OnCheckChanged(object? sender, RoutedEventArgs e)
    {
        if (_isLoaded) RaisePreview();
    }

    private void OnGlowColorValueChanged()
    {
        var color = GlowColorValue;
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
        GlowColorBrush = new SolidColorBrush(
            Color.FromArgb(_color.Alpha, _color.Red, _color.Green, _color.Blue));
    }

    private void UpdateColorText()
    {
        GlowColorText = _color.Alpha == 0
            ? "Transparent"
            : $"#{_color.Alpha:X2}{_color.Red:X2}{_color.Green:X2}{_color.Blue:X2}";
    }

    private void RaisePreview()
    {
        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => ImageHelpers.ApplyGlow(img, GetSize(), GetStrength(), _color, GetOffsetX(), GetOffsetY(), GetAutoResize()),
            "Glow applied"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => ImageHelpers.ApplyGlow(img, GetSize(), GetStrength(), _color, GetOffsetX(), GetOffsetY(), GetAutoResize()),
            "Glow applied"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
