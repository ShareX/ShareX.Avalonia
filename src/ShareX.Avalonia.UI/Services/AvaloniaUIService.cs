using System;
using System.Drawing;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ShareX.Ava.Platform.Abstractions;
using ShareX.Ava.UI.ViewModels;

namespace ShareX.Ava.UI.Services
{
    public class AvaloniaUIService : IUIService
    {
        public async Task ShowEditorAsync(System.Drawing.Image image)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (global::Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                    desktop.MainWindow is Views.MainWindow mainWindow)
                {
                    // Clone image to ensure ViewModel owns its copy
                    var imageClone = (System.Drawing.Image)image.Clone();

                    // Update ViewModel
                    MainViewModel.Current?.UpdatePreview(imageClone);

                    // Navigate
                    mainWindow.NavigateToEditor();
                }
            });
        }
    }
}
