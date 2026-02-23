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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using XerahS.Common;
using XerahS.Uploaders;
using XerahS.Uploaders.PluginSystem;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace ShareX.Dropbox.Plugin.ViewModels;

/// <summary>
/// ViewModel for Dropbox plugin configuration.
/// </summary>
public partial class DropboxConfigViewModel : ObservableObject, IUploaderConfigViewModel, IProviderContextAware
{
    [ObservableProperty]
    private string _clientId = string.Empty;

    [ObservableProperty]
    private string _clientSecret = string.Empty;

    [ObservableProperty]
    private string _uploadPath = "ShareX/%y/%mo";

    [ObservableProperty]
    private bool _autoCreateShareableLink = true;

    [ObservableProperty]
    private bool _useDirectLink;

    [ObservableProperty]
    private string _redirectUri = DropboxConfigModel.DefaultRedirectUri;

    [ObservableProperty]
    private string _authorizationCode = string.Empty;

    [ObservableProperty]
    private bool _isLoggedIn;

    [ObservableProperty]
    private bool _isLoginInProgress;

    [ObservableProperty]
    private string? _accountSummary;

    [ObservableProperty]
    private string? _statusMessage;

    private string _secretKey = Guid.NewGuid().ToString("N");
    private ISecretStore? _secrets;
    private DropboxConfigModel _config = new();
    private DropboxUploader? _uploader;
    private CancellationTokenSource? _browserLoginCts;
    private string? _pendingRedirectUri;
    private string? _pendingCodeVerifier;

