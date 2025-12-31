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

namespace ShareX.Avalonia.Uploaders.PluginSystem;

/// <summary>
/// Static registry of available uploader providers
/// </summary>
public static class ProviderCatalog
{
    private static readonly Dictionary<string, IUploaderProvider> _providers = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Register a provider in the catalog
    /// </summary>
    public static void RegisterProvider(IUploaderProvider provider)
    {
        if (provider == null) throw new ArgumentNullException(nameof(provider));
        
        lock (_lock)
        {
            if (!_providers.ContainsKey(provider.ProviderId))
            {
                _providers[provider.ProviderId] = provider;
            }
        }
    }

    /// <summary>
    /// Get a provider by its ID
    /// </summary>
    public static IUploaderProvider? GetProvider(string providerId)
    {
        lock (_lock)
        {
            return _providers.TryGetValue(providerId, out var provider) ? provider : null;
        }
    }

    /// <summary>
    /// Get all registered providers
    /// </summary>
    public static List<IUploaderProvider> GetAllProviders()
    {
        lock (_lock)
        {
            return _providers.Values.ToList();
        }
    }

    /// <summary>
    /// Get providers that support a specific category
    /// </summary>
    public static List<IUploaderProvider> GetProvidersByCategory(UploaderCategory category)
    {
        lock (_lock)
        {
            return _providers.Values
                .Where(p => p.SupportedCategories.Contains(category))
                .ToList();
        }
    }
}
