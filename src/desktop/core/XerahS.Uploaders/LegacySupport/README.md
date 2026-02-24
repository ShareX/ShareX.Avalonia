# LegacySupport

This folder exists for **ShareX legacy import compatibility** and **mobile compatibility**.

## Purpose

Files here support deserializing `UploadersConfig.json` produced by ShareX WinForms and
importing credentials/settings into the XerahS plugin system.

`LegacyDestinationEnums.cs` contains the deprecated destination enums (ImageDestination,
TextDestination, FileDestination, UrlShortenerType, URLSharingServices). The runtime uses
the plugin model (ProviderId); these enums are kept only for config serialization and
should not be used for new code.

## Rules

- **Duplicate-looking DTOs are intentional and required.** The classes here mirror the
  original ShareX `UploadersLib` structure so that JSON deserialization works without
  re-serialization or format conversion.
- **Namespaces are preserved** from the original ShareX source (e.g. `ShareX.UploadersLib`,
  `XerahS.Uploaders.FileUploaders`, `XerahS.Uploaders.Configuration`) regardless of folder
  location. This keeps legacy JSON compatible.
- **Runtime plugin code must not add new dependencies here** unless specifically required
  for legacy/mobile compatibility. New uploader logic belongs in `PluginSystem` or inside
  the plugin assemblies.
