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

namespace XerahS.Uploaders.PluginSystem;

/// <summary>
/// Implemented by providers that need to migrate legacy plaintext credentials
/// from a settings JSON blob into the <see cref="ISecretStore"/>.
/// </summary>
public interface IInstanceSecretMigrator
{
    /// <summary>
    /// Attempts to migrate plaintext secrets from <paramref name="settingsJson"/> into
    /// <paramref name="secrets"/>.
    /// </summary>
    /// <param name="settingsJson">Current raw settings JSON for the instance.</param>
    /// <param name="secrets">Secret store to write migrated secrets into.</param>
    /// <param name="updatedSettingsJson">
    /// The settings JSON after migration (plaintext fields removed, SecretKey ensured).
    /// Equals <paramref name="settingsJson"/> if no migration was performed.
    /// </param>
    /// <param name="migratedSecretCount">Number of individual secrets written to the store.</param>
    /// <returns>
    /// <see langword="true"/> if the JSON was modified and
    /// <paramref name="updatedSettingsJson"/> should be persisted; otherwise <see langword="false"/>.
    /// </returns>
    bool TryMigrateSecrets(
        string settingsJson,
        ISecretStore secrets,
        out string updatedSettingsJson,
        out int migratedSecretCount);
}
