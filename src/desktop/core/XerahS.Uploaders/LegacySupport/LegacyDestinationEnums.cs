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

namespace XerahS.Uploaders;

/// <summary>
/// Legacy image destination enum. Not used by runtime; plugin system uses ProviderId.
/// Kept for ApplicationConfig / serialization compatibility only.
/// </summary>
[Obsolete("Legacy; use plugin ProviderId (e.g. DestinationInstanceId). Kept for config serialization.")]
[EditorBrowsable(EditorBrowsableState.Advanced)]
[Description("Image uploaders"), DefaultValue(Imgur)]
public enum ImageDestination
{
    [Description("Imgur")]
    Imgur,
    [Description("ImageShack")]
    ImageShack,
    [Description("Flickr")]
    Flickr,
    [Description("Photobucket")]
    Photobucket,
    [Description("vgy.me")]
    Vgyme,
    CustomImageUploader, // Localized
    FileUploader // Localized
}

/// <summary>
/// Legacy text destination enum. Not used by runtime; plugin system uses ProviderId.
/// Kept for ApplicationConfig / serialization compatibility only.
/// </summary>
[Obsolete("Legacy; use plugin ProviderId (e.g. DestinationInstanceId). Kept for config serialization.")]
[EditorBrowsable(EditorBrowsableState.Advanced)]
[Description("Text uploaders"), DefaultValue(TextDestination.Pastebin)]
public enum TextDestination
{
    [Description("Pastebin")]
    Pastebin,
    [Description("Paste2")]
    Paste2,
    [Description("Slexy")]
    Slexy,
    [Description("Paste.ee")]
    Paste_ee,
    [Description("GitHub Gist")]
    Gist,
    [Description("uPaste")]
    Upaste,
    [Description("Hastebin")]
    Hastebin,
    [Description("OneTimeSecret")]
    OneTimeSecret,
    [Description("Pastie")]
    Pastie,
    CustomTextUploader, // Localized
    FileUploader // Localized
}

/// <summary>
/// Legacy file destination enum. Not used by runtime; plugin system uses ProviderId.
/// Kept for ApplicationConfig / serialization compatibility only.
/// </summary>
[Obsolete("Legacy; use plugin ProviderId (e.g. DestinationInstanceId). Kept for config serialization.")]
[EditorBrowsable(EditorBrowsableState.Advanced)]
[Description("File uploaders"), DefaultValue(Dropbox)]
public enum FileDestination
{
    [Description("Dropbox")]
    Dropbox,
    [Description("FTP")]
    FTP,
    [Description("OneDrive")]
    OneDrive,
    [Description("Google Drive")]
    GoogleDrive,
    [Description("puush")]
    Puush,
    [Description("Box")]
    Box,
    [Description("MEGA")]
    Mega,
    [Description("Amazon S3")]
    AmazonS3,
    [Description("Google Cloud Storage")]
    GoogleCloudStorage,
    [Description("Azure Storage")]
    AzureStorage,
    [Description("Backblaze B2")]
    BackblazeB2,
    [Description("ownCloud / Nextcloud")]
    OwnCloud,
    [Description("MediaFire")]
    MediaFire,
    [Description("Pushbullet")]
    Pushbullet,
    [Description("SendSpace")]
    SendSpace,
    [Description("Hostr")]
    Localhostr,
    [Description("Lambda")]
    Lambda,
    [Description("Pomf")]
    Pomf,
    [Description("Uguu")]
    Uguu,
    [Description("Seafile")]
    Seafile,
    [Description("Streamable")]
    Streamable,
    [Description("s-ul")]
    Sul,
    [Description("LobFile")]
    Lithiio,
    [Description("transfer.sh")]
    Transfersh,
    [Description("Plik")]
    Plik,
    [Description("YouTube")]
    YouTube,
    [Description("Vault.ooo")]
    Vault_ooo,
    SharedFolder, // Localized
    Email, // Localized
    CustomFileUploader // Localized
}

/// <summary>
/// Legacy URL shortener type enum. Not used by runtime; plugin system uses ProviderId.
/// Kept for TaskSettings / serialization compatibility only.
/// </summary>
[Obsolete("Legacy; use plugin ProviderId (e.g. UrlShortenerDestinationInstanceId). Kept for config serialization.")]
[EditorBrowsable(EditorBrowsableState.Advanced)]
[Description("URL shorteners"), DefaultValue(BITLY)]
public enum UrlShortenerType
{
    [Description("bit.ly")]
    BITLY,
    [Description("is.gd")]
    ISGD,
    [Description("v.gd")]
    VGD,
    [Description("tinyurl.com")]
    TINYURL,
    [Description("turl.ca")]
    TURL,
    [Description("yourls.org")]
    YOURLS,
    [Description("qr.net")]
    QRnet,
    [Description("vurl.com")]
    VURL,
    [Description("2.gp")]
    TwoGP,
    [Description("Polr")]
    Polr,
    [Description("Firebase Dynamic Links")]
    FirebaseDynamicLinks,
    [Description("Kutt")]
    Kutt,
    [Description("Zero Width Shortener")]
    ZeroWidthShortener,
    CustomURLShortener // Localized
}

/// <summary>
/// Legacy URL sharing service enum. Not used by runtime; plugin system uses ProviderId.
/// Kept for TaskSettings / serialization compatibility only.
/// </summary>
[Obsolete("Legacy; use plugin ProviderId. Kept for config serialization.")]
[EditorBrowsable(EditorBrowsableState.Advanced)]
[Description("URL sharing services"), DefaultValue(Email)]
public enum URLSharingServices
{
    Email, // Localized
    [Description("Facebook")]
    Facebook,
    [Description("Reddit")]
    Reddit,
    [Description("Pinterest")]
    Pinterest,
    [Description("Tumblr")]
    Tumblr,
    [Description("LinkedIn")]
    LinkedIn,
    [Description("StumbleUpon")]
    StumbleUpon,
    [Description("Delicious")]
    Delicious,
    [Description("VK")]
    VK,
    [Description("Pushbullet")]
    Pushbullet,
    GoogleImageSearch, // Localized
    BingVisualSearch, // Localized
    CustomURLSharingService // Localized
}
