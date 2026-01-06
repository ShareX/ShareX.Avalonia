using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ShareX.Ava.UI.Controls;
using ShareX.Ava.UI.ViewModels;

namespace ShareX.Ava.UI.Views
{
    public partial class TaskSettingsPanel : UserControl
    {
        public TaskSettingsPanel()
        {
            InitializeComponent();
            
            // Wire up PropertyGrid property changes to preview updates
            var propertyGrid = this.FindControl<PropertyGrid>("EffectPropertyGrid");
            if (propertyGrid != null)
            {
                propertyGrid.PropertyValueChanged += (s, e) =>
                {
                    if (DataContext is TaskSettingsViewModel vm)
                    {
                        vm.ImageEffects.UpdatePreview();
                    }
                };
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
