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

using ShareX.Ava.Uploaders.Plugins.AmazonS3Plugin;
using ShareX.Ava.Uploaders.Plugins.ImgurPlugin;

namespace ShareX.Ava.Uploaders.PluginSystem;

/// <summary>
/// Initializes built-in plugins on application startup
/// </summary>
public static class PluginInitializer
{
    private static bool _initialized = false;

    /// <summary>
    /// Initialize all built-in plugins
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        // Create instances to trigger auto-registration
        _ = new ImgurPlugin();
        _ = new AmazonS3Plugin();

        _initialized = true;
    }
}
