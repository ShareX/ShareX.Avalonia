using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ShareX.ImageEditor.Presentation.ViewModels
{
    public partial class ConfirmationDialogViewModel : ObservableObject
    {
        public string Title { get; } = "ShareX - Image editor";
        public string Message { get; } = "There are unsaved changes.\n\nWould you like to save the changes before closing the image editor?";

        public IRelayCommand YesCommand { get; }
        public IRelayCommand NoCommand { get; }
        public IRelayCommand CancelCommand { get; }

        public ConfirmationDialogViewModel(Action onYes, Action onNo, Action onCancel)
        {
            YesCommand = new RelayCommand(onYes);
            NoCommand = new RelayCommand(onNo);
            CancelCommand = new RelayCommand(onCancel);
        }
    }
}