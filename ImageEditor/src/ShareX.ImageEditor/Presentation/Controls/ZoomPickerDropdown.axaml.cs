using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace ShareX.ImageEditor.Presentation.Controls
{
    public partial class ZoomPickerDropdown : UserControl
    {
        public static readonly StyledProperty<double> SelectedZoomProperty =
            AvaloniaProperty.Register<ZoomPickerDropdown, double>(
                nameof(SelectedZoom),
                defaultValue: 1.0);

        public static readonly StyledProperty<IEnumerable<double>> ZoomLevelsProperty =
            AvaloniaProperty.Register<ZoomPickerDropdown, IEnumerable<double>>(
                nameof(ZoomLevels),
                defaultValue: GetDefaultZoomLevels());

        public double SelectedZoom
        {
            get => GetValue(SelectedZoomProperty);
            set => SetValue(SelectedZoomProperty, value);
        }

        public IEnumerable<double> ZoomLevels
        {
            get => GetValue(ZoomLevelsProperty);
            set => SetValue(ZoomLevelsProperty, value);
        }

        public event EventHandler<double>? ZoomChanged;
        public event EventHandler? ZoomToFitRequested;

        public ZoomPickerDropdown()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnDropdownButtonClick(object? sender, RoutedEventArgs e)
        {
            var popup = this.FindControl<Popup>("ZoomPopup");
            if (popup != null)
            {
                popup.IsOpen = !popup.IsOpen;
            }
        }

        private void OnZoomSelected(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is double selectedZoom)
            {
                SelectedZoom = selectedZoom;
                ZoomChanged?.Invoke(this, selectedZoom);

                // Close the popup
                var popup = this.FindControl<Popup>("ZoomPopup");
                if (popup != null)
                {
                    popup.IsOpen = false;
                }
            }
        }

        private void OnZoomToFitSelected(object? sender, RoutedEventArgs e)
        {
            var popup = this.FindControl<Popup>("ZoomPopup");
            if (popup != null)
            {
                popup.IsOpen = false;
            }

            ZoomToFitRequested?.Invoke(this, EventArgs.Empty);
        }

        private static IEnumerable<double> GetDefaultZoomLevels()
        {
            return new List<double> { 0.25, 0.5, 0.75, 1.0, 1.25, 1.5, 2.0, 3.0, 4.0 };
        }
    }
}
