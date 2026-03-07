using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Manipulations;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class SkewDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        public SkewDialog()
        {
            AvaloniaXamlLoader.Load(this);
            this.AttachedToVisualTree += (s, e) => RequestPreview();
        }

        private void OnValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            RequestPreview();
        }

        private void OnAutoResizeChanged(object? sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded) return;
            RequestPreview();
        }

        private bool GetAutoResize() => this.FindControl<CheckBox>("AutoResizeCheckBox")?.IsChecked ?? true;

        private void RequestPreview()
        {
            int horizontal = (int)(this.FindControl<Slider>("HorizontalSlider")?.Value ?? 0);
            int vertical = (int)(this.FindControl<Slider>("VerticalSlider")?.Value ?? 0);
            bool autoResize = GetAutoResize();
            PreviewRequested?.Invoke(this, new EffectEventArgs(
                img => new SkewImageEffect { Horizontally = horizontal, Vertically = vertical, AutoResize = autoResize }.Apply(img),
                "Skew"));
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            int horizontal = (int)(this.FindControl<Slider>("HorizontalSlider")?.Value ?? 0);
            int vertical = (int)(this.FindControl<Slider>("VerticalSlider")?.Value ?? 0);
            bool autoResize = GetAutoResize();
            ApplyRequested?.Invoke(this, new EffectEventArgs(
                img => new SkewImageEffect { Horizontally = horizontal, Vertically = vertical, AutoResize = autoResize }.Apply(img),
                "Applied Skew"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

