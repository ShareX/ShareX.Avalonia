using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ShareX.Ava.UI.Views
{
    public partial class WindowSelectorDialog : UserControl
    {
        public WindowSelectorDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
