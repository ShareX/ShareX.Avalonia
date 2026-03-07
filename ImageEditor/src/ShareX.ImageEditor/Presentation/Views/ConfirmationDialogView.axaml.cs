using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ShareX.ImageEditor.Presentation.Views
{
    public partial class ConfirmationDialogView : UserControl
    {
        public ConfirmationDialogView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
