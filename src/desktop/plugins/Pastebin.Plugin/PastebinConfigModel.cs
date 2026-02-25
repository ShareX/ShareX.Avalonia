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

namespace ShareX.Pastebin.Plugin;

public enum PastebinPrivacy
{
    Public,
    Unlisted,
    Private
}

public enum PastebinExpiration
{
    N,
    M10,
    H1,
    D1,
    W1,
    W2,
    M1
}

/// <summary>
/// Configuration model for Pastebin text uploader
/// </summary>
public class PastebinConfigModel
{
    /// <summary>
    /// Pastebin API developer key (get one at https://pastebin.com/doc_api)
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public PastebinPrivacy Exposure { get; set; } = PastebinPrivacy.Unlisted;
    public PastebinExpiration Expiration { get; set; } = PastebinExpiration.N;
    public string Title { get; set; } = string.Empty;
    public string TextFormat { get; set; } = "text";
    public string? UserKey { get; set; }
    public bool RawURL { get; set; }
}
