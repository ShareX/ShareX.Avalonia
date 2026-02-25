#region License Information (GPL v3)
/*
    XerahS - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2026 ShareX Team
*/
#endregion

namespace XerahS.Uploaders.PluginSystem;

/// <summary>Plugin manifest model (deserialized from plugin.json).</summary>
public class PluginManifest
{
    public string PluginId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "1.0";
    public string EntryPoint { get; set; } = string.Empty;
    public string? AssemblyFileName { get; set; }
    public List<string> SupportedCategories { get; set; } = new();
    public string? ConfigViewId { get; set; }
    public List<string> Dependencies { get; set; } = new();
    public string? HomepageUrl { get; set; }
    public bool SupportsExplorer { get; set; }

    public bool IsValid(out string? error)
    {
        if (string.IsNullOrWhiteSpace(PluginId)) { error = "PluginId is required"; return false; }
        if (string.IsNullOrWhiteSpace(Name)) { error = "Name is required"; return false; }
        if (string.IsNullOrWhiteSpace(EntryPoint)) { error = "EntryPoint is required"; return false; }
        if (string.IsNullOrWhiteSpace(ApiVersion)) { error = "ApiVersion is required"; return false; }
        if (!SupportedCategories.Any()) { error = "At least one SupportedCategory is required"; return false; }
        error = null;
        return true;
    }

    public bool IsCompatibleWith(string currentApiVersion)
    {
        var pluginMajor = GetMajorVersion(ApiVersion);
        var currentMajor = GetMajorVersion(currentApiVersion);
        return pluginMajor == currentMajor;
    }

    private static int GetMajorVersion(string version)
    {
        var parts = version.Split('.');
        return parts.Length > 0 && int.TryParse(parts[0], out var major) ? major : 0;
    }

    public string GetAssemblyFileName()
    {
        return string.IsNullOrWhiteSpace(AssemblyFileName) ? $"{PluginId}.dll" : AssemblyFileName;
    }
}
