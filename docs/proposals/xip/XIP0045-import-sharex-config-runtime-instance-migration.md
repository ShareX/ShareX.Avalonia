# XIP0045 Import ShareX Config — Runtime Instance Migration & UX Gaps

**Status**: LARGELY COMPLETE (tests and OAuth-provider instances pending)
**Priority**: High
**Related**: XIP0012, issue #171

---

## Problem Statement

`Import ShareX Config` imports legacy `UploadersConfig.json`, but active uploads use plugin
instances from `uploader-instances.json` (`ProviderId` + `SettingsJson`). There were two
distinct sub-problems:

1. **Custom Uploaders** — the `.sxcu` export pipeline was complete, but users still had to
   manually "Add from Catalog" to create a usable instance. Now fixed.

2. **Built-in Uploaders** — settings were imported into `SettingsManager.UploadersConfig`
   (backward-compat layer) but never connected to runtime instances. Now fixed via
   `BuiltinInstanceMigrator`.

---

## Code Audit (2026-03-03)

### What Is Fully Working

| Feature | File | Status |
|---------|------|--------|
| Detect / browse `UploadersConfig.json` | `UploadersConfigImporter.FindShareXUploadersConfig()` | ✅ |
| Deserialise and dispatch to per-category importers | `UploadersConfigImporter.ImportFromFile()` | ✅ |
| Extract `CustomUploaderItem` list from import result | `ImportResult.ImportedCustomUploaders` | ✅ |
| Export each item to `{PluginsFolder}/{name}.sxcu` | `ExportImportedCustomUploaders()` in `DestinationSettingsViewModel` | ✅ |
| Duplicate detection via `JToken.DeepEquals` | `IsEquivalentCustomUploaderFile()` | ✅ |
| Register new providers in `ProviderCatalog` | `ProviderCatalog.LoadCustomUploaders()` | ✅ |
| **Auto-create UploaderInstances for custom uploaders** | `AutoCreateCustomUploaderInstances()` | ✅ **NEW** |
| Reload catalog when only duplicates exist | condition: `ExportedCount > 0 \|\| SkippedCount > 0` | ✅ **FIXED** |
| Conditionalize "Next step" message | `BuildImportSummary()` — gated on `InstancesCreated == 0` | ✅ **FIXED** |
| Consistent duplicate detection in `AddCustomUploader` | `ResolveCustomUploaderFilePath()` | ✅ **FIXED** |
| Refresh all destination-category UI lists | `category.LoadInstances()` per category | ✅ |
| Formatted import-summary dialog (3-tier) | `BuildImportSummary()` | ✅ **EXTENDED** |
| **S3 full migration** — all 3 categories, credentials to secret store | `BuiltinInstanceMigrator.MigrateAmazonS3()` | ✅ **NEW** |
| **FTP full migration** — one instance per account, 3 categories | `BuiltinInstanceMigrator.MigrateFtp()` | ✅ **NEW** |
| **Pastebin partial migration** — UserKey + prefs, ApiKey empty | `BuiltinInstanceMigrator.MigratePastebin()` | ✅ **NEW** |
| **Imgur partial migration** — prefs only, OAuth re-auth required | `BuiltinInstanceMigrator.MigrateImgur()` | ✅ **NEW** |
| **Skipped providers report** — no-plugin services listed in dialog | `BuiltinInstanceMigrator.CollectSkippedProviders()` | ✅ **NEW** |
| Idempotency — existing S3/Imgur instances reuse stored SecretKey | `ExtractSecretKey()` helper | ✅ **NEW** |

---

## Implementation Summary (2026-03-03)

### Stage 1 — Bug fixes in `DestinationSettingsViewModel.cs` (commit `678bd0fb`)

**Bug 1** (HIGH): `AutoCreateCustomUploaderInstances()` added. After exporting `.sxcu` files
and reloading the catalog, each exported file has its `ProviderId` derived via Slugify and
`InstanceManager.AddInstance()` called for each supported category. `CustomUploaderExportResult`
extended with `ExportedFilePaths`, `InstancesCreated`, `InstancesSkipped`.

**Bug 2** (MEDIUM): Gate condition changed from `ExportedCount > 0` to
`ExportedCount > 0 || SkippedCount > 0`. The catalog now reloads in a fresh session where
all files are duplicates but not yet registered.

**Bug 3** (LOW): "Next step: use Add from Catalog" is now conditional — suppressed when
`InstancesCreated > 0`, replaced by "Auto-created N destination instance(s) — ready to use."