    [RelayCommand]
    private async Task StartBrowserLoginAsync()
    {
        EnsureConfigFromFields(resetToken: false);

        if (!TryValidateRequiredInputs(out string? validationError))
        {
            StatusMessage = validationError;
            return;
        }

        if (!TryGetValidatedRedirectUri(out Uri redirectUri, out string? redirectUriError))
        {
            StatusMessage = redirectUriError;
            return;
        }

        PersistCredentials();
        DropboxUploader uploader = BuildUploader();
        _uploader = uploader;
        string state = Guid.NewGuid().ToString("N");
        OAuth2ProofKey proofKey = new(OAuth2ChallengeMethod.SHA256);

        _pendingRedirectUri = redirectUri.AbsoluteUri;
        _pendingCodeVerifier = proofKey.CodeVerifier;
        AuthorizationCode = string.Empty;

        _browserLoginCts?.Cancel();
        _browserLoginCts?.Dispose();
        _browserLoginCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        IsLoginInProgress = true;
        StatusMessage = "Opening Dropbox login in your browser...";

        try
        {
            using DropboxOAuthLoopbackListener listener = new(redirectUri);
            listener.Start();

            string url = uploader.GetAuthorizationURL(redirectUri.AbsoluteUri, state, proofKey);
            OpenUrl(url);

            StatusMessage = "Complete login in your browser. Waiting for callback...";
            DropboxOAuthCallbackResult callbackResult = await listener.WaitForCallbackAsync(state, _browserLoginCts.Token);
            if (!callbackResult.IsSuccess || string.IsNullOrWhiteSpace(callbackResult.Code))
            {
                string callbackError = callbackResult.ErrorDescription ?? callbackResult.Error ?? "Unknown error";
                StatusMessage = "Dropbox authorization failed: " + callbackError;
                return;
            }

            AuthorizationCode = callbackResult.Code;
            if (uploader.GetAccessToken(callbackResult.Code, redirectUri.AbsoluteUri, proofKey.CodeVerifier))
            {
                PersistToken(uploader.AuthInfo.Token);
                IsLoggedIn = true;
                AuthorizationCode = string.Empty;
                _pendingCodeVerifier = null;
                _pendingRedirectUri = null;
                StatusMessage = "Dropbox login completed.";
                LoadAccountSummary();
            }
            else
            {
                StatusMessage = "Dropbox token exchange failed. Verify app credentials and redirect URI.";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Dropbox login canceled.";
        }
        catch (HttpListenerException ex)
        {
            StatusMessage = "Unable to listen on redirect URI. " +
                            "Use a free local port and configure the same URI in Dropbox app settings. " +
                            ex.Message;
        }
        catch (Exception ex)
        {
            StatusMessage = "Dropbox login failed: " + ex.Message;
        }
        finally
        {
            IsLoginInProgress = false;
            _browserLoginCts?.Dispose();
            _browserLoginCts = null;
        }
    }

    [RelayCommand]
    private void CancelLogin()
    {
        _browserLoginCts?.Cancel();
    }

    [RelayCommand]
    private void OpenDropboxAppConsole()
    {
        OpenUrl("https://www.dropbox.com/developers/apps");
    }

    [RelayCommand]
    private void OpenLoginUrl()
    {
        EnsureConfigFromFields(resetToken: false);

        if (!TryValidateRequiredInputs(out string? validationError))
        {
            StatusMessage = validationError;
            return;
        }

        if (!TryGetValidatedRedirectUri(out Uri redirectUri, out string? redirectUriError))
        {
            StatusMessage = redirectUriError;
            return;
        }

        DropboxUploader uploader = BuildUploader();
        _uploader = uploader;
        string url = uploader.GetAuthorizationURL(redirectUri.AbsoluteUri);

        try
        {
            OpenUrl(url);
            StatusMessage = "Browser opened. Complete Dropbox login, then paste the returned code below.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Failed to open browser: " + ex.Message;
        }
    }

    [RelayCommand]
    private void CompleteLogin()
    {
        EnsureConfigFromFields(resetToken: false);

        if (string.IsNullOrWhiteSpace(AuthorizationCode))
        {
            StatusMessage = "Please enter the code from the Dropbox callback URL.";
            return;
        }

        if (!TryGetValidatedRedirectUri(out Uri redirectUri, out string? redirectUriError))
        {
            StatusMessage = redirectUriError;
            return;
        }

        DropboxUploader uploader = BuildUploader();
        _uploader = uploader;
        string redirectUriToUse = string.IsNullOrWhiteSpace(_pendingRedirectUri) ? redirectUri.AbsoluteUri : _pendingRedirectUri!;

        if (uploader.GetAccessToken(AuthorizationCode, redirectUriToUse, _pendingCodeVerifier))
        {
            PersistToken(uploader.AuthInfo.Token);
            IsLoggedIn = true;
            AuthorizationCode = string.Empty;
            _pendingCodeVerifier = null;
            _pendingRedirectUri = null;
            StatusMessage = "Dropbox login completed.";
            LoadAccountSummary();
        }
        else
        {
            StatusMessage = "Dropbox login failed. Please verify the code and credentials.";
        }
    }

    [RelayCommand]
    private void ClearLogin()
    {
        _browserLoginCts?.Cancel();
        _browserLoginCts?.Dispose();
        _browserLoginCts = null;
        _secrets?.DeleteSecret("dropbox", _secretKey, "oauthToken");
        _pendingCodeVerifier = null;
        _pendingRedirectUri = null;
        IsLoginInProgress = false;
        IsLoggedIn = false;
        AccountSummary = string.Empty;
        StatusMessage = "Stored Dropbox login token has been cleared.";
    }

    [RelayCommand]
    private void RefreshAccountInfo()
    {
        EnsureConfigFromFields(resetToken: false);
        _uploader ??= BuildUploader();

        if (!_uploader.CheckAuthorization())
        {
            StatusMessage = "Dropbox login is required.";
            return;
        }

        LoadAccountSummary();
    }

    public void LoadFromJson(string json)
    {
        try
        {
            DropboxConfigModel? config = JsonConvert.DeserializeObject<DropboxConfigModel>(json);
            if (config == null)
            {
                return;
            }

            _config = config;
            _secretKey = string.IsNullOrWhiteSpace(config.SecretKey) ? Guid.NewGuid().ToString("N") : config.SecretKey;

            UploadPath = _config.UploadPath;
            AutoCreateShareableLink = _config.AutoCreateShareableLink;
            UseDirectLink = _config.UseDirectLink;
            RedirectUri = string.IsNullOrWhiteSpace(_config.RedirectUri) ? DropboxConfigModel.DefaultRedirectUri : _config.RedirectUri;

            ClientId = _secrets?.GetSecret("dropbox", _secretKey, "clientId") ?? string.Empty;
            ClientSecret = _secrets?.GetSecret("dropbox", _secretKey, "clientSecret") ?? string.Empty;

            IsLoggedIn = HasToken();
            AccountSummary = IsLoggedIn ? "Token saved." : string.Empty;
        }
        catch
        {
            StatusMessage = "Failed to load Dropbox configuration.";
        }
    }

    public string ToJson()
    {
        EnsureConfigFromFields(resetToken: false);
        PersistCredentials();
        return JsonConvert.SerializeObject(_config, Formatting.Indented);
    }

    public bool Validate()
    {
        if (!TryValidateRequiredInputs(out string? validationError))
        {
            StatusMessage = validationError;
            return false;
        }

        if (string.IsNullOrWhiteSpace(UploadPath))
        {
            StatusMessage = "Upload path is required.";
            return false;
        }

        if (!IsLoggedIn)
        {
            StatusMessage = "You must login to Dropbox.";
            return false;
        }

        if (!TryGetValidatedRedirectUri(out _, out string? redirectUriError))
        {
            StatusMessage = redirectUriError;
            return false;
        }

        PersistCredentials();
        StatusMessage = null;
        return true;
    }

    public void SetContext(IProviderContext context)
    {
        _secrets = context.Secrets;
    }

    private void EnsureConfigFromFields(bool resetToken)
    {
        _config.SecretKey = _secretKey;
        _config.UploadPath = UploadPath;
        _config.AutoCreateShareableLink = AutoCreateShareableLink;
        _config.UseDirectLink = UseDirectLink;
        _config.RedirectUri = RedirectUri;

        if (resetToken)
        {
            _secrets?.DeleteSecret("dropbox", _secretKey, "oauthToken");
            _pendingCodeVerifier = null;
            _pendingRedirectUri = null;
            IsLoggedIn = false;
            AccountSummary = string.Empty;
        }

        _uploader = null;
    }

    private DropboxUploader BuildUploader()
    {
        OAuth2Info authInfo = new(ClientId ?? string.Empty, ClientSecret ?? string.Empty);
        string? tokenJson = _secrets?.GetSecret("dropbox", _secretKey, "oauthToken");
        if (!string.IsNullOrWhiteSpace(tokenJson))
        {
            OAuth2Token? token = JsonConvert.DeserializeObject<OAuth2Token>(tokenJson);
            if (token != null)
            {
                authInfo.Token = token;
            }
        }

        return new DropboxUploader(_config, authInfo, PersistToken);
    }

    private void PersistCredentials()
    {
        if (_secrets == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(ClientId))
        {
            _secrets.DeleteSecret("dropbox", _secretKey, "clientId");
        }
        else
        {
            _secrets.SetSecret("dropbox", _secretKey, "clientId", ClientId);
        }

        if (string.IsNullOrWhiteSpace(ClientSecret))
        {
            _secrets.DeleteSecret("dropbox", _secretKey, "clientSecret");
        }
        else
        {
            _secrets.SetSecret("dropbox", _secretKey, "clientSecret", ClientSecret);
        }
    }

    private void PersistToken(OAuth2Token? token = null)
    {
        OAuth2Token? targetToken = token ?? _uploader?.AuthInfo.Token;
        if (_secrets == null || targetToken == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(targetToken.access_token))
        {
            return;
        }

        string tokenJson = JsonConvert.SerializeObject(targetToken, Formatting.None);
        _secrets.SetSecret("dropbox", _secretKey, "oauthToken", tokenJson);
    }

    private bool HasToken()
    {
        if (_secrets == null)
        {
            return false;
        }

        string? tokenJson = _secrets.GetSecret("dropbox", _secretKey, "oauthToken");
        if (string.IsNullOrWhiteSpace(tokenJson))
        {
            return false;
        }

        OAuth2Token? token = JsonConvert.DeserializeObject<OAuth2Token>(tokenJson);
        return token != null && !string.IsNullOrWhiteSpace(token.access_token);
    }

    private void LoadAccountSummary()
    {
        if (_uploader == null)
        {
            return;
        }

        DropboxAccount? account = _uploader.GetCurrentAccount();
        if (account == null)
        {
            AccountSummary = "Account details are unavailable.";
            return;
        }

        string displayName = account.Name?.DisplayName ?? "Unknown";
        string email = account.Email ?? "unknown email";
        AccountSummary = $"{displayName} ({email})";
    }

    private bool TryValidateRequiredInputs(out string? error)
    {
        if (string.IsNullOrWhiteSpace(ClientId))
        {
            error = "Client ID is required.";
            return false;
        }

        error = null;
        return true;
    }

    private bool TryGetValidatedRedirectUri(out Uri uri, out string? error)
    {
        uri = null!;
        error = null;

        if (string.IsNullOrWhiteSpace(RedirectUri))
        {
            error = "Redirect URI is required.";
            return false;
        }

        if (!Uri.TryCreate(RedirectUri.Trim(), UriKind.Absolute, out Uri? parsedUri))
        {
            error = "Redirect URI is not a valid absolute URI.";
            return false;
        }

        if (!parsedUri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            error = "Redirect URI must use http:// for local loopback login.";
            return false;
        }

        bool isLocalhost = parsedUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
        bool isLoopbackIp = IPAddress.TryParse(parsedUri.Host, out IPAddress? ipAddress) && IPAddress.IsLoopback(ipAddress);
        if (!isLocalhost && !isLoopbackIp)
        {
            error = "Redirect URI host must be localhost or a loopback IP address.";
            return false;
        }

        if (parsedUri.IsDefaultPort || parsedUri.Port < 1024)
        {
            error = "Redirect URI must include a non-privileged local port (for example: 127.0.0.1:52475).";
            return false;
        }

        UriBuilder builder = new(parsedUri)
        {
            Query = string.Empty,
            Fragment = string.Empty
        };

        uri = builder.Uri;
        return true;
    }

    private static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        else
        {
            URLHelpers.OpenURL(url);
        }
    }
}
