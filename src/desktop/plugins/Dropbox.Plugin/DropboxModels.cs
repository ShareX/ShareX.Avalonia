#region License Information (GPL v3)

/*
    XerahS - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2026 ShareX Team

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

using Newtonsoft.Json;

namespace ShareX.Dropbox.Plugin;

public sealed class DropboxAccount
{
    [JsonProperty("account_id")]
    public string? AccountId { get; set; }

    [JsonProperty("name")]
    public DropboxAccountName? Name { get; set; }

    [JsonProperty("email")]
    public string? Email { get; set; }
}

public sealed class DropboxAccountName
{
    [JsonProperty("display_name")]
    public string? DisplayName { get; set; }
}

public sealed class DropboxMetadata
{
    [JsonProperty(".tag")]
    public string Tag { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("path_lower")]
    public string? PathLower { get; set; }

    [JsonProperty("path_display")]
    public string? PathDisplay { get; set; }

    [JsonProperty("client_modified")]
    public string? ClientModified { get; set; }

    [JsonProperty("server_modified")]
    public string? ServerModified { get; set; }

    [JsonProperty("size")]
    public long Size { get; set; }
}

public sealed class DropboxLinkMetadata
{
    [JsonProperty("url")]
    public string? Url { get; set; }
}

public sealed class DropboxListSharedLinksResult
{
    [JsonProperty("links")]
    public List<DropboxLinkMetadata> Links { get; set; } = new();
}

internal sealed class DropboxListFolderResult
{
    [JsonProperty("entries")]
    public List<DropboxMetadata> Entries { get; set; } = new();

    [JsonProperty("cursor")]
    public string Cursor { get; set; } = string.Empty;

    [JsonProperty("has_more")]
    public bool HasMore { get; set; }
}

internal sealed class DropboxTemporaryLinkResult
{
    [JsonProperty("link")]
    public string? Link { get; set; }
}

internal sealed class DropboxDeleteResponse
{
    [JsonProperty("metadata")]
    public DropboxMetadata? Metadata { get; set; }
}

internal sealed class DropboxCreateFolderResponse
{
    [JsonProperty("metadata")]
    public DropboxMetadata? Metadata { get; set; }
}

internal sealed class DropboxApiError
{
    [JsonProperty("error_summary")]
    public string? ErrorSummary { get; set; }
}

