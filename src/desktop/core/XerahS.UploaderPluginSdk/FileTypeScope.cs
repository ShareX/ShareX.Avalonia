#region License Information (GPL v3)
/*
    XerahS - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2026 ShareX Team
*/
#endregion

namespace XerahS.Uploaders.PluginSystem;

/// <summary>Defines which file types an uploader instance handles within its category.</summary>
public class FileTypeScope
{
    public bool AllFileTypes { get; set; }
    public List<string> FileExtensions { get; set; } = new();

    public bool HandlesExtension(string extension)
    {
        if (AllFileTypes) return true;
        return FileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}
