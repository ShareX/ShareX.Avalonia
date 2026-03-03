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
using XerahS.Common;
using XerahS.Uploaders.PluginSystem;

namespace XerahS.Uploaders.LegacySupport;

/// <summary>
/// Result of a built-in provider migration pass.
/// </summary>
public class BuiltinMigrationResult
{
    public List<string> InstancesCreated { get; } = new();
    public List<string> InstancesUpdated { get; } = new();
    public List<string> PartialMigrations { get; } = new();
    public List<string> SkippedProviders { get; } = new();

    public int TotalCreated => InstancesCreated.Count;
    public int TotalUpdated => InstancesUpdated.Count;
    public int TotalPartial => PartialMigrations.Count;
    public int TotalSkipped => SkippedProviders.Count;

    public bool HasAnything => TotalCreated + TotalUpdated + TotalPartial + TotalSkipped > 0;

    public string GetSummary()
    {
        var sb = new System.Text.StringBuilder();

        if (InstancesCreated.Count > 0)
        {
            sb.AppendLine($"Created {InstancesCreated.Count} built-in instance(s):");
            foreach (var item in InstancesCreated)
                sb.AppendLine($"  + {item}");
        }

        if (InstancesUpdated.Count > 0)
        {
            sb.AppendLine($"Updated {InstancesUpdated.Count} existing instance(s):");
            foreach (var item in InstancesUpdated)
                sb.AppendLine($"  ~ {item}");
        }

        if (PartialMigrations.Count > 0)
        {
            sb.AppendLine($"Partially migrated {PartialMigrations.Count} provider(s) (re-auth required):");
            foreach (var item in PartialMigrations)
                sb.AppendLine($"  ! {item}");
        }

        if (SkippedProviders.Count > 0)
        {
            sb.AppendLine($"Skipped {SkippedProviders.Count} provider(s) (no XerahS plugin available):");
            foreach (var item in SkippedProviders)
                sb.AppendLine($"  - {item}");
        }

        return sb.ToString().TrimEnd();
    }
}

/// <summary>
/// Migrates built-in ShareX uploader settings to XerahS UploaderInstances.
/// Lives in XerahS.Uploaders (core) and uses JObject to build SettingsJson without
/// directly referencing plugin assemblies.
/// </summary>
public static class BuiltinInstanceMigrator
{
    private const string LogPrefix = "[BuiltinInstanceMigrator]";

    /// <summary>
    /// Migrates all supported built-in providers from the given legacy config.
    /// </summary>
    public static BuiltinMigrationResult Migrate(UploadersConfig source)
    {
        var result = new BuiltinMigrationResult();

        MigrateAmazonS3(source, result);
        MigrateFtp(source, result);
        MigratePastebin(source, result);
        MigrateImgur(source, result);
        CollectSkippedProviders(source, result);

        return result;
    }

    // ── Amazon S3 (full migration) ────────────────────────────────────────────

