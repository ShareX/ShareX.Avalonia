using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Filters;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class ColorDepthDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        public ColorDepthDialog()
        {
            AvaloniaXamlLoader.Load(this);
            AttachedToVisualTree += (s, e) => RequestPreview();
        }

        private void OnValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!IsLoaded) return;
            RequestPreview();
        }

        private int GetBits()
        {
            var slider = this.FindControl<Slider>("BitsSlider");
            return (int)(slider?.Value ?? 4);
        }

        private void RequestPreview()
        {
            int bits = GetBits();
            PreviewRequested?.Invoke(this, new EffectEventArgs(
                img => new ColorDepthImageEffect { BitsPerChannel = bits }.Apply(img),
                $"Color depth: {bits} bits"));
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            int bits = GetBits();
            ApplyRequested?.Invoke(this, new EffectEventArgs(
                img => new ColorDepthImageEffect { BitsPerChannel = bits }.Apply(img),
                $"Applied Color depth: {bits} bits"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
