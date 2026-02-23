# XIP0040: Plugin and Uploaders Architecture - Implementation Action Items

## Objective
Implement the next step of plugin/runtime separation while keeping legacy ShareX import and mobile compatibility intact.

This XIP now has three mandatory outcomes:
1. Move all legacy-support code into `UploadersLib/LegacySupport` (physical consolidation).
2. Remove provider-ID secret migration logic from `InstanceManager` by introducing provider capability contracts.
3. Make Amazon S3 runtime enum ownership plugin-local.

Architecture source: [docs/architecture/PLUGIN_AND_UPLOADERS_ARCHITECTURE_ANALYSIS.md](../docs/architecture/PLUGIN_AND_UPLOADERS_ARCHITECTURE_ANALYSIS.md)

## Non-Negotiable Scope Decisions
1. Runtime plugin flow remains `ProviderId + SettingsJson`.
2. Legacy import/mobile behavior remains supported with current DTO shapes.
3. Consolidation is organizational only. No namespace/type rename and no JSON schema change.
4. `UploadersConfig` redesign and `UploaderType` replacement are not part of this XIP.

## Work Item 1: Consolidate legacy support code into `UploadersLib/LegacySupport`

### Goal
Make compatibility-only code explicit and centralized so duplicate-looking types are clearly marked as legacy/mobile support.

### Target root
- `src/desktop/core/XerahS.Uploaders/UploadersLib/LegacySupport`

### Required file moves
Move these files under `UploadersLib/LegacySupport` (you may keep subfolders, but all must live under this root):

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

### Required docs in the folder
Add:
- `src/desktop/core/XerahS.Uploaders/UploadersLib/LegacySupport/README.md`

Required README content:
- State this folder exists for ShareX legacy import compatibility and mobile compatibility.
- State duplicate-looking DTOs are intentional.
- State runtime plugin code must not add new dependencies on types in this folder unless for legacy/mobile compatibility.

### Rules
1. Keep existing namespaces unchanged.
2. Keep public type names unchanged.
3. Keep serializer behavior unchanged.
4. Keep references/build behavior unchanged.

### Acceptance criteria
- All listed files are located under `UploadersLib/LegacySupport`.
- No listed file remains outside that root.
- Project builds without code behavior changes.

## Work Item 2: Introduce `IInstanceSecretMigrator` and refactor `InstanceManager`

### Goal
Remove hard-coded provider IDs from core secret migration logic.

### Core contract (exact)
Add in `src/desktop/core/XerahS.Uploaders/PluginSystem/IInstanceSecretMigrator.cs`:

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

### Core refactor requirements
Update `src/desktop/core/XerahS.Uploaders/PluginSystem/InstanceManager.cs`:
1. Remove `RequiresSecretKey` and all provider-specific branches (`amazons3`, `imgur`, `gist`).
2. Resolve provider by `ProviderCatalog.GetProvider(instance.ProviderId)`.
3. If provider implements `IInstanceSecretMigrator`, call it.
4. Replace `instance.SettingsJson` with returned JSON when migration reports update.
5. Keep one save pass at end (`SaveConfiguration`) only if any instance changed.

### Provider implementations required in this XIP
- `src/desktop/plugins/AmazonS3.Plugin/AmazonS3Provider.cs`
- `src/desktop/plugins/Imgur.Plugin/ImgurProvider.cs`
- `src/desktop/plugins/GitHubGist.Plugin/GitHubGistProvider.cs`

### Required migration behavior parity
- `amazons3`: migrate `AccessKeyId`, `SecretAccessKey` into secret store (`accessKeyId`, `secretAccessKey`), ensure `SecretKey`, remove plaintext fields after successful secret write.
- `imgur`: migrate `OAuth2Info.Client_Secret` and `OAuth2Info.Token`, ensure `SecretKey`, copy `OAuth2Info.Client_ID` to `ClientId` when missing, remove `OAuth2Info` only when secrets moved.
- `gist`: migrate `OAuth2Info.Client_ID`, `OAuth2Info.Client_Secret`, `OAuth2Info.Token`, ensure `SecretKey`, remove `OAuth2Info` only when secrets moved.

### Acceptance criteria
- `InstanceManager.cs` contains no provider-ID special-casing for migration.
- Migration remains idempotent and non-destructive.
- Existing secrets migration behavior is preserved.

## Work Item 3: Make Amazon S3 runtime enum plugin-local

### Goal
Remove runtime dependency on core `AmazonS3StorageClass` while preserving settings compatibility.

### Required changes
1. Add plugin-local enum in `src/desktop/plugins/AmazonS3.Plugin` (new file).
2. Update `src/desktop/plugins/AmazonS3.Plugin/S3ConfigModel.cs` to use plugin enum.
3. Update `src/desktop/plugins/AmazonS3.Plugin/ViewModels/AmazonS3ConfigViewModel.cs` to use plugin enum.
4. Remove runtime references to `XerahS.Uploaders.FileUploaders.AmazonS3StorageClass` from Amazon S3 plugin.

### Compatibility requirement
- Keep numeric enum values aligned with existing persisted JSON values (`0..4`).

### Acceptance criteria
- `rg -n "AmazonS3StorageClass" src/desktop/plugins/AmazonS3.Plugin` returns no core-type usage in runtime config path.
- S3 provider compiles and runtime settings serialize/deserialize without migration break.

## Implementation Order
1. Work Item 1 (LegacySupport consolidation).
2. Work Item 2 (secret migrator contract and provider implementations).
3. Work Item 3 (S3 runtime enum ownership).
4. Final docs pass and verification.

## Verification Commands
Run from repo root:

```powershell
rg --files src/desktop/core/XerahS.Uploaders/UploadersLib/LegacySupport
rg -n "class UploadersConfig|class UploadersConfigImporter|interface IUploaderConfig|enum UploaderType|class AmazonS3Settings|enum AmazonS3StorageClass|namespace ShareX.UploadersLib" src/desktop/core/XerahS.Uploaders/UploadersLib/LegacySupport
rg -n "providerId == \"amazons3\"|providerId is \"imgur\" or \"gist\"|RequiresSecretKey" src/desktop/core/XerahS.Uploaders/PluginSystem/InstanceManager.cs
rg -n "XerahS\.Uploaders\.FileUploaders\.AmazonS3StorageClass" src/desktop/plugins/AmazonS3.Plugin
dotnet test tests/XerahS.Tests/XerahS.Tests.csproj --filter UploadersConfigPolymorphicTests -m:1
dotnet build src/desktop/XerahS.sln -m:1
```

Build timeout rule: if a build hangs beyond 5 minutes, stop it, fix the lock/process issue, and rerun.

## Out of Scope
- `UploadersConfig` opaque/slim redesign.
- Global replacement of `UploaderType` with `ProviderId`.
- Extracting legacy support into a separate assembly.