    private static void MigrateAmazonS3(UploadersConfig source, BuiltinMigrationResult result)
    {
        var s3 = source.AmazonS3Settings;
        if (s3 == null || string.IsNullOrEmpty(s3.AccessKeyID))
            return;

        const string providerId = "amazons3";
        var secrets = ProviderCatalog.GetProviderContext()?.Secrets;
        if (secrets == null)
        {
            DebugHelper.WriteLine($"{LogPrefix} S3: secret store unavailable — skipping credential migration.");
            return;
        }

        // Reuse the SecretKey from an existing instance to avoid orphaning stored credentials.
        var existingInstances = InstanceManager.Instance.GetInstancesByCategory(UploaderCategory.File)
            .Where(i => i.ProviderId == providerId)
            .ToList();

        string secretKey = existingInstances.Count > 0
            ? ExtractSecretKey(existingInstances[0].SettingsJson) ?? Guid.NewGuid().ToString("N")
            : Guid.NewGuid().ToString("N");

        // Persist credentials into the secret store.
        try
        {
            secrets.SetSecret(providerId, secretKey, "accessKeyId", s3.AccessKeyID);
            secrets.SetSecret(providerId, secretKey, "secretAccessKey", s3.SecretAccessKey);
        }
        catch (Exception ex)
        {
            DebugHelper.WriteException(ex, $"{LogPrefix} S3: failed to store credentials");
            return;
        }

        // Build SettingsJson matching S3ConfigModel property names (PascalCase, Newtonsoft defaults).
        var json = new JObject
        {
            ["AuthMode"] = 0,                                     // S3AuthMode.AccessKeys
            ["SecretKey"] = secretKey,
            ["BucketName"] = s3.Bucket,
            ["Region"] = s3.Region,
            ["ObjectPrefix"] = s3.ObjectPrefix,
            ["UseCustomCNAME"] = s3.UseCustomCNAME,
            ["CustomDomain"] = s3.CustomDomain,
            ["StorageClass"] = (int)s3.StorageClass,              // enum values are identical
            ["SetPublicACL"] = s3.SetPublicACL,
            ["SetPublicPolicy"] = false,
            ["UsePathStyleUrl"] = s3.UsePathStyle,
            ["SignedPayload"] = s3.SignedPayload,
            ["Endpoint"] = s3.Endpoint,
            ["RemoveExtensionImage"] = s3.RemoveExtensionImage,
            ["RemoveExtensionVideo"] = s3.RemoveExtensionVideo,
            ["RemoveExtensionText"] = s3.RemoveExtensionText
        };

        string settingsJson = json.ToString(Formatting.Indented);

        // S3 supports Image, Text, and File categories.
        foreach (var category in new[] { UploaderCategory.Image, UploaderCategory.Text, UploaderCategory.File })
        {
            var existing = InstanceManager.Instance.GetInstancesByCategory(category)
                .FirstOrDefault(i => i.ProviderId == providerId);

            string displayName = $"Amazon S3 ({s3.Bucket})";

            if (existing != null)
            {
                existing.SettingsJson = settingsJson;
                existing.DisplayName = displayName;
                InstanceManager.Instance.UpdateInstance(existing);
                result.InstancesUpdated.Add($"Amazon S3 [{category}]");
            }
            else
            {
                InstanceManager.Instance.AddInstance(new UploaderInstance
                {
                    ProviderId = providerId,
                    Category = category,
                    DisplayName = displayName,
                    SettingsJson = settingsJson,
                    FileTypeRouting = new FileTypeScope { AllFileTypes = true }
                });
                result.InstancesCreated.Add($"Amazon S3 [{category}]");
            }
        }

        DebugHelper.WriteLine($"{LogPrefix} S3: migrated bucket '{s3.Bucket}' (secretKey={secretKey[..8]}...)");
    }

    // ── FTP / FTPS / SFTP (full migration) ───────────────────────────────────

    private static void MigrateFtp(UploadersConfig source, BuiltinMigrationResult result)
    {
        if (source.FTPAccountList == null || source.FTPAccountList.Count == 0)
            return;

        const string providerId = "ftp";

        foreach (var account in source.FTPAccountList)
        {
            if (account == null || string.IsNullOrEmpty(account.Host))
                continue;

            // Build SettingsJson matching FtpConfigModel property names.
            var json = new JObject
            {
                ["AccountName"] = account.Name,
                ["Protocol"] = (int)account.Protocol,
                ["Host"] = account.Host,
                ["Port"] = account.Port,
                ["Username"] = account.Username,
                ["Password"] = account.Password,
                ["IsActive"] = account.IsActive,
                ["SubFolderPath"] = account.SubFolderPath,
                ["BrowserProtocol"] = (int)account.BrowserProtocol,
                ["HttpHomePath"] = account.HttpHomePath,
                ["HttpHomePathAutoAddSubFolderPath"] = account.HttpHomePathAutoAddSubFolderPath,
                ["HttpHomePathNoExtension"] = account.HttpHomePathNoExtension,
                ["FTPSEncryption"] = (int)account.FTPSEncryption,
                ["Keypath"] = account.Keypath,
                ["Passphrase"] = account.Passphrase
            };

            string settingsJson = json.ToString(Formatting.Indented);
            string displayName = string.IsNullOrWhiteSpace(account.Name)
                ? $"{account.Host}:{account.Port}"
                : account.Name;

            // FTP supports File, Image, and Text categories.
            foreach (var category in new[] { UploaderCategory.File, UploaderCategory.Image, UploaderCategory.Text })
            {
                var existing = InstanceManager.Instance.GetInstancesByCategory(category)
                    .FirstOrDefault(i => i.ProviderId == providerId && i.DisplayName == displayName);

                if (existing != null)
                {
                    existing.SettingsJson = settingsJson;
                    InstanceManager.Instance.UpdateInstance(existing);
                    result.InstancesUpdated.Add($"FTP '{displayName}' [{category}]");
                }
                else
                {
                    InstanceManager.Instance.AddInstance(new UploaderInstance
                    {
                        ProviderId = providerId,
                        Category = category,
                        DisplayName = displayName,
                        SettingsJson = settingsJson,
                        FileTypeRouting = new FileTypeScope { AllFileTypes = true }
                    });
                    result.InstancesCreated.Add($"FTP '{displayName}' [{category}]");
                }
            }

            DebugHelper.WriteLine($"{LogPrefix} FTP: migrated account '{account.Name}' ({account.Host}:{account.Port})");
        }
    }

