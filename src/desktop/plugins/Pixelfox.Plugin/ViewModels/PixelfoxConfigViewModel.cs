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
using Newtonsoft.Json;
using XerahS.Uploaders.PluginSystem;

namespace ShareX.Pixelfox.Plugin.ViewModels;

public partial class PixelfoxConfigViewModel : ObservableObject, IUploaderConfigViewModel, IProviderContextAware
{
    [ObservableProperty]
    private string _serverUrl = "https://pixelfox.cc";

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private string _albumId = string.Empty;

    [ObservableProperty]
    private bool _isNsfw;

    [ObservableProperty]
    private int _processingProfileIndex;

    [ObservableProperty]
    private string? _statusMessage;

    public string[] ProcessingProfiles { get; } =
    {
        "default",
        "original_only"
    };

    private string _secretKey = Guid.NewGuid().ToString("N");
    private ISecretStore? _secrets;

    public void LoadFromJson(string json)
    {
        try
        {
            PixelfoxConfigModel? config = JsonConvert.DeserializeObject<PixelfoxConfigModel>(json);
            if (config == null)
            {
                return;
            }

            _secretKey = string.IsNullOrWhiteSpace(config.SecretKey) ? Guid.NewGuid().ToString("N") : config.SecretKey;
            ServerUrl = string.IsNullOrWhiteSpace(config.ServerUrl) ? "https://pixelfox.cc" : config.ServerUrl;
            AlbumId = config.AlbumId ?? string.Empty;
            IsNsfw = config.IsNsfw;
            ProcessingProfileIndex = config.ProcessingProfile == PixelfoxProcessingProfile.OriginalOnly ? 1 : 0;
            LoadSecretsFromStore();
        }
        catch
        {
            StatusMessage = "Failed to load Pixelfox configuration.";
        }
    }

    public string ToJson()
    {
        if (!string.IsNullOrWhiteSpace(PixelfoxUploader.NormalizeServerUrl(ServerUrl)))
        {
            ServerUrl = PixelfoxUploader.NormalizeServerUrl(ServerUrl);
        }

        PersistSecrets();

        PixelfoxConfigModel config = new()
        {
            SecretKey = _secretKey,
            ServerUrl = ServerUrl,
            AlbumId = AlbumId?.Trim() ?? string.Empty,
            IsNsfw = IsNsfw,
            ProcessingProfile = ProcessingProfileIndex == 1
                ? PixelfoxProcessingProfile.OriginalOnly
                : PixelfoxProcessingProfile.Default
        };

        return JsonConvert.SerializeObject(config, Formatting.Indented);
    }

    public bool Validate()
    {
        string normalizedServerUrl = PixelfoxUploader.NormalizeServerUrl(ServerUrl);
        if (string.IsNullOrWhiteSpace(normalizedServerUrl))
        {
            StatusMessage = "Pixelfox server URL must be a valid http:// or https:// URL.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            StatusMessage = "Pixelfox API key is required.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(AlbumId) && (!long.TryParse(AlbumId.Trim(), out long parsedAlbumId) || parsedAlbumId <= 0))
        {
            StatusMessage = "Album ID must be a positive integer.";
            return false;
        }

        ServerUrl = normalizedServerUrl;
        PersistSecrets();
        StatusMessage = null;
        return true;
    }

    public void SetContext(IProviderContext context)
    {
        _secrets = context.Secrets;
        LoadSecretsFromStore();
    }

    private void LoadSecretsFromStore()
    {
        if (_secrets == null)
        {
            return;
        }

        ApiKey = _secrets.GetSecret("pixelfox", _secretKey, "apiKey") ?? string.Empty;
    }

    private void PersistSecrets()
    {
        if (_secrets == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            _secrets.DeleteSecret("pixelfox", _secretKey, "apiKey");
        }
        else
        {
            _secrets.SetSecret("pixelfox", _secretKey, "apiKey", ApiKey.Trim());
        }
    }
}
