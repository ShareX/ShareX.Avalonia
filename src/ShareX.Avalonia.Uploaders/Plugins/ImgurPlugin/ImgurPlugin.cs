#region License Information (GPL v3)

/*
    ShareX.Ava - The Avalonia UI implementation of ShareX
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

using ShareX.Ava.Uploaders.PluginSystem;

namespace ShareX.Ava.Uploaders.Plugins.ImgurPlugin;

/// <summary>
/// Imgur image uploader plugin (POC stub implementation)
/// </summary>
public class ImgurPlugin : UploaderPluginBase
{
    public override string Id => "imgur";
    public override string Name => "Imgur";
    public override string Description => "Upload images to Imgur - free image hosting service";
    public override Version Version => new Version(1, 0, 0);
    public override UploaderCategory Category => UploaderCategory.Image;
    public override Type ConfigModelType => typeof(ImgurConfigModel);

    public ImgurPlugin()
    {
        // Register this plugin with the manager
        PluginManager.Instance.RegisterPlugin(this);
    }

    public override Uploader CreateInstance(object config)
    {
        if (config is not ImgurConfigModel imgurConfig)
        {
            throw new ArgumentException("Invalid configuration type for Imgur plugin", nameof(config));
        }

        return new ImgurUploader(imgurConfig);
    }

    public override object GetDefaultConfig()
    {
        return new ImgurConfigModel();
    }

    // UI view will be created in Phase 4
    public override object? CreateConfigView()
    {
        // TODO: Return ImgurConfigView instance when UI is implemented
        return null;
    }
}
