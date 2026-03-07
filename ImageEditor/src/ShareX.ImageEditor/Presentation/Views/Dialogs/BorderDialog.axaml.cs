using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class BorderDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler? CancelRequested;

    public static readonly StyledProperty<IBrush> BorderColorBrushProperty =
        AvaloniaProperty.Register<BorderDialog, IBrush>(nameof(BorderColorBrush), Brushes.Black);

    public static readonly StyledProperty<Color> BorderColorValueProperty =
        AvaloniaProperty.Register<BorderDialog, Color>(nameof(BorderColorValue), Colors.Black);

    public static readonly StyledProperty<string> BorderColorTextProperty =
        AvaloniaProperty.Register<BorderDialog, string>(nameof(BorderColorText), "#FF000000");

    public IBrush BorderColorBrush
    {
        get => GetValue(BorderColorBrushProperty);
        set => SetValue(BorderColorBrushProperty, value);
    }

    public Color BorderColorValue
    {
        get => GetValue(BorderColorValueProperty);
        set => SetValue(BorderColorValueProperty, value);
    }

    public string BorderColorText
    {
        get => GetValue(BorderColorTextProperty);
        set => SetValue(BorderColorTextProperty, value);
    }

    private SKColor _color = SKColors.Black;
    private bool _isLoaded = false;

    // Control references
    private ComboBox? _typeComboBox;
    private ComboBox? _dashStyleComboBox;
    private Slider? _sizeSlider;

    static BorderDialog()
    {
        BorderColorValueProperty.Changed.AddClassHandler<BorderDialog>((s, e) =>
        {
            s.OnBorderColorValueChanged();
        });
    }

    public BorderDialog()
    {
        InitializeComponent();

        // Find controls after XAML is loaded
        _typeComboBox = this.FindControl<ComboBox>("TypeComboBox");
        _dashStyleComboBox = this.FindControl<ComboBox>("DashStyleComboBox");
        _sizeSlider = this.FindControl<Slider>("SizeSlider");

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

    private ImageHelpers.BorderType GetBorderType()
    {
        return _typeComboBox?.SelectedIndex == 1 ? ImageHelpers.BorderType.Inside : ImageHelpers.BorderType.Outside;
    }

    private ImageHelpers.DashStyle GetDashStyle()
    {
        return _dashStyleComboBox?.SelectedIndex switch
        {
            1 => ImageHelpers.DashStyle.Dash,
            2 => ImageHelpers.DashStyle.Dot,
            3 => ImageHelpers.DashStyle.DashDot,
            _ => ImageHelpers.DashStyle.Solid
        };
    }

    private int GetSize() => (int)(_sizeSlider?.Value ?? 5);

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isLoaded) RaisePreview();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isLoaded) RaisePreview();
    }

    private void OnBorderColorValueChanged()
    {
        var color = BorderColorValue;
        _color = new SKColor(color.R, color.G, color.B, color.A);
        UpdateColorBrush();
        UpdateColorText();
        if (_isLoaded) RaisePreview();
    }

    private void OnColorButtonClick(object? sender, RoutedEventArgs e)
    {
        var popup = this.FindControl<Popup>("ColorPopup");
        if (popup != null)
        {
            popup.IsOpen = !popup.IsOpen;
        }
    }

    private void UpdateColorBrush()
    {
        BorderColorBrush = new SolidColorBrush(
            Color.FromArgb(_color.Alpha, _color.Red, _color.Green, _color.Blue));
    }

    private void UpdateColorText()
    {
        if (_color.Alpha == 0)
        {
            BorderColorText = "Transparent";
        }
        else
        {
            BorderColorText = $"#{_color.Alpha:X2}{_color.Red:X2}{_color.Green:X2}{_color.Blue:X2}";
        }
    }

    private void RaisePreview()
    {
        var type = GetBorderType();
        var size = GetSize();
        var dashStyle = GetDashStyle();

        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => ImageHelpers.ApplyBorder(img, type, size, dashStyle, _color),
            "Border applied"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        var type = GetBorderType();
        var size = GetSize();
        var dashStyle = GetDashStyle();

        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => ImageHelpers.ApplyBorder(img, type, size, dashStyle, _color),
            "Border applied"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
