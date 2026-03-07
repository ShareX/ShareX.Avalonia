using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Adjustments;
using SkiaSharp;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class BrightnessDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        private bool _suppressPreview = false;

        public BrightnessDialog()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_suppressPreview) return;

            // Debounce logic could be added here if needed, but for simple brightness it should be fast enough.
            // Or rely on MainViewModel to handle update speed.

            // We pass the function to apply, or the result?
            // Passing the result means we do the processing here or in a helper.
            // Better to decouple: Notify that we want a preview with these parameters.
            // But actually, MainViewModel expects a Bitmap for preview.
            // So we should compute it here? Or MainViewModel computes it?
            // Implementation Plan said: "On PreviewRequested, apply effect to a copy..."
            // So MainViewModel does the heavy lifting? 
            // BUT MainViewModel doesn't know WHICH effect is running unless we tell it.
            // So let's pass a Func<SKBitmap, SKBitmap> in the event args.

            RequestPreview();
        }

        private void RequestPreview()
        {
            var slider = this.FindControl<Slider>("AmountSlider");
            float amount = (float)(slider?.Value ?? 0);

            PreviewRequested?.Invoke(this, new EffectEventArgs(img => new BrightnessImageEffect { Amount = amount }.Apply(img), $"Brightness: {amount:0}"));
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            var slider = this.FindControl<Slider>("AmountSlider");
            float amount = (float)(slider?.Value ?? 0);

            ApplyRequested?.Invoke(this, new EffectEventArgs(img => new BrightnessImageEffect { Amount = amount }.Apply(img), $"Adjusted brightness by {amount:0}"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public class EffectEventArgs : EventArgs
    {
        public Func<SKBitmap, SKBitmap> EffectOperation { get; }
        public string StatusMessage { get; }

        public EffectEventArgs(Func<SKBitmap, SKBitmap> operation, string statusMessage)
        {
            EffectOperation = operation;
            StatusMessage = statusMessage;
        }
    }
}

