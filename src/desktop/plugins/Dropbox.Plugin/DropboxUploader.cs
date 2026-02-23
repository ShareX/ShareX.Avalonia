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
using XerahS.Common;
using XerahS.Uploaders;
using System.Collections.Specialized;
using System.Linq;

namespace ShareX.Dropbox.Plugin;

/// <summary>
/// Dropbox uploader with OAuth2 code flow.
/// </summary>
public sealed class DropboxUploader : FileUploader, IOAuth2Basic
{
    private const string ContentTypeJson = "application/json";
    private const string ContentTypeOctetStream = "application/octet-stream";

    private const string ApiVersion = "2";

    private const string UrlWeb = "https://www.dropbox.com";
    private const string UrlApiBase = "https://api.dropboxapi.com";
    private const string UrlApi = UrlApiBase + "/" + ApiVersion;
    private const string UrlContent = "https://content.dropboxapi.com/" + ApiVersion;

    private const string UrlOAuth2Authorize = UrlWeb + "/oauth2/authorize";
    private const string UrlOAuth2Token = UrlApiBase + "/oauth2/token";

    private const string UrlGetCurrentAccount = UrlApi + "/users/get_current_account";
    private const string UrlUpload = UrlContent + "/files/upload";
    private const string UrlCreateSharedLink = UrlApi + "/sharing/create_shared_link_with_settings";
    private const string UrlListSharedLinks = UrlApi + "/sharing/list_shared_links";

    private readonly DropboxConfigModel _config;
    private readonly OAuth2Info _authInfo;
    private readonly Action<OAuth2Token>? _tokenUpdated;

    public OAuth2Info AuthInfo => _authInfo;

    public DropboxUploader(DropboxConfigModel config, OAuth2Info authInfo, Action<OAuth2Token>? tokenUpdated = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _authInfo = authInfo ?? throw new ArgumentNullException(nameof(authInfo));
        _tokenUpdated = tokenUpdated;
    }

    public override UploadResult Upload(Stream stream, string fileName)
    {
        return UploadFile(stream, _config.UploadPath, fileName, _config.AutoCreateShareableLink, _config.UseDirectLink);
    }

    public string GetAuthorizationURL()
    {
        string redirectUri = string.IsNullOrWhiteSpace(_config.RedirectUri) ? Links.Callback : _config.RedirectUri;
        return GetAuthorizationURL(redirectUri);
    }

