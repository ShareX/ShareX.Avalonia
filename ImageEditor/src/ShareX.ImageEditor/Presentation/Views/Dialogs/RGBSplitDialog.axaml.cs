using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Filters;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class RGBSplitDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        public RGBSplitDialog()
        {
            AvaloniaXamlLoader.Load(this);
            AttachedToVisualTree += (s, e) => RequestPreview();
        }

        private void OnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            if (!IsLoaded) return;
            RequestPreview();
        }

        private int GetInt(string name, int fallback)
        {
            NumericUpDown? control = this.FindControl<NumericUpDown>(name);
            return (int)Math.Round(control?.Value ?? fallback);
        }

        private RGBSplitImageEffect CreateEffect()
        {
            return new RGBSplitImageEffect
            {
                OffsetRedX = GetInt("RedXInput", -5),
                OffsetRedY = GetInt("RedYInput", 0),
                OffsetGreenX = GetInt("GreenXInput", 0),
                OffsetGreenY = GetInt("GreenYInput", 0),
                OffsetBlueX = GetInt("BlueXInput", 5),
                OffsetBlueY = GetInt("BlueYInput", 0)
            };
        }

        private void RequestPreview()
        {
            PreviewRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "RGB split"));
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            ApplyRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Applied RGB split"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
