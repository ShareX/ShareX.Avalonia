using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Adjustments;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class ColorMatrixDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        public ColorMatrixDialog()
        {
            AvaloniaXamlLoader.Load(this);
            AttachedToVisualTree += (s, e) => RequestPreview();
        }

        private void OnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            if (!IsLoaded) return;
            RequestPreview();
        }

        private float GetFloat(string name, float fallback = 0f)
        {
            NumericUpDown? control = this.FindControl<NumericUpDown>(name);
            return (float)(control?.Value ?? (decimal)fallback);
        }

        private ColorMatrixImageEffect CreateEffect()
        {
            return new ColorMatrixImageEffect
            {
                Rr = GetFloat("RrInput", 1f),
                Rg = GetFloat("RgInput"),
                Rb = GetFloat("RbInput"),
                Ra = GetFloat("RaInput"),
                Ro = GetFloat("RoInput"),

                Gr = GetFloat("GrInput"),
                Gg = GetFloat("GgInput", 1f),
                Gb = GetFloat("GbInput"),
                Ga = GetFloat("GaInput"),
                Go = GetFloat("GoInput"),

                Br = GetFloat("BrInput"),
                Bg = GetFloat("BgInput"),
                Bb = GetFloat("BbInput", 1f),
                Ba = GetFloat("BaInput"),
                Bo = GetFloat("BoInput"),

                Ar = GetFloat("ArInput"),
                Ag = GetFloat("AgInput"),
                Ab = GetFloat("AbInput"),
                Aa = GetFloat("AaInput", 1f),
                Ao = GetFloat("AoInput")
            };
        }

        private void RequestPreview()
        {
            PreviewRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Color matrix"));
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            ApplyRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Applied Color matrix"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
