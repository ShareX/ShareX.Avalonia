# XerahS.UploaderPluginSdk

Lightweight **contracts only** (interfaces and DTOs) for building XerahS uploader destination plugins. This package has **no implementation** and no dependency on uploader types (e.g. `Uploader`), so plugins can depend on the SDK alone for the contract and optionally reference **XerahS.Uploaders** for base classes and runtime types.

## Use cases

- **Plugin authors**: Reference this package to implement `IUploaderProvider` (and optionally `IUploaderExplorer`). For convenience, also reference **XerahS.Uploaders** to use `UploaderProviderBase`, `Uploader`, `GenericUploader`, etc.
- **Host/app**: Reference this package when you only need the plugin contract types (e.g. `IUploaderProvider`, `PluginManifest`, `UploaderInstance`).

## Main types

| Type | Description |
|------|-------------|
| `IUploaderProvider` | Entry point for a destination: metadata, `CreateInstance(settingsJson)`, config view/VM, validation. |
| `IUploaderExplorer` | Optional: Media Explorer (list/thumbnail/delete/create folder). |
| `IUploaderConfigViewModel` | Optional: ViewModel for provider config UI. |
| `UploaderCategory` | Image, Text, File, UrlShortener, UrlSharing. |
| `UploaderInstance` | A configured instance (ProviderId, DisplayName, SettingsJson, FileTypeScope, etc.). |
| `FileTypeScope` | Per-category file extension routing for an instance. |
| `PluginManifest` | Deserialized from `plugin.json` (PluginId, EntryPoint, SupportedCategories, ApiVersion, etc.). |
| `ExplorerQuery` / `ExplorerPage` / `MediaItem` | Used by `IUploaderExplorer.ListAsync`. |
| `ISecretStore` / `IProviderContext` / `IProviderContextAware` | Optional: host-provided secrets and context. |
| `IInstanceSecretMigrator` / `ISecretStoreInfo` | Optional: migrate legacy plaintext settings into the host secret store; describe secret keys. |

## IUploaderProvider.CreateInstance

`CreateInstance(string settingsJson)` returns **object** so the SDK does not depend on `Uploader`. The host (or code that references XerahS.Uploaders) casts the result to `Uploader` (or the appropriate uploader type) for use.

## Secrets (optional)

Implement **IInstanceSecretMigrator** so the host can migrate legacy plaintext credentials from settings JSON into the secret store. Implement **ISecretStoreInfo** to describe which secret keys your provider uses (for UI or tooling).

## plugin.json

Each plugin ships a `plugin.json` next to its assembly. Required fields include: `pluginId`, `name`, `apiVersion`, `entryPoint` (full type name of the class implementing `IUploaderProvider`), and `supportedCategories`. See the plugin developer guide (`docs/developers/PLUGIN_SDK.md`) and the template in `docs/templates/PluginTemplate/`.

## Versioning

- **ApiVersion** in the manifest should match the host’s supported API (e.g. `"1.0"`). Major version mismatch may prevent loading.
- This NuGet package follows semantic versioning; contract changes will be reflected in the package version.
