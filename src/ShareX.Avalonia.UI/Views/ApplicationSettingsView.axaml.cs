using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;

namespace ShareX.Avalonia.UI.Views
{
    public partial class ApplicationSettingsView : UserControl
    {
        public ApplicationSettingsView()
        {
            InitializeComponent();
            var vm = new ViewModels.SettingsViewModel();
            DataContext = vm;
            
            // Wire up the edit requester
            vm.HotkeySettings.EditHotkeyRequester = async (settings) => 
            {
                var editVm = new ViewModels.HotkeyEditViewModel(settings);
                var dialog = new HotkeyEditView
                {
                    DataContext = editVm
                };
                
                if (VisualRoot is Window window)
                {
                   return await dialog.ShowDialog<bool>(window);
                }
                
                return false;
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