    public string GetAuthorizationURL(string redirectUri, string? state = null, OAuth2ProofKey? proofKey = null)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            redirectUri = Links.Callback;
        }

        Dictionary<string, string> args = new()
        {
            ["response_type"] = "code",
            ["client_id"] = _authInfo.Client_ID,
            ["redirect_uri"] = redirectUri,
            ["token_access_type"] = "offline"
        };

        if (!string.IsNullOrWhiteSpace(state))
        {
            args["state"] = state;
        }

        if (proofKey != null)
        {
            args["code_challenge"] = proofKey.CodeChallenge;
            args["code_challenge_method"] = proofKey.ChallengeMethod;
        }

        return URLHelpers.CreateQueryString(UrlOAuth2Authorize, args);
    }

    public bool GetAccessToken(string code)
    {
        string redirectUri = string.IsNullOrWhiteSpace(_config.RedirectUri) ? Links.Callback : _config.RedirectUri;
        return GetAccessToken(code, redirectUri);
    }

    public bool GetAccessToken(string code, string redirectUri, string? codeVerifier = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            redirectUri = Links.Callback;
        }

        Dictionary<string, string> args = new()
        {
            ["client_id"] = _authInfo.Client_ID,
            ["grant_type"] = "authorization_code",
            ["code"] = code.Trim(),
            ["redirect_uri"] = redirectUri
        };

        if (!string.IsNullOrWhiteSpace(_authInfo.Client_Secret))
        {
            args["client_secret"] = _authInfo.Client_Secret;
        }

        if (!string.IsNullOrWhiteSpace(codeVerifier))
        {
            args["code_verifier"] = codeVerifier;
        }

        string? response = SendRequestURLEncoded(XerahS.Uploaders.HttpMethod.POST, UrlOAuth2Token, args);
        return ApplyTokenResponse(response);
    }

    public bool RefreshAccessToken()
    {
        OAuth2Token? currentToken = _authInfo.Token;
        if (currentToken == null || string.IsNullOrWhiteSpace(currentToken.refresh_token))
        {
            return false;
        }

        Dictionary<string, string> args = new()
        {
            ["client_id"] = _authInfo.Client_ID,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = currentToken.refresh_token
        };

        if (!string.IsNullOrWhiteSpace(_authInfo.Client_Secret))
        {
            args["client_secret"] = _authInfo.Client_Secret;
        }

        string? response = SendRequestURLEncoded(XerahS.Uploaders.HttpMethod.POST, UrlOAuth2Token, args);
        return ApplyTokenResponse(response);
    }

    public bool CheckAuthorization()
    {
        if (!HasOAuthConfiguration())
        {
            Errors.Add("Dropbox login is required.");
            return false;
        }

        OAuth2Token token = _authInfo.Token;
        if (NeedsRefresh(token) && !RefreshAccessToken())
        {
            Errors.Add("Dropbox access token refresh failed. Please login again.");
            return false;
        }

        return true;
    }

    public DropboxAccount? GetCurrentAccount()
    {
        if (!CheckAuthorization())
        {
            return null;
        }

        string? response = SendRequest(
            XerahS.Uploaders.HttpMethod.POST,
            UrlGetCurrentAccount,
            "null",
            ContentTypeJson,
            null,
            GetAuthHeaders());

        if (string.IsNullOrWhiteSpace(response))
        {
            return null;
        }

        return JsonConvert.DeserializeObject<DropboxAccount>(response);
    }

    public UploadResult UploadFile(Stream stream, string path, string fileName, bool createShareableLink = false, bool useDirectLink = false)
    {
        UploadResult result = new();

        if (stream.Length > 150_000_000)
        {
            Errors.Add("There is a 150 MB limit to uploads through the Dropbox API upload endpoint.");
            return result;
        }

        if (!CheckAuthorization())
        {
            return result;
        }

        string parsedPath = NameParser.Parse(NameParserType.Default, VerifyPath(path));
        string uploadPath = VerifyPath(parsedPath, fileName);

        string json = JsonConvert.SerializeObject(new
        {
            path = uploadPath,
            mode = "overwrite",
            autorename = false,
            mute = true
        });

        Dictionary<string, string> args = new()
        {
            ["arg"] = json
        };

        string? response = SendRequest(
            XerahS.Uploaders.HttpMethod.POST,
            UrlUpload,
            stream,
            ContentTypeOctetStream,
            args,
            GetAuthHeaders());

        if (string.IsNullOrWhiteSpace(response))
        {
            return result;
        }

        result.Response = response;
        result.IsSuccess = true;

        if (!createShareableLink)
        {
            result.IsURLExpected = false;
            return result;
        }

        DropboxMetadata? metadata = JsonConvert.DeserializeObject<DropboxMetadata>(response);
        if (!string.IsNullOrWhiteSpace(metadata?.PathDisplay))
        {
            result.URL = CreateShareableLink(metadata.PathDisplay, useDirectLink);
        }

        return result;
    }

    public string? CreateShareableLink(string path, bool directLink)
    {
        if (!CheckAuthorization())
        {
            return null;
        }

        string json = JsonConvert.SerializeObject(new
        {
            path = VerifyPath(path),
            settings = new
            {
                requested_visibility = "public"
            }
        });

        string? response = SendRequest(
            XerahS.Uploaders.HttpMethod.POST,
            UrlCreateSharedLink,
            json,
            ContentTypeJson,
            null,
            GetAuthHeaders());

        DropboxLinkMetadata? linkMetadata = null;
        if (!string.IsNullOrWhiteSpace(response))
        {
            linkMetadata = JsonConvert.DeserializeObject<DropboxLinkMetadata>(response);
        }
        else if (IsError && Errors.Errors.LastOrDefault()?.Text.Contains("shared_link_already_exists", StringComparison.OrdinalIgnoreCase) == true)
        {
            DropboxListSharedLinksResult? existingLinks = ListSharedLinks(path, true);
            linkMetadata = existingLinks?.Links.FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(linkMetadata?.Url))
        {
            return null;
        }

        return directLink ? GetDirectShareableUrl(linkMetadata.Url) : linkMetadata.Url;
    }

    public DropboxListSharedLinksResult? ListSharedLinks(string path, bool directOnly = false)
    {
        if (!CheckAuthorization())
        {
            return null;
        }

        string json = JsonConvert.SerializeObject(new
        {
            path = VerifyPath(path),
            direct_only = directOnly
        });

        string? response = SendRequest(
            XerahS.Uploaders.HttpMethod.POST,
            UrlListSharedLinks,
            json,
            ContentTypeJson,
            null,
            GetAuthHeaders());

        if (string.IsNullOrWhiteSpace(response))
        {
            return null;
        }

        return JsonConvert.DeserializeObject<DropboxListSharedLinksResult>(response);
    }

    public static string VerifyPath(string? path, string? fileName = null)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            string normalized = path.Trim().Replace('\\', '/').Trim('/');
            normalized = URLHelpers.AddSlash(normalized, SlashType.Prefix);

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                normalized = URLHelpers.CombineURL(normalized, fileName);
            }

            return normalized;
        }

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            return fileName;
        }

        return string.Empty;
    }

    private NameValueCollection GetAuthHeaders()
    {
        return new NameValueCollection
        {
            ["Authorization"] = "Bearer " + _authInfo.Token.access_token
        };
    }

    private bool ApplyTokenResponse(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return false;
        }

        OAuth2Token? token = JsonConvert.DeserializeObject<OAuth2Token>(response);
        if (token == null || string.IsNullOrWhiteSpace(token.access_token))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(token.refresh_token) && _authInfo.Token != null)
        {
            token.refresh_token = _authInfo.Token.refresh_token;
        }

        if (token.expires_in > 0)
        {
            token.UpdateExpireDate();
        }
        else
        {
            token.ExpireDate = DateTime.MaxValue;
        }

        _authInfo.Token = token;
        _tokenUpdated?.Invoke(token);
        return true;
    }

    private bool HasOAuthConfiguration()
    {
        return !string.IsNullOrWhiteSpace(_authInfo.Client_ID)
            && _authInfo.Token != null
            && !string.IsNullOrWhiteSpace(_authInfo.Token.access_token);
    }

    private static bool NeedsRefresh(OAuth2Token token)
    {
        if (token.ExpireDate == DateTime.MinValue)
        {
            return token.expires_in > 0 || !string.IsNullOrWhiteSpace(token.refresh_token);
        }

        return token.IsExpired;
    }

    private static string? GetDirectShareableUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? originalUri))
        {
            return null;
        }

        UriBuilder builder = new(originalUri)
        {
            Scheme = "https",
            Host = "dl.dropboxusercontent.com",
            Port = -1
        };

        Dictionary<string, string> queryParams = ParseQuery(builder.Query);
        queryParams.Remove("dl");
        queryParams["raw"] = "1";
        builder.Query = BuildQuery(queryParams);

        return builder.Uri.AbsoluteUri;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
        {
            return result;
        }

        string trimmed = query.TrimStart('?');
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return result;
        }

        string[] parts = trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (string part in parts)
        {
            int separatorIndex = part.IndexOf('=');
            if (separatorIndex <= 0)
            {
                result[Uri.UnescapeDataString(part)] = string.Empty;
                continue;
            }

            string key = Uri.UnescapeDataString(part[..separatorIndex]);
            string value = Uri.UnescapeDataString(part[(separatorIndex + 1)..]);
            result[key] = value;
        }

        return result;
    }

    private static string BuildQuery(Dictionary<string, string> queryParams)
    {
        if (queryParams.Count == 0)
        {
            return string.Empty;
        }

        return string.Join("&", queryParams.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
    }
}
