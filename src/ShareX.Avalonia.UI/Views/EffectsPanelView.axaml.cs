using Avalonia.Controls;
using Avalonia.Interactivity;
using ShareX.Avalonia.UI.ViewModels;

namespace ShareX.Avalonia.UI.Views
{
    public partial class EffectsPanelView : UserControl
    {
        public EffectsPanelView()
        {
            InitializeComponent();
        }

        private void OnCategoryClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string category && DataContext is EffectsPanelViewModel vm)
            {
                vm.SelectCategoryCommand.Execute(category);
            }
        }
    }
}
