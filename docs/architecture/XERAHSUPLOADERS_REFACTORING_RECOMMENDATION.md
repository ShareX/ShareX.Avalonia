# XerahS.Uploaders Refactoring Recommendation

**Goal (from [PLUGIN_AND_UPLOADERS_ARCHITECTURE_ANALYSIS.md](PLUGIN_AND_UPLOADERS_ARCHITECTURE_ANALYSIS.md)):**  
Plugins should be as self-contained as possible at runtime; governance and interface live in `src/desktop/core/XerahS.Uploaders`. Legacy import and mobile support remain required (ShareX `UploadersConfig.json` import, mobile compatibility with legacy DTO shapes).

---

## 1. Which XerahS.Uploaders?

| Path | Status | Referenced by |
|------|--------|----------------|
| **`src/desktop/core/XerahS.Uploaders`** | Active project (85 .cs files) | Desktop app, Core, all desktop plugins, mobile-experimental, tests, PluginExporter |
| **`src/XerahS.Uploaders`** | Orphan folder (no .csproj, only `bin/` and `obj/`) | Nothing |

All refactoring below applies to **`src/desktop/core/XerahS.Uploaders`**. The folder **`src/XerahS.Uploaders`** is unrelated to the architecture goal and should be cleaned up separately (see §4).

---

## 2. Required Refactoring: None

With the goal in mind, **no further refactoring of `src/desktop/core/XerahS.Uploaders` is required** for the stated architecture:

- **LegacySupport isolation** – Done. Legacy/compat code is under `LegacySupport`; README states purpose and rules.
- **Runtime governance** – PluginSystem, BaseUploaders, CustomUploader, and contracts (`IUploaderProvider`, `IUploaderConfigViewModel`, `UploaderInstance`, etc.) live outside LegacySupport; runtime is provider-driven (`ProviderId` + `SettingsJson`).
- **No provider-specific branches in core** – `InstanceManager.MigrateSecretsIfNeeded()` uses `IInstanceSecretMigrator` only; no hard-coded provider IDs.
- **No plugin dependency on core legacy enum** – Amazon S3 plugin uses its own `S3StorageClass` in `AmazonS3.Plugin/S3StorageClass.cs`; no reference to `XerahS.Uploaders.FileUploaders.AmazonS3StorageClass`.
- **Plugins** – Reference only PluginSystem contracts (`IUploaderConfigViewModel`, `IProviderContextAware`, etc.), not LegacySupport types, for runtime behavior.

---

## 3. Optional Refactoring (Goal-Aligned)

These are improvements for consistency and long-term maintainability; they are **not** required to satisfy the goal.

### 3.1 ~~Move~~ CheveretoUploader removed

- **Done:** `ImageUploaders/CheveretoUploader.cs` was removed; legacy `UploadersConfig` properties and importer branch for Chevereto and `ImageDestination.Chevereto` enum were removed. ShareX import no longer imports Chevereto settings.

### 3.2 DEAD_LEGACY cleanup (out of scope for goal; optional later)

The [uploader audit](https://github.com/ShareXteam/ShareXteam/blob/main/XerahS/docs/audits/202602/uploader-audit/20260223_xerahs_uploaders_usage_audit.md) (2026-02-23) classifies 71 files as `DEAD_LEGACY` (e.g. `BaseServices/*`, many `FileUploaders/*` concrete uploaders, `UploaderFactory`, `URLShortener`). The architecture doc explicitly leaves “splitting legacy support into a new assembly” out of scope. Removing or relocating dead code is optional and should be a separate change with full regression and mobile/import checks; it is **not** a refactoring required by the current goal.

### 3.3 Verification gates

Re-run the verification commands from the architecture doc (and XIP0040) after any future edits to uploaders/plugins:

```bash
rg --files src/desktop/core/XerahS.Uploaders/LegacySupport
rg -n "providerId == \"amazons3\"|providerId.*imgur|RequiresSecretKey" src/desktop/core/XerahS.Uploaders/PluginSystem/InstanceManager.cs
rg -n "XerahS\.Uploaders\.FileUploaders\.AmazonS3StorageClass" src/desktop/plugins/AmazonS3.Plugin
dotnet test tests/XerahS.Tests/XerahS.Tests.csproj --filter UploadersConfigPolymorphicTests -m:1
dotnet build src/desktop/XerahS.sln -m:1
```

Expected: LegacySupport file list correct; no provider-ID branches in InstanceManager; no core enum usage in Amazon S3 plugin; tests and build pass.

---

## 4. Cleanup of `src/XerahS.Uploaders` (Separate from Goal)

- **Current state:** Directory exists with only `bin/` and `obj/`; no `.csproj` and no source files. Not referenced by any solution or project.
- **Recommendation:** Remove the directory, or add a brief `README.md` stating that the canonical uploaders project is `src/desktop/core/XerahS.Uploaders` and that this folder is obsolete/unused. Prefer removal if nothing (e.g. scripts or docs) depends on its path.

---

## 5. Summary

| Question | Answer |
|----------|--------|
| Is refactoring of **desktop core** XerahS.Uploaders **required** for the goal? | **No.** |
| Optional refactors for the goal? | CheveretoUploader removed; keep verification gates in mind. |
| What about **src/XerahS.Uploaders**? | Clean up orphan folder (remove or document as obsolete); not part of the plugin/uploaders architecture goal. |
