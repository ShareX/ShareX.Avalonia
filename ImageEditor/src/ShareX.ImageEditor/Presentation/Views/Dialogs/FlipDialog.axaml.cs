using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Core.ImageEffects.Manipulations;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class FlipDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        public FlipDialog()
        {
            AvaloniaXamlLoader.Load(this);
            AttachedToVisualTree += (s, e) => RequestPreview();
        }

        private bool GetHorizontal() => this.FindControl<CheckBox>("HorizontalCheckBox")?.IsChecked ?? false;
        private bool GetVertical() => this.FindControl<CheckBox>("VerticalCheckBox")?.IsChecked ?? false;

        private FlipImageEffect CreateEffect()
        {
            return new FlipImageEffect
            {
                Horizontally = GetHorizontal(),
                Vertically = GetVertical()
            };
        }

        private void OnCheckChanged(object? sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            RequestPreview();
        }

        private void RequestPreview()
        {
            PreviewRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Flip"));
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            ApplyRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Applied Flip"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
