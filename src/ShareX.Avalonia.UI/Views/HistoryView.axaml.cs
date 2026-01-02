using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ShareX.Ava.History;
using ShareX.Ava.UI.ViewModels;
using ShareX.Ava.Common;
using System.Diagnostics;

namespace ShareX.Ava.UI.Views
{
    public partial class HistoryView : UserControl
    {
        public HistoryView()
        {
            InitializeComponent();
            DataContext = new HistoryViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void OnItemPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // Only handle double-left-click
            // Right-click is handled natively by Avalonia ContextMenu
            var point = e.GetCurrentPoint(sender as Visual);
            
            if (e.ClickCount == 2 && point.Properties.IsLeftButtonPressed)
            {
                if (sender is not Border border || border.DataContext is not HistoryItem item)
                    return;

                if (DataContext is HistoryViewModel vm)
                {
                    DebugHelper.WriteLine($"HistoryView.OnItemPointerPressed - Double-click detected on item: {item.FileName}");
                    await vm.EditImageCommand.ExecuteAsync(item);
                    e.Handled = true;
                }
            }
        }
    }
}
