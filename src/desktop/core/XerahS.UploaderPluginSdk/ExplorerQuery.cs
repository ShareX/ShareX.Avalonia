#region License Information (GPL v3)
/*
    XerahS - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2026 ShareX Team
*/
#endregion

namespace XerahS.Uploaders.PluginSystem;

/// <summary>Parameters for IUploaderExplorer.ListAsync.</summary>
public class ExplorerQuery
{
    public string? SettingsJson { get; set; }
    public string? FolderPath { get; set; }
    public string? SearchText { get; set; }
    public string? FileTypeFilter { get; set; }
    public ExplorerSortField SortBy { get; set; } = ExplorerSortField.Date;
    public bool SortDescending { get; set; } = true;
    public int PageSize { get; set; } = 50;
    public string? ContinuationToken { get; set; }
}

public enum ExplorerSortField
{
    Name,
    Date,
    Size
}
