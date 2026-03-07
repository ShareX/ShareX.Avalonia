using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ShareX.ImageEditor.Core.ImageEffects.Adjustments;
using SkiaSharp;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class ReplaceColorDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        private bool _suppressPreview = false;

        public static readonly StyledProperty<IBrush> TargetColorBrushProperty =
            AvaloniaProperty.Register<ReplaceColorDialog, IBrush>(nameof(TargetColorBrush), Brushes.White);

        public static readonly StyledProperty<Color> TargetColorValueProperty =
            AvaloniaProperty.Register<ReplaceColorDialog, Color>(nameof(TargetColorValue), Colors.White);

        public static readonly StyledProperty<string> TargetColorTextProperty =
            AvaloniaProperty.Register<ReplaceColorDialog, string>(nameof(TargetColorText), "#FFFFFFFF");

        public static readonly StyledProperty<IBrush> ReplaceColorBrushProperty =
            AvaloniaProperty.Register<ReplaceColorDialog, IBrush>(nameof(ReplaceColorBrush), Brushes.Black);

        public static readonly StyledProperty<Color> ReplaceColorValueProperty =
            AvaloniaProperty.Register<ReplaceColorDialog, Color>(nameof(ReplaceColorValue), Colors.Black);

        public static readonly StyledProperty<string> ReplaceColorTextProperty =
            AvaloniaProperty.Register<ReplaceColorDialog, string>(nameof(ReplaceColorText), "#FF000000");

        public IBrush TargetColorBrush
        {
            get => GetValue(TargetColorBrushProperty);
            set => SetValue(TargetColorBrushProperty, value);
        }

        public Color TargetColorValue
        {
            get => GetValue(TargetColorValueProperty);
            set => SetValue(TargetColorValueProperty, value);
        }

        public string TargetColorText
        {
            get => GetValue(TargetColorTextProperty);
            set => SetValue(TargetColorTextProperty, value);
        }

        public IBrush ReplaceColorBrush
        {
            get => GetValue(ReplaceColorBrushProperty);
            set => SetValue(ReplaceColorBrushProperty, value);
        }

        public Color ReplaceColorValue
        {
            get => GetValue(ReplaceColorValueProperty);
            set => SetValue(ReplaceColorValueProperty, value);
        }

        public string ReplaceColorText
        {
            get => GetValue(ReplaceColorTextProperty);
            set => SetValue(ReplaceColorTextProperty, value);
        }

        private SKColor _targetColor = SKColors.White;
        private SKColor _replaceColor = SKColors.Black;

        static ReplaceColorDialog()
        {
            TargetColorValueProperty.Changed.AddClassHandler<ReplaceColorDialog>((s, e) =>
            {
                s.OnTargetColorValueChanged();
            });

            ReplaceColorValueProperty.Changed.AddClassHandler<ReplaceColorDialog>((s, e) =>
            {
                s.OnReplaceColorValueChanged();
            });
        }

        public ReplaceColorDialog()
        {
            AvaloniaXamlLoader.Load(this);

            UpdateTargetColorBrush();
            UpdateTargetColorText();
            UpdateReplaceColorBrush();
            UpdateReplaceColorText();

            this.Loaded += (s, e) => RequestPreview();
        }

        private void OnValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_suppressPreview) return;
            RequestPreview();
        }

        private void OnTargetColorValueChanged()
        {
            var color = TargetColorValue;
            _targetColor = new SKColor(color.R, color.G, color.B, color.A);
            UpdateTargetColorBrush();
            UpdateTargetColorText();
            if (!_suppressPreview) RequestPreview();
        }

        private void OnReplaceColorValueChanged()
        {
            var color = ReplaceColorValue;
            _replaceColor = new SKColor(color.R, color.G, color.B, color.A);
            UpdateReplaceColorBrush();
            UpdateReplaceColorText();
            if (!_suppressPreview) RequestPreview();
        }

        private void OnTargetColorButtonClick(object? sender, RoutedEventArgs e)
        {
            var popup = this.FindControl<Popup>("TargetColorPopup");
            if (popup != null)
            {
                popup.IsOpen = !popup.IsOpen;
            }
        }

        private void OnReplaceColorButtonClick(object? sender, RoutedEventArgs e)
        {
            var popup = this.FindControl<Popup>("ReplaceColorPopup");
            if (popup != null)
            {
                popup.IsOpen = !popup.IsOpen;
            }
        }

        private void UpdateTargetColorBrush()
        {
            TargetColorBrush = new SolidColorBrush(
                Color.FromArgb(_targetColor.Alpha, _targetColor.Red, _targetColor.Green, _targetColor.Blue));
        }

        private void UpdateTargetColorText()
        {
            if (_targetColor.Alpha == 0)
            {
                TargetColorText = "Transparent";
            }
            else
            {
                TargetColorText = $"#{_targetColor.Alpha:X2}{_targetColor.Red:X2}{_targetColor.Green:X2}{_targetColor.Blue:X2}";
            }
        }

        private void UpdateReplaceColorBrush()
        {
            ReplaceColorBrush = new SolidColorBrush(
                Color.FromArgb(_replaceColor.Alpha, _replaceColor.Red, _replaceColor.Green, _replaceColor.Blue));
        }

        private void UpdateReplaceColorText()
        {
            if (_replaceColor.Alpha == 0)
            {
                ReplaceColorText = "Transparent";
            }
            else
            {
                ReplaceColorText = $"#{_replaceColor.Alpha:X2}{_replaceColor.Red:X2}{_replaceColor.Green:X2}{_replaceColor.Blue:X2}";
            }
        }

        private void RequestPreview()
        {
            float tolerance = (float)(this.FindControl<Slider>("ToleranceSlider")?.Value ?? 0);

            PreviewRequested?.Invoke(this, new EffectEventArgs(img => new ReplaceColorImageEffect { TargetColor = _targetColor, ReplaceColor = _replaceColor, Tolerance = tolerance }.Apply(img), $"Replace Color"));
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            float tolerance = (float)(this.FindControl<Slider>("ToleranceSlider")?.Value ?? 0);

            ApplyRequested?.Invoke(this, new EffectEventArgs(img => new ReplaceColorImageEffect { TargetColor = _targetColor, ReplaceColor = _replaceColor, Tolerance = tolerance }.Apply(img), "Applied Replace Color"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

