using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Manipulations;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class Rotate3DBoxDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        public Rotate3DBoxDialog()
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

        private void RequestPreview()
        {
            float depth = (float)(this.FindControl<Slider>("DepthSlider")?.Value ?? 0);
            float rotateX = (float)(this.FindControl<Slider>("XAxisSlider")?.Value ?? 0);
            float rotateY = (float)(this.FindControl<Slider>("YAxisSlider")?.Value ?? 0);
            float rotateZ = (float)(this.FindControl<Slider>("ZAxisSlider")?.Value ?? 0);
            bool autoResize = this.FindControl<CheckBox>("AutoResizeCheckBox")?.IsChecked ?? true;
            PreviewRequested?.Invoke(this, new EffectEventArgs(
                img => new Rotate3DBoxImageEffect { Depth = depth, RotateX = rotateX, RotateY = rotateY, RotateZ = rotateZ, AutoResize = autoResize }.Apply(img),
                "3D Box / Extrude"));
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            float depth = (float)(this.FindControl<Slider>("DepthSlider")?.Value ?? 0);
            float rotateX = (float)(this.FindControl<Slider>("XAxisSlider")?.Value ?? 0);
            float rotateY = (float)(this.FindControl<Slider>("YAxisSlider")?.Value ?? 0);
            float rotateZ = (float)(this.FindControl<Slider>("ZAxisSlider")?.Value ?? 0);
            bool autoResize = this.FindControl<CheckBox>("AutoResizeCheckBox")?.IsChecked ?? true;
            ApplyRequested?.Invoke(this, new EffectEventArgs(
                img => new Rotate3DBoxImageEffect { Depth = depth, RotateX = rotateX, RotateY = rotateY, RotateZ = rotateZ, AutoResize = autoResize }.Apply(img),
                "Applied 3D Box / Extrude"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
