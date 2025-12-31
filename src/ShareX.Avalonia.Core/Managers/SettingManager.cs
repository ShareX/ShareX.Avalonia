#region License Information (GPL v3)

/*
    ShareX.Ava - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2025 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using ShareX.Ava.Common;
using ShareX.Ava.Uploaders;
using System;
using System.IO;

namespace ShareX.Ava.Core;

/// <summary>
/// Manages loading and saving of all application settings.
/// Provides centralized access to all configuration objects.
/// </summary>
public static class SettingManager
{
    #region Constants

    public const string ApplicationConfigFileName = "ApplicationConfig.json";
    public const string UploadersConfigFileName = "UploadersConfig.json";
    public const string HotkeysConfigFileName = "HotkeysConfig.json";
    public const string BackupFolder = "Backup";

    #endregion

    #region Static Properties

    /// <summary>
    /// Root folder for user settings
    /// </summary>
    public static string PersonalFolder { get; set; } = "";

    /// <summary>
    /// Folder containing settings files
    /// </summary>
    public static string SettingsFolder => PersonalFolder;

    /// <summary>
    /// Backup folder path
    /// </summary>
    public static string BackupFolderPath => Path.Combine(SettingsFolder, BackupFolder);

    /// <summary>
    /// Application config file path
    /// </summary>
    public static string ApplicationConfigFilePath => Path.Combine(SettingsFolder, ApplicationConfigFileName);

    /// <summary>
    /// Uploaders config file path
    /// </summary>
    public static string UploadersConfigFilePath => Path.Combine(SettingsFolder, GetUploadersConfigFileName());

    /// <summary>
    /// Hotkeys config file path
    /// </summary>
    public static string HotkeysConfigFilePath => Path.Combine(SettingsFolder, HotkeysConfigFileName);

    /// <summary>
    /// Main application settings
    /// </summary>
    public static ApplicationConfig Settings { get; set; } = new();

    /// <summary>
    /// Uploaders configuration
    /// </summary>
    public static UploadersConfig UploadersConfig { get; set; } = new();

    /// <summary>
    /// Hotkeys configuration
    /// </summary>
    public static HotkeysConfig HotkeysConfig { get; set; } = new();

    /// <summary>
    /// Default task settings (shortcut)
    /// </summary>
    public static TaskSettings DefaultTaskSettings => Settings.DefaultTaskSettings;

    /// <summary>
    /// Recent task manager
    /// </summary>
    public static RecentTaskManager RecentTaskManager { get; } = new();

    #endregion

    #region Load Methods

    /// <summary>
    /// Load all settings from disk
    /// </summary>
    public static void LoadAllSettings()
    {
        EnsureDirectoriesExist();
        LoadApplicationConfig();
        LoadUploadersConfig();
        LoadHotkeysConfig();
        InitializeRecentTasks();
    }

    /// <summary>
    /// Load application config from file
    /// </summary>
    public static void LoadApplicationConfig()
    {
        if (File.Exists(ApplicationConfigFilePath))
        {
            try
            {
                string json = File.ReadAllText(ApplicationConfigFilePath);
                var loaded = JsonHelpers.DeserializeFromString<ApplicationConfig>(json);
                if (loaded != null)
                {
                    Settings = loaded;
                }
            }
            catch (Exception ex)
            {
                DebugHelper.WriteException(ex, "Failed to load ApplicationConfig");
            }
        }
    }

    /// <summary>
    /// Load uploaders config from file
    /// </summary>
    public static void LoadUploadersConfig()
    {
        if (File.Exists(UploadersConfigFilePath))
        {
            try
            {
                string json = File.ReadAllText(UploadersConfigFilePath);
                var loaded = JsonHelpers.DeserializeFromString<UploadersConfig>(json);
                if (loaded != null)
                {
                    UploadersConfig = loaded;
                }
            }
            catch (Exception ex)
            {
                DebugHelper.WriteException(ex, "Failed to load UploadersConfig");
            }
        }
    }

    /// <summary>
    /// Load hotkeys config from file
    /// </summary>
    public static void LoadHotkeysConfig()
    {
        if (File.Exists(HotkeysConfigFilePath))
        {
            try
            {
                string json = File.ReadAllText(HotkeysConfigFilePath);
                var loaded = JsonHelpers.DeserializeFromString<HotkeysConfig>(json);
                if (loaded != null)
                {
                    HotkeysConfig = loaded;
                }
            }
            catch (Exception ex)
            {
                DebugHelper.WriteException(ex, "Failed to load HotkeysConfig");
            }
        }
    }

    private static void InitializeRecentTasks()
    {
        RecentTaskManager.Initialize(Settings.RecentTasks, Settings.RecentTasksMaxCount);
    }

    #endregion

    #region Save Methods

    /// <summary>
    /// Save all settings to disk
    /// </summary>
    public static void SaveAllSettings()
    {
        SaveApplicationConfig();
        SaveUploadersConfig();
        SaveHotkeysConfig();
    }

    /// <summary>
    /// Save application config to file
    /// </summary>
    public static void SaveApplicationConfig()
    {
        try
        {
            // Update recent tasks before saving
            if (Settings.RecentTasksSave)
            {
                Settings.RecentTasks = RecentTaskManager.ToArray();
            }
            else
            {
                Settings.RecentTasks = null;
            }

            string json = JsonHelpers.SerializeToString(Settings);
            File.WriteAllText(ApplicationConfigFilePath, json);
        }
        catch (Exception ex)
        {
            DebugHelper.WriteException(ex, "Failed to save ApplicationConfig");
        }
    }

    /// <summary>
    /// Save uploaders config to file
    /// </summary>
    public static void SaveUploadersConfig()
    {
        try
        {
            string json = JsonHelpers.SerializeToString(UploadersConfig);
            File.WriteAllText(UploadersConfigFilePath, json);
        }
        catch (Exception ex)
        {
            DebugHelper.WriteException(ex, "Failed to save UploadersConfig");
        }
    }

    /// <summary>
    /// Save hotkeys config to file
    /// </summary>
    public static void SaveHotkeysConfig()
    {
        try
        {
            string json = JsonHelpers.SerializeToString(HotkeysConfig);
            File.WriteAllText(HotkeysConfigFilePath, json);
        }
        catch (Exception ex)
        {
            DebugHelper.WriteException(ex, "Failed to save HotkeysConfig");
        }
    }

    #endregion

    #region Helper Methods

    private static string GetUploadersConfigFileName()
    {
        if (Settings.UseMachineSpecificUploadersConfig)
        {
            return $"UploadersConfig-{Environment.MachineName}.json";
        }
        return UploadersConfigFileName;
    }

    /// <summary>
    /// Reset all settings to defaults
    /// </summary>
    public static void ResetSettings()
    {
        Settings = new ApplicationConfig();
        UploadersConfig = new UploadersConfig();
        HotkeysConfig = new HotkeysConfig();
    }

    /// <summary>
    /// Ensure required directories exist
    /// </summary>
    public static void EnsureDirectoriesExist()
    {
        if (!string.IsNullOrEmpty(SettingsFolder))
        {
            FileHelpers.CreateDirectory(SettingsFolder);
        }

        if (!string.IsNullOrEmpty(BackupFolderPath))
        {
            FileHelpers.CreateDirectory(BackupFolderPath);
        }
    }

    #endregion
}