    // ── Pastebin (partial — UserKey migrated; ApiKey not available) ───────────

    private static void MigratePastebin(UploadersConfig source, BuiltinMigrationResult result)
    {
        var settings = source.PastebinSettings;
        if (settings == null || string.IsNullOrEmpty(settings.UserKey))
            return;

        const string providerId = "pastebin";

        // Build SettingsJson matching PastebinConfigModel property names.
        // ApiKey (developer key) is NOT in legacy config — user will need to enter it.
        var json = new JObject
        {
            ["ApiKey"] = string.Empty,
            ["Username"] = settings.Username,
            ["Password"] = settings.Password,
            ["Exposure"] = (int)settings.Exposure,
            ["Expiration"] = (int)settings.Expiration,
            ["Title"] = settings.Title,
            ["TextFormat"] = settings.TextFormat,
            ["UserKey"] = settings.UserKey,
            ["RawURL"] = settings.RawURL
        };

        string settingsJson = json.ToString(Formatting.Indented);
        string displayName = string.IsNullOrEmpty(settings.Username) ? "Pastebin" : $"Pastebin ({settings.Username})";

        var existing = InstanceManager.Instance.GetInstancesByCategory(UploaderCategory.Text)
            .FirstOrDefault(i => i.ProviderId == providerId);

        if (existing != null)
        {
            existing.SettingsJson = settingsJson;
            existing.DisplayName = displayName;
            InstanceManager.Instance.UpdateInstance(existing);
            result.InstancesUpdated.Add("Pastebin [Text]");
        }
        else
        {
            InstanceManager.Instance.AddInstance(new UploaderInstance
            {
                ProviderId = providerId,
                Category = UploaderCategory.Text,
                DisplayName = displayName,
                SettingsJson = settingsJson,
                FileTypeRouting = new FileTypeScope { AllFileTypes = true }
            });
            result.InstancesCreated.Add("Pastebin [Text]");
        }

        result.PartialMigrations.Add("Pastebin: preferences migrated — enter your developer API key in settings to enable uploads.");
        DebugHelper.WriteLine($"{LogPrefix} Pastebin: partial migration (UserKey present, ApiKey not available).");
    }

    // ── Imgur (partial — preferences migrated; OAuth re-auth required) ────────

