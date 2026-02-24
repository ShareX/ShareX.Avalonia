# Plugin and Uploaders Architecture Analysis

## Goal
Plugins should be as self-contained as possible at runtime. Governance and the overall interface are provided by `src/desktop/core/XerahS.Uploaders`.

Legacy import and mobile support remain required. This includes ShareX `UploadersConfig.json` import and mobile compatibility with legacy DTO shapes.

## Scope and Non-Goals

### In scope for this analysis and XIP0040
- Keep runtime plugin behavior provider-driven (`ProviderId` + `SettingsJson`).
- Isolate legacy and mobile compatibility code in one explicit location.
- Remove provider-specific secret migration branches from core runtime manager.
- Remove Amazon S3 runtime dependency on core legacy enum.

### Out of scope
- Full redesign of `UploadersConfig` into opaque provider blobs.
- Full replacement of `UploaderType` with `ProviderId` across all legacy APIs.
- Splitting legacy support into a new assembly.

## Current State (Code-Verified)

### Runtime call path

1. Provider context and migration bootstrap:
- `src/desktop/core/XerahS.Core/Uploaders/ProviderContextManager.cs`
- `EnsureProviderContext()` sets provider context and calls `InstanceManager.Instance.MigrateSecretsIfNeeded()`.

2. Provider discovery and catalog:
- `src/desktop/core/XerahS.Uploaders/PluginSystem/ProviderCatalog.cs`
- providers are loaded and later resolved by `ProviderId`.

3. Instance creation and defaults in UI:
- `src/desktop/app/XerahS.UI/ViewModels/ProviderCatalogViewModel.cs`
- `AddSelected()` creates `UploaderInstance` using `provider.GetDefaultSettings(Category)`.

4. Runtime config round-trip in UI:
- `src/desktop/app/XerahS.UI/ViewModels/UploaderInstanceViewModel.cs`
- `ConfigViewModel.LoadFromJson(SettingsJson)` and `SettingsJson = ConfigViewModel.ToJson()`.

5. Upload execution:
- `src/desktop/core/XerahS.Core/Tasks/Processors/UploadJobProcessor.cs`
- provider resolved by `ProviderId`; runtime uploader created via `provider.CreateInstance(instance.SettingsJson)`.

Conclusion: runtime behavior is already plugin-centric and instance-based.

### Legacy and mobile compatibility call path

1. Legacy settings object and sync:
- `src/desktop/core/XerahS.Uploaders/UploadersConfig.cs`
- includes legacy flat properties and polymorphic `ServiceSettings` with sync methods.

2. ShareX import flow:
- `src/desktop/core/XerahS.Uploaders/UploadersConfigImporter.cs`
- invoked by `src/desktop/app/XerahS.UI/ViewModels/DestinationSettingsViewModel.cs`.

3. Settings load and save:
- `src/desktop/core/XerahS.Core/Managers/SettingsManager.cs`
- `LoadUploadersConfig()` -> `EnsurePolymorphicSettingsInitialized()`.
- save path calls `SyncPolymorphicSettingsFromLegacy()` before write.

4. Mobile compatibility usage:
- `src/mobile-experimental/XerahS.Mobile.Core/MobileAmazonS3ConfigViewModel.cs`
- attempts instance-based load first, then legacy fallback `LoadFromLegacySettings()` using `SettingsManager.UploadersConfig.AmazonS3Settings`.
- `src/mobile-experimental/XerahS.Mobile.Core/MobileCustomUploaderConfigViewModel.cs` reads/writes legacy custom uploader lists.

Conclusion: legacy and mobile compatibility are intentionally core-owned and still actively used.

## Architectural Decision: Explicit LegacySupport Isolation

All code used primarily for ShareX legacy compatibility and mobile fallback must be physically consolidated under:

- `src/desktop/core/XerahS.Uploaders/UploadersLib/LegacySupport`

This is an organization and discoverability boundary. It does not change runtime behavior.

### Why this is required
- Removes ambiguity between runtime plugin ownership and compatibility-only code.
- Makes duplicate-looking DTOs intentional and discoverable.
- Reduces accidental new runtime dependencies on legacy code.

## LegacySupport File Boundary

### Files that must be moved under `UploadersLib/LegacySupport`

