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
using XerahS.Uploaders;
using XerahS.Uploaders.FileUploaders;
using XerahS.Uploaders.PluginSystem;

namespace ShareX.Ftp.Plugin;

/// <summary>
/// FTP / FTPS / SFTP file uploader provider. Supports File, Image, and Text categories.
/// Uses FluentFTP (FTP/FTPS) and SSH.NET (SFTP) for robust, modern transfers.
/// </summary>
public class FtpProvider : UploaderProviderBase
{
    public override string ProviderId => "ftp";
    public override string Name => "FTP / FTPS / SFTP";
    public override string Description => "Upload files via FTP, FTPS (FTP over SSL/TLS), or SFTP (SSH)";
    public override Version Version => new Version(1, 0, 0);
    public override UploaderCategory[] SupportedCategories => new[] { UploaderCategory.File, UploaderCategory.Image, UploaderCategory.Text };
    public override Type ConfigModelType => typeof(FtpConfigModel);

    public override Uploader CreateInstance(string settingsJson)
    {
        var config = JsonConvert.DeserializeObject<FtpConfigModel>(settingsJson);
        if (config == null)
            throw new InvalidOperationException("Failed to deserialize FTP settings");

        FTPAccount account = ToFtpAccount(config);
        return new FtpUploader(account);
    }

    public override Dictionary<UploaderCategory, string[]> GetSupportedFileTypes()
    {
        return new Dictionary<UploaderCategory, string[]>
        {
            { UploaderCategory.File, new[] { "*" } },
            { UploaderCategory.Image, new[] { "png", "jpg", "jpeg", "gif", "bmp", "webp", "ico" } },
            { UploaderCategory.Text, new[] { "txt", "log", "json", "xml", "md", "html", "css", "js", "cs" } }
        };
    }

    public override object? CreateConfigView()
    {
        return new Views.FtpConfigView();
    }

    public override IUploaderConfigViewModel? CreateConfigViewModel()
    {
        return new ViewModels.FtpConfigViewModel();
    }

    private static FTPAccount ToFtpAccount(FtpConfigModel c)
    {
        return new FTPAccount
        {
            Name = c.AccountName ?? "FTP Account",
            Protocol = c.Protocol,
            Host = c.Host ?? "",
            Port = c.Port,
            Username = c.Username ?? "",
            Password = c.Password ?? "",
            IsActive = c.IsActive,
            SubFolderPath = c.SubFolderPath ?? "",
            BrowserProtocol = c.BrowserProtocol,
            HttpHomePath = c.HttpHomePath ?? "",
            HttpHomePathAutoAddSubFolderPath = c.HttpHomePathAutoAddSubFolderPath,
            HttpHomePathNoExtension = c.HttpHomePathNoExtension,
            FTPSEncryption = c.FTPSEncryption,
            Keypath = c.Keypath ?? "",
            Passphrase = c.Passphrase ?? ""
        };
    }
}
