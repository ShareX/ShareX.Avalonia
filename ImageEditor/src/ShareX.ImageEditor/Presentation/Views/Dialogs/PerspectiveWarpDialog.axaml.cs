using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Manipulations;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class PerspectiveWarpDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        public PerspectiveWarpDialog()
        {
            AvaloniaXamlLoader.Load(this);
            AttachedToVisualTree += (s, e) => RequestPreview();
        }

        private float GetFloat(string name)
        {
            NumericUpDown? input = this.FindControl<NumericUpDown>(name);
            return (float)(input?.Value ?? 0m);
        }

        private PerspectiveWarpImageEffect CreateEffect()
        {
            return new PerspectiveWarpImageEffect
            {
                TopLeftX = GetFloat("TopLeftXInput"),
                TopLeftY = GetFloat("TopLeftYInput"),
                TopRightX = GetFloat("TopRightXInput"),
                TopRightY = GetFloat("TopRightYInput"),
                BottomRightX = GetFloat("BottomRightXInput"),
                BottomRightY = GetFloat("BottomRightYInput"),
                BottomLeftX = GetFloat("BottomLeftXInput"),
                BottomLeftY = GetFloat("BottomLeftYInput")
            };
        }

        private void OnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            if (!IsLoaded) return;
            RequestPreview();
        }

        private void RequestPreview()
        {
            PreviewRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Perspective warp"));
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            ApplyRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Applied Perspective warp"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
