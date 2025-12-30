using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareX.Avalonia.Core;

namespace ShareX.Avalonia.UI.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _screenshotsFolder;

        [ObservableProperty]
        private string _saveImageSubFolderPattern;

        [ObservableProperty]
        private bool _useCustomScreenshotsPath;

        [ObservableProperty]
        private bool _showTray;

        [ObservableProperty]
        private bool _silentRun;

        [ObservableProperty]
        private int _selectedTheme;

        public SettingsViewModel()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            var settings = SettingManager.Settings;
            
            _screenshotsFolder = settings.CustomScreenshotsPath ?? 
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "ShareX");
            _saveImageSubFolderPattern = settings.SaveImageSubFolderPattern ?? "%y-%mo";
            _useCustomScreenshotsPath = settings.UseCustomScreenshotsPath;
            _showTray = settings.ShowTray;
            _silentRun = settings.SilentRun;
            _selectedTheme = settings.SelectedTheme;
        }

        [RelayCommand]
        private void SaveSettings()
        {
            var settings = SettingManager.Settings;
            
            settings.CustomScreenshotsPath = ScreenshotsFolder;
            settings.SaveImageSubFolderPattern = SaveImageSubFolderPattern;
            settings.UseCustomScreenshotsPath = UseCustomScreenshotsPath;
            settings.ShowTray = ShowTray;
            settings.SilentRun = SilentRun;
            settings.SelectedTheme = SelectedTheme;
            
            // TODO: Save to disk
            // SettingManager.Save();
        }

        [RelayCommand]
        private void BrowseFolder()
        {
            // TODO: Implement folder picker dialog
        }

        [RelayCommand]
        private void ResetToDefaults()
        {
            ScreenshotsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "ShareX");
            SaveImageSubFolderPattern = "%y-%mo";
            UseCustomScreenshotsPath = false;
            ShowTray = true;
            SilentRun = false;
            SelectedTheme = 0;
        }
    }
}
