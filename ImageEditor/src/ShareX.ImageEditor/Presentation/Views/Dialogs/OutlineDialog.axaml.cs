using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class OutlineDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler? CancelRequested;

    public static readonly StyledProperty<IBrush> OutlineColorBrushProperty =
        AvaloniaProperty.Register<OutlineDialog, IBrush>(nameof(OutlineColorBrush), Brushes.Black);

    public static readonly StyledProperty<Color> OutlineColorValueProperty =
        AvaloniaProperty.Register<OutlineDialog, Color>(nameof(OutlineColorValue), Colors.Black);

    public static readonly StyledProperty<string> OutlineColorTextProperty =
        AvaloniaProperty.Register<OutlineDialog, string>(nameof(OutlineColorText), "#FF000000");

    public IBrush OutlineColorBrush
    {
        get => GetValue(OutlineColorBrushProperty);
        set => SetValue(OutlineColorBrushProperty, value);
    }

    public Color OutlineColorValue
    {
        get => GetValue(OutlineColorValueProperty);
        set => SetValue(OutlineColorValueProperty, value);
    }

    public string OutlineColorText
    {
        get => GetValue(OutlineColorTextProperty);
        set => SetValue(OutlineColorTextProperty, value);
    }

    private SKColor _color = SKColors.Black;
    private bool _isLoaded = false;
    private bool _outlineOnly = false;

    // Control references
    private Slider? _sizeSlider;
    private Slider? _paddingSlider;

    static OutlineDialog()
    {
        OutlineColorValueProperty.Changed.AddClassHandler<OutlineDialog>((s, e) =>
        {
            s.OnOutlineColorValueChanged();
        });
    }

    public OutlineDialog()
    {
        InitializeComponent();

        _sizeSlider = this.FindControl<Slider>("SizeSlider");
        _paddingSlider = this.FindControl<Slider>("PaddingSlider");

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

    private int GetSize() => (int)(_sizeSlider?.Value ?? 3);
    private int GetPadding() => (int)(_paddingSlider?.Value ?? 0);

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isLoaded) RaisePreview();
    }

    private void OnOutlineOnlyChanged(object? sender, RoutedEventArgs e)
    {
        _outlineOnly = this.FindControl<CheckBox>("OutlineOnlyCheckBox")?.IsChecked == true;
        if (_isLoaded) RaisePreview();
    }

    private void OnOutlineColorValueChanged()
    {
        var color = OutlineColorValue;
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
        OutlineColorBrush = new SolidColorBrush(
            Color.FromArgb(_color.Alpha, _color.Red, _color.Green, _color.Blue));
    }

    private void UpdateColorText()
    {
        OutlineColorText = _color.Alpha == 0
            ? "Transparent"
            : $"#{_color.Alpha:X2}{_color.Red:X2}{_color.Green:X2}{_color.Blue:X2}";
    }

    private void RaisePreview()
    {
        var size = GetSize();
        var padding = GetPadding();
        var outlineOnly = _outlineOnly;

        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => ImageHelpers.ApplyOutline(img, size, padding, outlineOnly, _color),
            "Outline applied"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        var size = GetSize();
        var padding = GetPadding();
        var outlineOnly = _outlineOnly;

        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => ImageHelpers.ApplyOutline(img, size, padding, outlineOnly, _color),
            "Outline applied"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
