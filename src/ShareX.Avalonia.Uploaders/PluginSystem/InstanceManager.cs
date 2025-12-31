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

using Newtonsoft.Json;

namespace ShareX.Avalonia.Uploaders.PluginSystem;

/// <summary>
/// Manages uploader instances - lifecycle, persistence, default selection
/// </summary>
public class InstanceManager
{
    private static readonly Lazy<InstanceManager> _instance = new(() => new InstanceManager());
    public static InstanceManager Instance => _instance.Value;

    private readonly object _lock = new();
    private InstanceConfiguration _configuration;
    private readonly string _configFilePath;

    private InstanceManager()
    {
        // TODO: Get proper config path from app settings
        var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShareX.Avalonia");
        Directory.CreateDirectory(configDir);
        _configFilePath = Path.Combine(configDir, "uploader-instances.json");
        
        _configuration = LoadConfiguration();
    }

    /// <summary>
    /// Get all configured uploader instances
    /// </summary>
    public List<UploaderInstance> GetInstances()
    {
        lock (_lock)
        {
            return new List<UploaderInstance>(_configuration.Instances);
        }
    }

    /// <summary>
    /// Get instances for a specific category
    /// </summary>
    public List<UploaderInstance> GetInstancesByCategory(UploaderCategory category)
    {
        lock (_lock)
        {
            return _configuration.Instances
                .Where(i => i.Category == category)
                .ToList();
        }
    }

    /// <summary>
    /// Get an instance by its ID
    /// </summary>
    public UploaderInstance? GetInstance(Guid instanceId)
    {
        lock (_lock)
        {
            return _configuration.Instances.FirstOrDefault(i => i.InstanceId == instanceId);
        }
    }

    /// <summary>
    /// Add a new uploader instance
    /// </summary>
    public void AddInstance(UploaderInstance instance)
    {
        lock (_lock)
        {
            if (_configuration.Instances.Any(i => i.InstanceId == instance.InstanceId))
            {
                throw new InvalidOperationException($"Instance with ID {instance.InstanceId} already exists");
            }

            instance.CreatedAt = DateTime.UtcNow;
            instance.ModifiedAt = DateTime.UtcNow;
            _configuration.Instances.Add(instance);
            SaveConfiguration();
        }
    }

    /// <summary>
    /// Update an existing instance
    /// </summary>
    public void UpdateInstance(UploaderInstance instance)
    {
        lock (_lock)
        {
            var existing = _configuration.Instances.FirstOrDefault(i => i.InstanceId == instance.InstanceId);
            if (existing == null)
            {
                throw new InvalidOperationException($"Instance with ID {instance.InstanceId} not found");
            }

            var index = _configuration.Instances.IndexOf(existing);
            instance.ModifiedAt = DateTime.UtcNow;
            _configuration.Instances[index] = instance;
            SaveConfiguration();
        }
    }

    /// <summary>
    /// Remove an instance
    /// </summary>
    public void RemoveInstance(Guid instanceId)
    {
        lock (_lock)
        {
            var instance = _configuration.Instances.FirstOrDefault(i => i.InstanceId == instanceId);
            if (instance != null)
            {
                _configuration.Instances.Remove(instance);
                
                // Remove from defaults if it was set
                var defaultsToRemove = _configuration.DefaultInstances
                    .Where(kvp => kvp.Value == instanceId)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                foreach (var category in defaultsToRemove)
                {
                    _configuration.DefaultInstances.Remove(category);
                }
                
                SaveConfiguration();
            }
        }
    }

    /// <summary>
    /// Duplicate an instance with new ID and optional display name
    /// </summary>
    public UploaderInstance DuplicateInstance(Guid sourceInstanceId, string? newDisplayName = null)
    {
        lock (_lock)
        {
            var source = _configuration.Instances.FirstOrDefault(i => i.InstanceId == sourceInstanceId);
            if (source == null)
            {
                throw new InvalidOperationException($"Instance with ID {sourceInstanceId} not found");
            }

            var duplicate = new UploaderInstance
            {
                InstanceId = Guid.NewGuid(),
                ProviderId = source.ProviderId,
                Category = source.Category,
                DisplayName = newDisplayName ?? $"{source.DisplayName} (Copy)",
                SettingsJson = source.SettingsJson,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
                IsAvailable = source.IsAvailable
            };

            _configuration.Instances.Add(duplicate);
            SaveConfiguration();
            
            return duplicate;
        }
    }

    /// <summary>
    /// Set the default instance for a category
    /// </summary>
    public void SetDefaultInstance(UploaderCategory category, Guid instanceId)
    {
        lock (_lock)
        {
            var instance = _configuration.Instances.FirstOrDefault(i => i.InstanceId == instanceId);
            if (instance == null)
            {
                throw new InvalidOperationException($"Instance with ID {instanceId} not found");
            }

            if (instance.Category != category)
            {
                throw new InvalidOperationException($"Instance category {instance.Category} does not match target category {category}");
            }

            _configuration.DefaultInstances[category] = instanceId;
            SaveConfiguration();
        }
    }

    /// <summary>
    /// Get the default instance for a category
    /// </summary>
    public UploaderInstance? GetDefaultInstance(UploaderCategory category)
    {
        lock (_lock)
        {
            if (_configuration.DefaultInstances.TryGetValue(category, out var instanceId))
            {
                return _configuration.Instances.FirstOrDefault(i => i.InstanceId == instanceId);
            }
            return null;
        }
    }

    private InstanceConfiguration LoadConfiguration()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                return JsonConvert.DeserializeObject<InstanceConfiguration>(json) ?? new InstanceConfiguration();
            }
        }
        catch
        {
            // If loading fails, return empty configuration
        }
        
        return new InstanceConfiguration();
    }

    private void SaveConfiguration()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_configuration, Formatting.Indented);
            File.WriteAllText(_configFilePath, json);
        }
        catch
        {
            // TODO: Add proper logging
        }
    }
}
