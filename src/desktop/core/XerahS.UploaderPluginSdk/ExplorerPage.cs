#region License Information (GPL v3)
/*
    XerahS - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2026 ShareX Team
*/
#endregion

namespace XerahS.Uploaders.PluginSystem;

/// <summary>A single page of results from IUploaderExplorer.ListAsync.</summary>
public class ExplorerPage
{
    public IReadOnlyList<MediaItem> Items { get; set; } = Array.Empty<MediaItem>();
    public string? ContinuationToken { get; set; }
    public int? TotalCount { get; set; }
}