- `src/desktop/core/XerahS.Uploaders/UploadersConfig.cs`
- `src/desktop/core/XerahS.Uploaders/UploadersConfigImporter.cs`
- `src/desktop/core/XerahS.Uploaders/Abstractions/IUploaderConfig.cs`
- `src/desktop/core/XerahS.Uploaders/Configuration/UploaderType.cs`
- `src/desktop/core/XerahS.Uploaders/Configuration/ImgurConfig.cs`
- `src/desktop/core/XerahS.Uploaders/Configuration/DropboxConfig.cs`
- `src/desktop/core/XerahS.Uploaders/Configuration/FtpConfig.cs`
- `src/desktop/core/XerahS.Uploaders/Configuration/S3Config.cs`
- `src/desktop/core/XerahS.Uploaders/Configuration/CustomUploaderConfig.cs`
- `src/desktop/core/XerahS.Uploaders/FileUploaders/AmazonS3Settings.cs`
- `src/desktop/core/XerahS.Uploaders/FileUploaders/AmazonS3StorageClass.cs`
- `src/desktop/core/XerahS.Uploaders/FileUploaders/FTPAccount.cs`
- `src/desktop/core/XerahS.Uploaders/Compatibility/UploaderFilter.cs`
- `src/desktop/core/XerahS.Uploaders/UploadersLib/Stubs.cs`
- `src/desktop/core/XerahS.Uploaders/UploadersLib/Properties/Resources.cs`

### Files that must remain outside `LegacySupport`

- `src/desktop/core/XerahS.Uploaders/PluginSystem/*`
- `src/desktop/core/XerahS.Uploaders/BaseUploaders/*`
- `src/desktop/core/XerahS.Uploaders/CustomUploader/*`
- runtime contracts used by plugins (`IUploaderProvider`, `IUploaderConfigViewModel`, `ISecretStore`, `UploaderCategory`, `UploaderInstance`)

## Runtime Couplings That Must Be Fixed in XIP0040

1. Hard-coded provider IDs in core migration logic:
- `src/desktop/core/XerahS.Uploaders/PluginSystem/InstanceManager.cs`
- `MigrateSecretsIfNeeded()` currently branches on `amazons3`, `imgur`, `gist`.

2. Amazon S3 plugin runtime enum coupling:
- `src/desktop/plugins/AmazonS3.Plugin/S3ConfigModel.cs`
- currently uses `XerahS.Uploaders.FileUploaders.AmazonS3StorageClass`.

## Required Runtime Contract Change

Introduce provider capability interface:

- new file: `src/desktop/core/XerahS.Uploaders/PluginSystem/IInstanceSecretMigrator.cs`

Recommended signature:

```csharp
namespace XerahS.Uploaders.PluginSystem;

public interface IInstanceSecretMigrator
{
    bool TryMigrateSecrets(
        string settingsJson,
        ISecretStore secrets,
        out string updatedSettingsJson,
        out int migratedSecretCount);
}
```

Behavior rules:
- idempotent
- non-destructive
- no provider-specific branches in core manager
- provider-specific migration implemented by provider classes

## Invariants (Must Not Change)

1. Keep existing namespaces and public type names for moved legacy files.
2. Keep serializer field names and semantics unchanged.
3. Keep `UploadersConfig` import and mobile behavior intact.
4. Keep runtime provider execution model unchanged (`ProviderId` + `SettingsJson`).

## Documentation Requirements

Add:
- `src/desktop/core/XerahS.Uploaders/UploadersLib/LegacySupport/README.md`

README must state:
- folder purpose (legacy ShareX import + mobile compatibility)
- duplicate-looking DTOs are intentional
- runtime plugin code should not add new dependencies here unless for legacy/mobile compatibility

## Verification Gates

Run from repo root:

```powershell
rg --files src/desktop/core/XerahS.Uploaders/UploadersLib/LegacySupport
rg -n "class UploadersConfig|class UploadersConfigImporter|interface IUploaderConfig|enum UploaderType|class AmazonS3Settings|enum AmazonS3StorageClass|namespace ShareX.UploadersLib" src/desktop/core/XerahS.Uploaders/UploadersLib/LegacySupport
rg -n "providerId == \"amazons3\"|providerId is \"imgur\" or \"gist\"|RequiresSecretKey" src/desktop/core/XerahS.Uploaders/PluginSystem/InstanceManager.cs
rg -n "XerahS\.Uploaders\.FileUploaders\.AmazonS3StorageClass" src/desktop/plugins/AmazonS3.Plugin
dotnet test tests/XerahS.Tests/XerahS.Tests.csproj --filter UploadersConfigPolymorphicTests -m:1
dotnet build src/desktop/XerahS.sln -m:1
```

Build timeout policy: if any build exceeds 5 minutes, stop it, resolve lock/process issues, and rerun.

Implementation plan and file-level execution details are defined in [tasks/XIP0040_Plugin_Architecture_Action_Items.md](../../tasks/XIP0040_Plugin_Architecture_Action_Items.md).
