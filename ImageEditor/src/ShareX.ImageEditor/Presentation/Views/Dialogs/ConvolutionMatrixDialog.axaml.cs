using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Filters;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class ConvolutionMatrixDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        public ConvolutionMatrixDialog()
        {
            AvaloniaXamlLoader.Load(this);
            AttachedToVisualTree += (s, e) => RequestPreview();
        }

        private void OnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            if (!IsLoaded) return;
            RequestPreview();
        }

        private int GetInt(string name, int fallback = 0)
        {
            NumericUpDown? control = this.FindControl<NumericUpDown>(name);
            return (int)Math.Round((double)(control?.Value ?? (decimal)fallback));
        }

        private double GetDouble(string name, double fallback = 1d)
        {
            NumericUpDown? control = this.FindControl<NumericUpDown>(name);
            return (double)(control?.Value ?? (decimal)fallback);
        }

        private ConvolutionMatrixImageEffect CreateEffect()
        {
            return new ConvolutionMatrixImageEffect
            {
                X0Y0 = GetInt("X0Y0Input"),
                X1Y0 = GetInt("X1Y0Input"),
                X2Y0 = GetInt("X2Y0Input"),
                X0Y1 = GetInt("X0Y1Input"),
                X1Y1 = GetInt("X1Y1Input", 1),
                X2Y1 = GetInt("X2Y1Input"),
                X0Y2 = GetInt("X0Y2Input"),
                X1Y2 = GetInt("X1Y2Input"),
                X2Y2 = GetInt("X2Y2Input"),
                Factor = GetDouble("FactorInput", 1d),
                Offset = GetInt("OffsetInput")
            };
        }

        private void RequestPreview()
        {
            PreviewRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Convolution matrix"));
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            ApplyRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Applied Convolution matrix"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
