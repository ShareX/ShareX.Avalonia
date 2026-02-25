#region License Information (GPL v3)
/*
    XerahS - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2026 ShareX Team
*/
#endregion

namespace XerahS.Uploaders.PluginSystem;

/// <summary>Represents a configured instance of an uploader provider.</summary>
public class UploaderInstance
{
    public string InstanceId { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public UploaderCategory Category { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string SettingsJson { get; set; } = "{}";
    public FileTypeScope FileTypeRouting { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public bool IsAvailable { get; set; } = true;
}
