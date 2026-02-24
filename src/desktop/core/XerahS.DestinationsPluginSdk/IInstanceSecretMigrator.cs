#region License Information (GPL v3)
/*
    XerahS - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2026 ShareX Team
*/
#endregion

namespace XerahS.Uploaders.PluginSystem;

/// <summary>
/// Implemented by providers that migrate legacy plaintext credentials from settings JSON into ISecretStore.
/// </summary>
public interface IInstanceSecretMigrator
{
    bool TryMigrateSecrets(
        string settingsJson,
        ISecretStore secrets,
        out string updatedSettingsJson,
        out int migratedSecretCount);
}
