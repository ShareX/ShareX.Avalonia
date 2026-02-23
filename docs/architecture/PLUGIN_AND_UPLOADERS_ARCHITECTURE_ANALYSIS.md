# Plugin and XerahS.Uploaders Architecture Analysis

## Goal
Plugins should be as self-contained as possible at runtime. Governance and the overall plugin interface are provided by `src/desktop/core/XerahS.Uploaders`.

Legacy import and mobile support remain first-class requirements. XerahS supports ShareX `UploadersConfig.json` import and mobile uses the same legacy config shape (`UploadersConfig`, `AmazonS3Settings`, and related DTOs).

## Core Architectural Decision
Legacy support code stays in core, but it must be clearly isolated so contributors do not mistake it for runtime plugin ownership.

For XIP0040, legacy-support code is physically consolidated under:

- `src/desktop/core/XerahS.Uploaders/UploadersLib/LegacySupport`

This is an organization and clarity change, not a behavior change.

## What This Means

### Runtime plugin path (self-contained)
Runtime upload flow remains plugin-owned and instance-driven:

- Provider discovery/load: `PluginSystem/ProviderCatalog.cs`
- Instance storage: `PluginSystem/UploaderInstance.cs` (`ProviderId`, `SettingsJson`)
- Runtime execution: `XerahS.Core/Tasks/Processors/UploadJobProcessor.cs` (`provider.CreateInstance(instance.SettingsJson)`)
- Config UI round-trip: `XerahS.UI/ViewModels/UploaderInstanceViewModel.cs`

### Legacy/mobile compatibility path (core-owned)
Legacy/mobile compatibility remains in core and is intentionally separate from runtime plugin ownership:

- `UploadersConfig.cs`
- `UploadersConfigImporter.cs`
- `Configuration/*` polymorphic legacy config types
- `FileUploaders/AmazonS3Settings.cs`, `FileUploaders/AmazonS3StorageClass.cs`
- `UploadersLib/*` ShareX compatibility stubs
- Mobile fallback usage in `src/mobile-experimental/XerahS.Mobile.Core`

## Couplings Still To Fix in XIP0040
1. `InstanceManager.MigrateSecretsIfNeeded()` hard-codes provider IDs (`amazons3`, `imgur`, `gist`).
2. Amazon S3 runtime model still references core enum `XerahS.Uploaders.FileUploaders.AmazonS3StorageClass`.
3. Legacy support code is not yet physically grouped under one explicit folder.

## XIP0040 Required Outcomes
1. Provider-specific secret migration logic moves from `InstanceManager` to provider capabilities.
2. Amazon S3 runtime enum ownership moves into `src/desktop/plugins/AmazonS3.Plugin`.
3. Compatibility-only code is consolidated under `UploadersLib/LegacySupport` and documented as legacy/mobile support.

## LegacySupport Consolidation Rules
1. Folder move only, no runtime behavior change.
2. Keep existing namespaces and public type names unchanged.
3. Keep JSON field names and serialization behavior unchanged.
4. Add a short `README.md` in `UploadersLib/LegacySupport` explaining duplicate-looking types are intentional for legacy and mobile compatibility.

## Out of Scope for XIP0040
- Replacing `UploaderType` with `ProviderId` everywhere.
- Redesigning `UploadersConfig` to an opaque/slim model.
- Moving legacy compatibility out of `XerahS.Uploaders` into a separate assembly.

Implementation details are defined in [tasks/XIP0040_Plugin_Architecture_Action_Items.md](../../tasks/XIP0040_Plugin_Architecture_Action_Items.md).
