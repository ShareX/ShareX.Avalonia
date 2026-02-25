#region License Information (GPL v3)
/*
    XerahS - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2026 ShareX Team
*/
#endregion

namespace XerahS.Uploaders.PluginSystem;

/// <summary>
/// Optional interface for providers that support browsing remote files (Media Explorer).
/// Implement alongside IUploaderProvider.
/// </summary>
public interface IUploaderExplorer
{
    bool SupportsFolders { get; }
    Task<ExplorerPage> ListAsync(ExplorerQuery query, CancellationToken cancellation = default);
    Task<byte[]?> GetThumbnailAsync(MediaItem item, int maxWidthPx = 180, CancellationToken cancellation = default);
    Task<Stream?> GetContentAsync(MediaItem item, CancellationToken cancellation = default);
    Task<bool> DeleteAsync(MediaItem item, CancellationToken cancellation = default);
    Task<bool> CreateFolderAsync(string parentPath, string folderName, CancellationToken cancellation = default);
}
