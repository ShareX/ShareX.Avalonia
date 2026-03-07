using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace ShareX.ImageEditor.Presentation.Controls
{
    public partial class WidthPickerDropdown : UserControl
    {
        public static readonly StyledProperty<int> SelectedWidthProperty =
            AvaloniaProperty.Register<WidthPickerDropdown, int>(
                nameof(SelectedWidth),
                defaultValue: 4);

        public static readonly StyledProperty<IEnumerable<int>> WidthOptionsProperty =
            AvaloniaProperty.Register<WidthPickerDropdown, IEnumerable<int>>(
                nameof(WidthOptions),
                defaultValue: GetDefaultWidthOptions());

        public int SelectedWidth
        {
            get => GetValue(SelectedWidthProperty);
            set => SetValue(SelectedWidthProperty, value);
        }

        public IEnumerable<int> WidthOptions
        {
            get => GetValue(WidthOptionsProperty);
            set => SetValue(WidthOptionsProperty, value);
        }

        public event EventHandler<int>? WidthChanged;

        public WidthPickerDropdown()
        {
            AvaloniaXamlLoader.Load(this);

            var popup = this.FindControl<Popup>("WidthPopup");
            if (popup != null)
            {
                popup.Opened += OnPopupOpened;
            }
        }

        private void OnPopupOpened(object? sender, EventArgs e)
        {
            UpdateActiveStates();
        }

        private void UpdateActiveStates()
        {
            var popup = this.FindControl<Popup>("WidthPopup");
            if (popup?.Child is Border border && border.Child is ItemsControl itemsControl)
            {
                foreach (var item in itemsControl.GetRealizedContainers())
                {
                    if (item is ContentPresenter presenter && presenter.Child is Button button)
                    {
                        if (button.CommandParameter is int width && width == SelectedWidth)
                        {
                            button.Classes.Add("active");
                        }
                        else
                        {
                            button.Classes.Remove("active");
                        }
                    }
                }
            }
        }

        private void OnDropdownButtonClick(object? sender, RoutedEventArgs e)
        {
            var popup = this.FindControl<Popup>("WidthPopup");
            if (popup != null)
            {
                popup.IsOpen = !popup.IsOpen;
            }
        }

        private void OnWidthSelected(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is int selectedWidth)
            {
                SelectedWidth = selectedWidth;
                WidthChanged?.Invoke(this, selectedWidth);

                // Update active states before closing
                UpdateActiveStates();

                // Close the popup
                var popup = this.FindControl<Popup>("WidthPopup");
                if (popup != null)
                {
                    popup.IsOpen = false;
                }
            }
        }

        private static IEnumerable<int> GetDefaultWidthOptions()
        {
            return new List<int> { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20 };
        }
    }
}
