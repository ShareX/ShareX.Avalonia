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
using Newtonsoft.Json.Linq;
using XerahS.Uploaders;
using XerahS.Uploaders.PluginSystem;

namespace ShareX.Pixelfox.Plugin;

public sealed class PixelfoxProvider : UploaderProviderBase, IInstanceSecretMigrator
{
    public override string ProviderId => "pixelfox";
    public override string Name => "Pixelfox";
    public override string Description => "Upload images to Pixelfox using API-key authenticated direct upload sessions";
    public override Version Version => new(1, 0, 0);
    public override UploaderCategory[] SupportedCategories => new[] { UploaderCategory.Image };
    public override Type ConfigModelType => typeof(PixelfoxConfigModel);

    public override Uploader CreateInstance(string settingsJson)
    {
        PixelfoxConfigModel config = DeserializeConfig(settingsJson);
        string apiKey = ResolveApiKey(config.SecretKey);
        return new PixelfoxUploader(config, apiKey);
    }

    public override bool ValidateSettings(string settingsJson)
    {
        PixelfoxConfigModel config = DeserializeConfig(settingsJson);

        if (string.IsNullOrWhiteSpace(PixelfoxUploader.NormalizeServerUrl(config.ServerUrl)))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(ResolveApiKey(config.SecretKey)))
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(config.AlbumId) || long.TryParse(config.AlbumId, out long albumId) && albumId > 0;
    }

    public override Dictionary<UploaderCategory, string[]> GetSupportedFileTypes()
    {
        string[] imageTypes =
        {
            "png", "jpg", "jpeg", "gif", "bmp", "tiff", "webp", "avif", "svg", "heic", "heif", "jxl"
        };

        return new Dictionary<UploaderCategory, string[]>
        {
            { UploaderCategory.Image, imageTypes }
        };
    }

    public override object? CreateConfigView()
    {
        return new Views.PixelfoxConfigView();
    }

    public override IUploaderConfigViewModel? CreateConfigViewModel()
    {
        return new ViewModels.PixelfoxConfigViewModel();
    }

    public bool TryMigrateSecrets(string settingsJson, ISecretStore secrets, out string updatedSettingsJson, out int migratedSecretCount)
    {
        updatedSettingsJson = settingsJson;
        migratedSecretCount = 0;

        JObject? json;
        try
        {
            json = JObject.Parse(settingsJson);
        }
        catch
        {
            return false;
        }

        string secretKey = json.Value<string>("SecretKey") ?? Guid.NewGuid().ToString("N");
        bool changed = false;

        if (!string.Equals(json.Value<string>("SecretKey"), secretKey, StringComparison.Ordinal))
        {
            json["SecretKey"] = secretKey;
            changed = true;
        }

        string? apiKey = json.Value<string>("ApiKey");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            secrets.SetSecret(ProviderId, secretKey, "apiKey", apiKey);
            json.Remove("ApiKey");
            migratedSecretCount = 1;
            changed = true;
        }

        if (changed)
        {
            updatedSettingsJson = json.ToString(Formatting.Indented);
        }

        return changed;
    }

    private PixelfoxConfigModel DeserializeConfig(string settingsJson)
    {
        PixelfoxConfigModel? config = JsonConvert.DeserializeObject<PixelfoxConfigModel>(settingsJson);
        return config ?? new PixelfoxConfigModel();
    }

    private string ResolveApiKey(string secretKey)
    {
        if (Secrets == null || string.IsNullOrWhiteSpace(secretKey))
        {
            return string.Empty;
        }

        return Secrets.GetSecret(ProviderId, secretKey, "apiKey") ?? string.Empty;
    }
}
