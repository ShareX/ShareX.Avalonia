using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Manipulations;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class Rotate3DDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        public Rotate3DDialog()
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
            float rotateX = (float)(this.FindControl<Slider>("XAxisSlider")?.Value ?? 0);
            float rotateY = (float)(this.FindControl<Slider>("YAxisSlider")?.Value ?? 0);
            float rotateZ = (float)(this.FindControl<Slider>("ZAxisSlider")?.Value ?? 0);
            bool autoResize = GetAutoResize();
            PreviewRequested?.Invoke(this, new EffectEventArgs(
                img => new Rotate3DImageEffect { RotateX = rotateX, RotateY = rotateY, RotateZ = rotateZ, AutoResize = autoResize }.Apply(img),
                "Rotate 3D"));
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            float rotateX = (float)(this.FindControl<Slider>("XAxisSlider")?.Value ?? 0);
            float rotateY = (float)(this.FindControl<Slider>("YAxisSlider")?.Value ?? 0);
            float rotateZ = (float)(this.FindControl<Slider>("ZAxisSlider")?.Value ?? 0);
            bool autoResize = GetAutoResize();
            ApplyRequested?.Invoke(this, new EffectEventArgs(
                img => new Rotate3DImageEffect { RotateX = rotateX, RotateY = rotateY, RotateZ = rotateZ, AutoResize = autoResize }.Apply(img),
                "Applied Rotate 3D"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