**Bug 4** (LOW): `AddCustomUploader()` now calls `ResolveCustomUploaderFilePath()` instead of a
bare counter loop. If an identical uploader already exists, shows "Custom Uploader Already Exists"
dialog instead of silently creating a duplicate file.

### Stage 2+3 — `BuiltinInstanceMigrator` + wiring (commit `4a0a7390`)

**New file**: `src/desktop/core/XerahS.Uploaders/LegacySupport/BuiltinInstanceMigrator.cs`

Migration tiers:

| Provider | Category | Migration | Notes |
|----------|----------|-----------|-------|
| Amazon S3 | Image, Text, File | **Full** | Credentials to secret store via `SetSecret("accessKeyId"/"secretAccessKey")`. Idempotent: reuses existing `SecretKey` GUID. StorageClass enum values are identical (0–4). |
| FTP/FTPS/SFTP | File, Image, Text | **Full** | One instance per account. Password in SettingsJson (no secret store). Dedup by `displayName + providerId`. |
| Pastebin | Text | **Partial** | `UserKey`, `Username`, prefs copied. `ApiKey` (developer key) not in legacy config → left empty. User warned. |
| Imgur | Image | **Partial** | `AccountType`, `DirectLink`, `ThumbnailType`, `UseGIFV`, `UploadToSelectedAlbum` copied. OAuth tokens cannot be migrated → new `SecretKey` GUID. If existing instance found, its `SecretKey` is preserved. User warned to re-authorize. |
| ImageShack, Flickr, Photobucket, vgy.me, Gist, Paste.ee, Hastebin (custom), OneTimeSecret, Dropbox, OneDrive, Google Drive, Azure Storage, Backblaze B2, bit.ly, YOURLS, Polr, Firebase, Kutt, uPaste | — | **Skipped** | Listed in dialog under "no XerahS plugin available". |

**Summary dialog** now shows three tiers:
1. Uploader settings imported (existing)
2. Custom uploader export (existing, now with instance-creation count)
3. Built-in provider migration (new)

---

## Remaining Items

- [ ] Unit tests (see Tests section below).
- [ ] OAuth-based providers (Dropbox, OneDrive, Google Drive): consider a "reconnect" UX
  that imports settings then prompts for re-auth — currently these are listed as skipped.
- [ ] Pastebin: surface a prompt for the developer API key after instance creation (so the
  instance becomes fully usable without manual settings navigation).

---

## Tests

- [ ] Unit test: import config with 3 custom uploaders → 3 `.sxcu` files + 3 instances.
- [ ] Unit test: re-import same config → 0 new files, 0 new instances (idempotent).
- [ ] Unit test: import config where identical `.sxcu` already exists → skipped, existing
  instance preserved.
- [ ] Unit test: built-in importer (Imgur) creates instance when none exists.
- [ ] Unit test: built-in importer (Imgur) updates instance (reuses SecretKey) when one already
  exists with same `ProviderId`.
- [ ] Unit test: built-in importer (S3) stores credentials in secret store.

---

## Key Files Changed / Created

| File | Change |
|------|--------|
| `src/desktop/app/XerahS.UI/ViewModels/DestinationSettingsViewModel.cs` | Bug 1–4 fixes; `AutoCreateCustomUploaderInstances()`; `BuiltinInstanceMigrator.Migrate()` call; `BuildImportSummary()` extended |
| `src/desktop/core/XerahS.Uploaders/LegacySupport/BuiltinInstanceMigrator.cs` | **NEW** — full S3/FTP + partial Pastebin/Imgur migration; skipped-provider collection |

---

## Acceptance Criteria

1. ✅ Importing `UploadersConfig.json` with custom uploaders automatically creates usable
   destination instances — no "Add from Catalog" step required.
2. ✅ Re-importing the same file does not create duplicate `.sxcu` files or instances.
3. ✅ Importing in a fresh session where `.sxcu` files already exist registers providers
   and creates missing instances correctly.
4. ✅ S3 and FTP built-in uploaders produce runtime instances with settings populated.
5. ✅ Import completion dialog accurately reports: files created, files skipped, instances
   created, instances skipped, per-provider partial/warning messages.
6. ✅ `dotnet build` on `XerahS.UI` and `XerahS.Uploaders` passes with 0 errors.
7. ⬜ Unit tests for idempotency and instance creation scenarios.
8. ⬜ OAuth re-auth UX for Dropbox/OneDrive/Google Drive (post-import reconnect flow).

---

## Verification Commands

```powershell
dotnet build src/desktop/core/XerahS.Uploaders/XerahS.Uploaders.csproj -m:1
dotnet build src/desktop/app/XerahS.UI/XerahS.UI.csproj -m:1
```
