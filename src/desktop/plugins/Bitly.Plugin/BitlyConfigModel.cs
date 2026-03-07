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

namespace ShareX.Bitly.Plugin;

/// <summary>
/// Configuration model for Bitly URL shortener (OAuth2 + optional custom domain).
/// </summary>
public class BitlyConfigModel
{
    /// <summary>
    /// Bitly OAuth2 client ID (from https://bitly.com/a/settings/advanced).
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Bitly OAuth2 client secret.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Optional custom short domain (e.g. bit.ly or a custom branded domain).
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 access token. Obtain via Bitly OAuth flow or paste after authorizing in browser.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
}
