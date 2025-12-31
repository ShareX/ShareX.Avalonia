#region License Information (GPL v3)

/*
    ShareX.Avalonia - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2025 ShareX Team

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

using ShareX.Avalonia.Uploaders.PluginSystem;

namespace ShareX.Avalonia.Uploaders.Plugins.AmazonS3Plugin;

/// <summary>
/// Amazon S3 file uploader plugin (POC stub implementation)
/// </summary>
public class AmazonS3Plugin : UploaderPluginBase
{
    public override string Id => "amazons3";
    public override string Name => "Amazon S3";
    public override string Description => "Upload files to Amazon Simple Storage Service (S3)";
    public override Version Version => new Version(1, 0, 0);
    public override UploaderCategory Category => UploaderCategory.File;
    public override Type ConfigModelType => typeof(S3ConfigModel);

    public AmazonS3Plugin()
    {
        // Register this plugin with the manager
        PluginManager.Instance.RegisterPlugin(this);
    }

    public override Uploader CreateInstance(object config)
    {
        if (config is not S3ConfigModel s3Config)
        {
            throw new ArgumentException("Invalid configuration type for Amazon S3 plugin", nameof(config));
        }

        return new AmazonS3Uploader(s3Config);
    }

    public override object GetDefaultConfig()
    {
        return new S3ConfigModel();
    }

    // UI view will be created in Phase 4
    public override object? CreateConfigView()
    {
        // TODO: Return AmazonS3ConfigView instance when UI is implemented
        return null;
    }
}
