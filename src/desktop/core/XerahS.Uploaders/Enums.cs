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

using System.ComponentModel;

namespace XerahS.Uploaders
{
    // Legacy destination enums (ImageDestination, TextDestination, FileDestination, UrlShortenerType, URLSharingServices)
    // moved to LegacySupport/LegacyDestinationEnums.cs and deprecated; runtime uses plugin ProviderId.

    public enum HttpMethod
    {
        GET = 0,
        POST = 1,
        PUT = 2,
        PATCH = 3,
        DELETE = 4,
        HEAD = 5
    }

    public enum FTPProtocol
    {
        [Description("FTP")]
        FTP,
        [Description("FTPS (FTP over SSL)")]
        FTPS,
        [Description("SFTP (SSH FTP)")]
        SFTP
    }

    public enum BrowserProtocol
    {
        [Description("http://")]
        http,
        [Description("https://")]
        https,
        [Description("ftp://")]
        ftp,
        [Description("ftps://")]
        ftps,
        [Description("file://")]
        file,
        [Description("sftp://")]
        sftp
    }

    public enum Privacy
    {
        Public,
        Private
    }

    public enum AccountType
    {
        [Description("Anonymous")]
        Anonymous,
        [Description("User")]
        User
    }

    public enum LinkFormatEnum
    {
        [Description("Full URL")]
        URL,
        [Description("Full Image for Forums")]
        ForumImage,
        [Description("Full Image as HTML")]
        HTMLImage,
        [Description("Full Image for Wiki")]
        WikiImage,
        [Description("Shortened URL")]
        ShortenedURL,
        [Description("Linked Thumbnail for Forums")]
        ForumLinkedImage,
        [Description("Linked Thumbnail as HTML")]
        HTMLLinkedImage,
        [Description("Linked Thumbnail for Wiki")]
        WikiLinkedImage,
        [Description("Thumbnail")]
        ThumbnailURL,
        [Description("Local File path")]
        LocalFilePath,
        [Description("Local File path as URI")]
        LocalFilePathUri
    }

    public enum CustomUploaderBody
    {
        [Description("No body")]
        None,
        [Description("Form data (multipart/form-data)")]
        MultipartFormData,
        [Description("Form URL encoded (application/x-www-form-urlencoded)")]
        FormURLEncoded,
        [Description("JSON (application/json)")]
        JSON,
        [Description("XML (application/xml)")]
        XML,
        [Description("Binary")]
        Binary
    }

    [Flags]
    public enum CustomUploaderDestinationType
    {
        [Description("None")]
        None = 0,
        ImageUploader = 1, // Localized
        TextUploader = 1 << 1, // Localized
        FileUploader = 1 << 2, // Localized
        URLShortener = 1 << 3, // Localized
        URLSharingService = 1 << 4 // Localized
    }

    public enum FTPSEncryption
    {
        /// <summary>
        /// Connection starts in plain text and encryption is enabled with the AUTH command immediately after the server greeting.
        /// </summary>
        Explicit,
        /// <summary>
        /// Encryption is used from the start of the connection, port 990
        /// </summary>
        Implicit
    }

    public enum OAuthLoginStatus
    {
        LoginRequired,
        LoginSuccessful,
        LoginFailed
    }

    public enum YouTubeVideoPrivacy // Localized
    {
        Public,
        Unlisted,
        Private
    }

    public enum BoxShareAccessLevel
    {
        [Description("Public - People with the link")]
        Open,
        [Description("Company - People in your company")]
        Company,
        [Description("Collaborators - Invited people only")]
        Collaborators
    }
}