    private static void MigrateImgur(UploadersConfig source, BuiltinMigrationResult result)
    {
        // Only migrate if user had a non-anonymous account or configured preferences.
        if (source.ImgurOAuth2Info == null && source.ImgurAccountType == AccountType.Anonymous)
            return;

        const string providerId = "imgur";

        // Build SettingsJson matching ImgurConfigModel property names.
        // OAuth tokens cannot be migrated — a new SecretKey GUID is generated.
        string secretKey = Guid.NewGuid().ToString("N");
        var json = new JObject
        {
            ["AccountType"] = (int)source.ImgurAccountType,
            ["SecretKey"] = secretKey,
            ["DirectLink"] = source.ImgurDirectLink,
            ["ThumbnailType"] = (int)source.ImgurThumbnailType,
            ["UseGIFV"] = source.ImgurUseGIFV,
            ["UploadToSelectedAlbum"] = source.ImgurUploadSelectedAlbum,
            ["SelectedAlbum"] = null,
            ["ClientId"] = "30d41ft9z9r8jtt"        // Default ShareX client ID
        };

        string settingsJson = json.ToString(Formatting.Indented);
        string displayName = "Imgur";

        var existing = InstanceManager.Instance.GetInstancesByCategory(UploaderCategory.Image)
            .FirstOrDefault(i => i.ProviderId == providerId);

        if (existing != null)
        {
            // Preserve the existing SecretKey to avoid orphaning stored OAuth tokens.
            string? existingSecretKey = ExtractSecretKey(existing.SettingsJson);
            if (!string.IsNullOrEmpty(existingSecretKey))
            {
                json["SecretKey"] = existingSecretKey;
                settingsJson = json.ToString(Formatting.Indented);
            }

            existing.SettingsJson = settingsJson;
            InstanceManager.Instance.UpdateInstance(existing);
            result.InstancesUpdated.Add("Imgur [Image]");
        }
        else
        {
            InstanceManager.Instance.AddInstance(new UploaderInstance
            {
                ProviderId = providerId,
                Category = UploaderCategory.Image,
                DisplayName = displayName,
                SettingsJson = settingsJson,
                FileTypeRouting = new FileTypeScope { AllFileTypes = true }
            });
            result.InstancesCreated.Add("Imgur [Image]");
        }

        result.PartialMigrations.Add("Imgur: preferences migrated — re-authorize your account in settings to enable authenticated uploads.");
        DebugHelper.WriteLine($"{LogPrefix} Imgur: partial migration (preferences copied, OAuth re-auth needed).");
    }

    // ── Skipped providers (no XerahS plugin available) ────────────────────────

    private static void CollectSkippedProviders(UploadersConfig source, BuiltinMigrationResult result)
    {
        if (!string.IsNullOrEmpty(source.ImageShackSettings?.Auth_token))
            result.SkippedProviders.Add("ImageShack");

        if (source.FlickrOAuthInfo != null)
            result.SkippedProviders.Add("Flickr");

        if (source.PhotobucketOAuthInfo != null)
            result.SkippedProviders.Add("Photobucket");

        if (!string.IsNullOrEmpty(source.VgymeUserKey))
            result.SkippedProviders.Add("vgy.me");

        if (source.GistOAuth2Info != null)
            result.SkippedProviders.Add("GitHub Gist");

        if (!string.IsNullOrEmpty(source.Paste_eeUserKey))
            result.SkippedProviders.Add("Paste.ee");

        // HastebinCustomDomain defaults to "https://hastebin.com" so only report if customized.
        if (!string.IsNullOrEmpty(source.HastebinCustomDomain) &&
            source.HastebinCustomDomain != "https://hastebin.com")
            result.SkippedProviders.Add("Hastebin (custom domain)");

        if (!string.IsNullOrEmpty(source.OneTimeSecretAPIKey))
            result.SkippedProviders.Add("OneTimeSecret");

        if (source.DropboxOAuth2Info != null)
            result.SkippedProviders.Add("Dropbox");

        if (source.OneDriveV2OAuth2Info != null)
            result.SkippedProviders.Add("OneDrive");

        if (source.GoogleDriveOAuth2Info != null)
            result.SkippedProviders.Add("Google Drive");

        if (!string.IsNullOrEmpty(source.AzureStorageAccountName))
            result.SkippedProviders.Add("Azure Storage");

        if (!string.IsNullOrEmpty(source.B2ApplicationKeyId))
            result.SkippedProviders.Add("Backblaze B2");

        if (source.BitlyOAuth2Info != null)
            result.SkippedProviders.Add("bit.ly");

        if (!string.IsNullOrEmpty(source.YourlsAPIURL))
            result.SkippedProviders.Add("YOURLS");

        if (!string.IsNullOrEmpty(source.PolrAPIHostname))
            result.SkippedProviders.Add("Polr");

        if (!string.IsNullOrEmpty(source.FirebaseWebAPIKey))
            result.SkippedProviders.Add("Firebase Dynamic Links");

        if (source.KuttSettings != null && !string.IsNullOrEmpty(source.KuttSettings.APIKey))
            result.SkippedProviders.Add("Kutt");

        if (!string.IsNullOrEmpty(source.UpasteUserKey))
            result.SkippedProviders.Add("uPaste");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? ExtractSecretKey(string? settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
            return null;
        try
        {
            var token = JObject.Parse(settingsJson)["SecretKey"];
            string? val = token?.Value<string>();
            return string.IsNullOrWhiteSpace(val) ? null : val;
        }
        catch
        {
            return null;
        }
    }
}
