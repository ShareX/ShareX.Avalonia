using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ShareX.ImageEditor.Core.ImageEffects.Helpers;
using SkiaSharp;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class NeonEdgeGlowDialog : UserControl, IEffectDialog
{
    public event EventHandler<EffectEventArgs>? PreviewRequested;
    public event EventHandler<EffectEventArgs>? ApplyRequested;
    public event EventHandler? CancelRequested;

    public static readonly StyledProperty<IBrush> NeonColorBrushProperty =
        AvaloniaProperty.Register<NeonEdgeGlowDialog, IBrush>(nameof(NeonColorBrush), Brushes.Cyan);

    public static readonly StyledProperty<Color> NeonColorValueProperty =
        AvaloniaProperty.Register<NeonEdgeGlowDialog, Color>(nameof(NeonColorValue), Color.FromArgb(255, 0, 240, 255));

    public static readonly StyledProperty<string> NeonColorTextProperty =
        AvaloniaProperty.Register<NeonEdgeGlowDialog, string>(nameof(NeonColorText), "#FF00F0FF");

    public IBrush NeonColorBrush
    {
        get => GetValue(NeonColorBrushProperty);
        set => SetValue(NeonColorBrushProperty, value);
    }

    public Color NeonColorValue
    {
        get => GetValue(NeonColorValueProperty);
        set => SetValue(NeonColorValueProperty, value);
    }

    public string NeonColorText
    {
        get => GetValue(NeonColorTextProperty);
        set => SetValue(NeonColorTextProperty, value);
    }

    private SKColor _neonColor = new SKColor(0, 240, 255, 255);
    private bool _isLoaded;

    static NeonEdgeGlowDialog()
    {
        NeonColorValueProperty.Changed.AddClassHandler<NeonEdgeGlowDialog>((s, e) =>
        {
            s.OnNeonColorValueChanged();
        });
    }

    public NeonEdgeGlowDialog()
    {
        AvaloniaXamlLoader.Load(this);

        UpdateColorBrush();
        UpdateColorText();

        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _isLoaded = true;
        RaisePreview();
    }

    private float GetFloat(string controlName, double fallback)
    {
        return (float)(this.FindControl<Slider>(controlName)?.Value ?? fallback);
    }

    private int GetInt(string controlName, double fallback)
    {
        return (int)Math.Round(this.FindControl<Slider>(controlName)?.Value ?? fallback);
    }

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isLoaded) RaisePreview();
    }

    private void OnNeonColorValueChanged()
    {
        Color color = NeonColorValue;
        _neonColor = new SKColor(color.R, color.G, color.B, color.A);
        UpdateColorBrush();
        UpdateColorText();
        if (_isLoaded) RaisePreview();
    }

    private void UpdateColorBrush()
    {
        NeonColorBrush = new SolidColorBrush(
            Color.FromArgb(_neonColor.Alpha, _neonColor.Red, _neonColor.Green, _neonColor.Blue));
    }

    private void UpdateColorText()
    {
        NeonColorText = _neonColor.Alpha == 0
            ? "Transparent"
            : $"#{_neonColor.Alpha:X2}{_neonColor.Red:X2}{_neonColor.Green:X2}{_neonColor.Blue:X2}";
    }

    private void OnColorButtonClick(object? sender, RoutedEventArgs e)
    {
        Popup? popup = this.FindControl<Popup>("ColorPopup");
        if (popup != null)
        {
            popup.IsOpen = !popup.IsOpen;
        }
    }

    private void RaisePreview()
    {
        float edgeStrength = GetFloat("StrengthSlider", 2.2d);
        int threshold = GetInt("ThresholdSlider", 36d);
        float glowRadius = GetFloat("RadiusSlider", 8d);
        float glowIntensity = GetFloat("IntensitySlider", 120d);
        float baseDim = GetFloat("BaseDimSlider", 30d);
        SKColor neonColor = _neonColor;

        PreviewRequested?.Invoke(this, new EffectEventArgs(
            img => ImageHelpers.ApplyNeonEdgeGlow(img, edgeStrength, threshold, glowRadius, glowIntensity, baseDim, neonColor),
            "Neon edge glow"));
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        float edgeStrength = GetFloat("StrengthSlider", 2.2d);
        int threshold = GetInt("ThresholdSlider", 36d);
        float glowRadius = GetFloat("RadiusSlider", 8d);
        float glowIntensity = GetFloat("IntensitySlider", 120d);
        float baseDim = GetFloat("BaseDimSlider", 30d);
        SKColor neonColor = _neonColor;

        ApplyRequested?.Invoke(this, new EffectEventArgs(
            img => ImageHelpers.ApplyNeonEdgeGlow(img, edgeStrength, threshold, glowRadius, glowIntensity, baseDim, neonColor),
            "Applied Neon edge glow"));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
