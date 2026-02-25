#region License Information (GPL v3)
/*
    XerahS - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2026 ShareX Team
    This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License.
*/
#endregion

namespace XerahS.Uploaders.PluginSystem;

/// <summary>
/// Interface for uploader destination providers (plugins).
/// Implement this and expose it via your plugin's entry point (see plugin.json).
/// </summary>
public interface IUploaderProvider
{
    string ProviderId { get; }
    string Name { get; }
    string Description { get; }
    Version Version { get; }
    UploaderCategory[] SupportedCategories { get; }
    Type ConfigModelType { get; }

    /// <summary>Creates a configuration view (e.g. Avalonia UserControl). Return null for default property grid.</summary>
    object? CreateConfigView();

    /// <summary>Creates a configuration ViewModel. Return null if no custom VM.</summary>
    IUploaderConfigViewModel? CreateConfigViewModel();

    /// <summary>
    /// Creates an uploader instance from serialized JSON settings.
    /// Must return an instance that the host can use to perform uploads (e.g. a type that extends XerahS.Uploaders.Uploader when referencing XerahS.Uploaders).
    /// </summary>
    object CreateInstance(string settingsJson);

    Dictionary<UploaderCategory, string[]> GetSupportedFileTypes();
    bool ValidateSettings(string settingsJson);
    string GetDefaultSettings(UploaderCategory category);
    event EventHandler? ConfigChanged;
}
