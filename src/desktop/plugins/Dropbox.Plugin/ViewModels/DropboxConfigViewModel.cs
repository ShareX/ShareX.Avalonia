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
using XerahS.Uploaders;
using XerahS.Uploaders.PluginSystem;
using System.Diagnostics;
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
    private string _authorizationCode = string.Empty;

    [ObservableProperty]
    private bool _isLoggedIn;

    [ObservableProperty]
    private string? _accountSummary;

    [ObservableProperty]
    private string? _statusMessage;

    private string _secretKey = Guid.NewGuid().ToString("N");
    private ISecretStore? _secrets;
    private DropboxConfigModel _config = new();
    private DropboxUploader? _uploader;

    [RelayCommand]
    private void OpenLoginUrl()
    {
        EnsureConfigFromFields(resetToken: false);

        if (string.IsNullOrWhiteSpace(ClientId))
        {
            StatusMessage = "Client ID is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(ClientSecret))
        {
            StatusMessage = "Client Secret is required.";
            return;
        }

        _uploader = BuildUploader();
        string url = _uploader.GetAuthorizationURL();

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
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

        _uploader ??= BuildUploader();

        if (_uploader.GetAccessToken(AuthorizationCode))
        {
            PersistToken();
            IsLoggedIn = true;
            AuthorizationCode = string.Empty;
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
        _secrets?.DeleteSecret("dropbox", _secretKey, "oauthToken");
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
        if (string.IsNullOrWhiteSpace(ClientId))
        {
            StatusMessage = "Client ID is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ClientSecret))
        {
            StatusMessage = "Client Secret is required.";
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

        if (resetToken)
        {
            _secrets?.DeleteSecret("dropbox", _secretKey, "oauthToken");
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

        return new DropboxUploader(_config, authInfo);
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

    private void PersistToken()
    {
        if (_secrets == null || _uploader?.AuthInfo.Token == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_uploader.AuthInfo.Token.access_token))
        {
            return;
        }

        string tokenJson = JsonConvert.SerializeObject(_uploader.AuthInfo.Token, Formatting.None);
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
}

