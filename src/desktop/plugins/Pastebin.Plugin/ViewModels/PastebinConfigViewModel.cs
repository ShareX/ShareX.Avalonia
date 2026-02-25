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

namespace ShareX.Pastebin.Plugin.ViewModels;

public partial class PastebinConfigViewModel : ObservableObject, IUploaderConfigViewModel
{
    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private PastebinPrivacy _exposure = PastebinPrivacy.Unlisted;

    [ObservableProperty]
    private PastebinExpiration _expiration = PastebinExpiration.N;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _textFormat = "text";

    [ObservableProperty]
    private bool _rawURL;

    [ObservableProperty]
    private string? _statusMessage;

    public IReadOnlyList<PastebinPrivacy> ExposureOptions { get; } = new[] { PastebinPrivacy.Public, PastebinPrivacy.Unlisted, PastebinPrivacy.Private };
    public IReadOnlyList<PastebinExpiration> ExpirationOptions { get; } = new[] { PastebinExpiration.N, PastebinExpiration.M10, PastebinExpiration.H1, PastebinExpiration.D1, PastebinExpiration.W1, PastebinExpiration.W2, PastebinExpiration.M1 };

    public void LoadFromJson(string json)
    {
        try
        {
            var config = JsonConvert.DeserializeObject<PastebinConfigModel>(json);
            if (config != null)
            {
                ApiKey = config.ApiKey ?? string.Empty;
                Username = config.Username ?? string.Empty;
                Password = config.Password ?? string.Empty;
                Exposure = config.Exposure;
                Expiration = config.Expiration;
                Title = config.Title ?? string.Empty;
                TextFormat = string.IsNullOrWhiteSpace(config.TextFormat) ? "text" : config.TextFormat;
                RawURL = config.RawURL;
            }
        }
        catch
        {
            StatusMessage = "Failed to load configuration";
        }
    }

    public string ToJson()
    {
        var config = new PastebinConfigModel
        {
            ApiKey = ApiKey ?? string.Empty,
            Username = Username ?? string.Empty,
            Password = Password ?? string.Empty,
            Exposure = Exposure,
            Expiration = Expiration,
            Title = Title ?? string.Empty,
            TextFormat = string.IsNullOrWhiteSpace(TextFormat) ? "text" : TextFormat,
            RawURL = RawURL
        };

        return JsonConvert.SerializeObject(config, Formatting.Indented);
    }

    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            StatusMessage = "API key is required. Get one at https://pastebin.com/doc_api";
            return false;
        }

        StatusMessage = null;
        return true;
    }
}
