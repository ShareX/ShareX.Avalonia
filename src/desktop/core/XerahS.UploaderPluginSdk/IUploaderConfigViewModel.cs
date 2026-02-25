#region License Information (GPL v3)
/*
    XerahS - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2026 ShareX Team
*/
#endregion

namespace XerahS.Uploaders.PluginSystem;

/// <summary>
/// Interface for uploader-specific configuration ViewModels.
/// </summary>
public interface IUploaderConfigViewModel
{
    void LoadFromJson(string json);
    string ToJson();
    bool Validate();
}
