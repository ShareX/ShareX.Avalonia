using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareX.Ava.Core;
using ShareX.Ava.Common;
using ShareX.Ava.Core.Tasks;
using ShareX.Ava.History;

namespace ShareX.Ava.UI.ViewModels
{
    public partial class HistoryViewModel : ViewModelBase
    {
        // Converter for view toggle button text
        public static IValueConverter ViewToggleConverter { get; } = new FuncValueConverter<bool, string>(
            isGrid => isGrid ? "📋 List View" : "🔲 Grid View");

        // Converter to load thumbnail from file path (resource-efficient)
        public static IValueConverter ThumbnailConverter { get; } = new FuncValueConverter<string?, Bitmap?>(
            filePath =>
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    return null;
                
                try
                {
                    // Check if it's an image file
                    var ext = Path.GetExtension(filePath).ToLowerInvariant();
                    if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".gif" && ext != ".bmp" && ext != ".webp")
                        return null;
                    
                    // Load with decode size for memory efficiency (thumbnail size)
                    using var stream = File.OpenRead(filePath);
                    return Bitmap.DecodeToWidth(stream, 180); // Decode to thumbnail width
                }
                catch
                {
                    return null;
                }
            });

        [ObservableProperty]
        private ObservableCollection<HistoryItem> _historyItems;

        [ObservableProperty]
        private bool _isGridView = true;

        private readonly HistoryManager _historyManager;

        public HistoryViewModel()
        {
            HistoryItems = new ObservableCollection<HistoryItem>();
            
            // Create history manager with centralized path
            var historyPath = SettingManager.GetHistoryFilePath();
            DebugHelper.WriteLine($"HistoryViewModel - History file path: {historyPath}");

            _historyManager = new HistoryManagerXML(historyPath);
            
            LoadHistory();
        }

        [RelayCommand]
        private void LoadHistory()
        {
            var historyPath = SettingManager.GetHistoryFilePath();
            DebugHelper.WriteLine($"History.xml location: {historyPath} (exists={File.Exists(historyPath)})");
            
            var items = _historyManager.GetHistoryItems();
            HistoryItems.Clear();
            foreach (var item in items)
            {
                HistoryItems.Add(item);
            }
        }

        [RelayCommand]
        private void ToggleView()
        {
            IsGridView = !IsGridView;
        }

        [RelayCommand]
        private void RefreshHistory()
        {
            LoadHistory();
        }

        [RelayCommand]
        private async Task EditImage(HistoryItem? item)
        {
            if (item == null || string.IsNullOrEmpty(item.FilePath)) return;
            if (!File.Exists(item.FilePath)) return;

            try
            {
                // Load the image from file
                using var fs = new FileStream(item.FilePath, FileMode.Open, FileAccess.Read);
                var image = System.Drawing.Image.FromStream(fs);
                
                // Open in Editor using the platform service
                await ShareX.Ava.Platform.Abstractions.PlatformServices.UI.ShowEditorAsync(image);
            }
            catch (Exception ex)
            {
                DebugHelper.WriteLine($"Failed to open image in editor: {ex.Message}");
            }
        }

        [RelayCommand]
        private void OpenFile(HistoryItem? item)
        {
            if (item == null || string.IsNullOrEmpty(item.FilePath)) return;
            if (!File.Exists(item.FilePath)) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = item.FilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                DebugHelper.WriteLine($"Failed to open file: {ex.Message}");
            }
        }

        [RelayCommand]
        private void OpenFolder(HistoryItem? item)
        {
            if (item == null || string.IsNullOrEmpty(item.FilePath)) return;
            if (!File.Exists(item.FilePath)) return;

            try
            {
                // Open folder and select the file
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{item.FilePath}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                DebugHelper.WriteLine($"Failed to open folder: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task CopyFilePath(HistoryItem? item)
        {
            if (item == null || string.IsNullOrEmpty(item.FilePath)) return;

            try
            {
                // Get clipboard from the main window
                if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                    && desktop.MainWindow != null)
                {
                    var clipboard = desktop.MainWindow.Clipboard;
                    if (clipboard != null)
                    {
                        await clipboard.SetTextAsync(item.FilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugHelper.WriteLine($"Failed to copy file path: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task CopyURL(HistoryItem? item)
        {
            if (item == null || string.IsNullOrEmpty(item.URL)) return;

            try
            {
                // Get clipboard from the main window
                if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                    && desktop.MainWindow != null)
                {
                    var clipboard = desktop.MainWindow.Clipboard;
                    if (clipboard != null)
                    {
                        await clipboard.SetTextAsync(item.URL);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugHelper.WriteLine($"Failed to copy URL: {ex.Message}");
            }
        }

        [RelayCommand]
        private void DeleteItem(HistoryItem? item)
        {
            if (item == null) return;
            
            // Remove from the observable collection (UI update)
            HistoryItems.Remove(item);
            
            // TODO: Persist deletion to history file
            DebugHelper.WriteLine($"Deleted history item: {item.FileName}");
        }
    }
}
