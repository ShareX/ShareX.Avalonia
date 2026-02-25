#region License Information (GPL v3)
/*
    XerahS - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2026 ShareX Team
*/
#endregion

namespace XerahS.Uploaders.PluginSystem;

public interface ISecretStore
{
    string? GetSecret(string providerId, string secretKey, string name);
    void SetSecret(string providerId, string secretKey, string name, string value);
    void DeleteSecret(string providerId, string secretKey, string name);
    bool HasSecret(string providerId, string secretKey, string name);
}
