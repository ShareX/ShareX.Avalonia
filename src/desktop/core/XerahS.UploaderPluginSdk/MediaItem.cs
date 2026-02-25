#region License Information (GPL v3)
/*
    XerahS - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2026 ShareX Team
*/
#endregion

namespace XerahS.Uploaders.PluginSystem;

/// <summary>Represents a remote file or folder returned by IUploaderExplorer.ListAsync.</summary>
public class MediaItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsFolder { get; set; }
    public long SizeBytes { get; set; }
    public string? MimeType { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? Url { get; set; }
    public string? ThumbnailUrl { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
