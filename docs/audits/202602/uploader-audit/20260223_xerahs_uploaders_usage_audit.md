# XerahS.Uploaders Usage Audit (Desktop Plugin Architecture)

Date: 2026-02-23

## Scope
- Audited every `*.cs` under `src/desktop/core/XerahS.Uploaders` (excluding generated `obj`/`bin`).
- Goal: classify each file as actively used vs dead/superseded in current desktop plugin architecture (`src/desktop/plugins`).

## Key Evidence (Active Architecture)
- Upload execution uses plugin system only in `src/desktop/core/XerahS.Core/Tasks/Processors/UploadJobProcessor.cs:118` and `src/desktop/core/XerahS.Core/Tasks/Processors/UploadJobProcessor.cs:131`.
- Capture upload path also uses plugin system in `src/desktop/core/XerahS.Core/Tasks/Processors/CaptureJobProcessor.cs:269`.
- Plugin catalog initialization/loading is done at startup in `src/desktop/app/XerahS.App/Program.cs:493` and `src/desktop/app/XerahS.App/Program.cs:498`.
- URL shortener legacy path is explicitly not implemented yet: `src/desktop/core/XerahS.Core/Tasks/Processors/UploadJobProcessor.cs:529`.
- Legacy reflection factory exists but has no active desktop call site: `src/desktop/core/XerahS.Uploaders/UploaderFactory.cs:33`.

## Status Legend
- `USED_RUNTIME`: directly part of active plugin/provider upload runtime.
- `USED_COMPAT`: used for settings/migration/import/compatibility, but not primary runtime upload execution.
- `DEAD_LEGACY`: legacy path superseded by plugin architecture, or otherwise unreferenced in active desktop flow.

## Summary
- Total files audited: **154**
- Used (`USED_RUNTIME` + `USED_COMPAT`): **83** (67 runtime, 16 compat)
- Dead legacy: **71**

## File-by-File Classification

| File | Used? | Classification | Reason |
| --- | --- | --- | --- |
| `src/desktop/core/XerahS.Uploaders/Abstractions/IUploaderConfig.cs` | YES | `USED_COMPAT` | Polymorphic uploader settings abstraction used by UploadersConfig service settings. |
| `src/desktop/core/XerahS.Uploaders/APIKeys/APIKeys.cs` | NO | `DEAD_LEGACY` | No active references found. |
| `src/desktop/core/XerahS.Uploaders/BaseServices/FileUploaderService.cs` | NO | `DEAD_LEGACY` | Legacy UploaderFactory service layer; not used by current desktop upload flow. |
| `src/desktop/core/XerahS.Uploaders/BaseServices/IGenericUploaderService.cs` | NO | `DEAD_LEGACY` | Legacy UploaderFactory service layer; not used by current desktop upload flow. |
| `src/desktop/core/XerahS.Uploaders/BaseServices/ImageUploaderService.cs` | NO | `DEAD_LEGACY` | Legacy UploaderFactory service layer; not used by current desktop upload flow. |
| `src/desktop/core/XerahS.Uploaders/BaseServices/IUploaderService.cs` | NO | `DEAD_LEGACY` | Legacy UploaderFactory service layer; not used by current desktop upload flow. |
| `src/desktop/core/XerahS.Uploaders/BaseServices/TextUploaderService.cs` | NO | `DEAD_LEGACY` | Legacy UploaderFactory service layer; not used by current desktop upload flow. |
| `src/desktop/core/XerahS.Uploaders/BaseServices/UploaderService.cs` | NO | `DEAD_LEGACY` | Legacy UploaderFactory service layer; not used by current desktop upload flow. |
| `src/desktop/core/XerahS.Uploaders/BaseServices/URLSharingService.cs` | NO | `DEAD_LEGACY` | Legacy UploaderFactory service layer; not used by current desktop upload flow. |
| `src/desktop/core/XerahS.Uploaders/BaseServices/URLShortenerService.cs` | NO | `DEAD_LEGACY` | Legacy UploaderFactory service layer; not used by current desktop upload flow. |
| `src/desktop/core/XerahS.Uploaders/BaseUploaders/FileUploader.cs` | YES | `USED_RUNTIME` | Used by file plugins and upload processors. |
| `src/desktop/core/XerahS.Uploaders/BaseUploaders/GenericUploader.cs` | YES | `USED_RUNTIME` | Used by plugin uploaders and upload processors. |
| `src/desktop/core/XerahS.Uploaders/BaseUploaders/ImageUploader.cs` | YES | `USED_RUNTIME` | Used by image plugins (Imgur) and shared uploader model. |
| `src/desktop/core/XerahS.Uploaders/BaseUploaders/TextUploader.cs` | YES | `USED_RUNTIME` | Used by text plugins (Gist/Paste2). |
| `src/desktop/core/XerahS.Uploaders/BaseUploaders/Uploader.cs` | YES | `USED_RUNTIME` | Core HTTP/upload base used by plugin uploaders and processors. |
| `src/desktop/core/XerahS.Uploaders/BaseUploaders/UploaderExtensions.cs` | NO | `DEAD_LEGACY` | No active call sites found. |
| `src/desktop/core/XerahS.Uploaders/BaseUploaders/URLSharer.cs` | NO | `DEAD_LEGACY` | Legacy URL-sharing service path; not called by active processor path. |
| `src/desktop/core/XerahS.Uploaders/BaseUploaders/URLShortener.cs` | NO | `DEAD_LEGACY` | URL shortening pipeline still TODO in processor; class only used by legacy services. |
| `src/desktop/core/XerahS.Uploaders/Configuration/CustomUploaderConfig.cs` | YES | `USED_COMPAT` | Polymorphic settings models used by UploadersConfig migration/serialization. |
| `src/desktop/core/XerahS.Uploaders/Configuration/DropboxConfig.cs` | YES | `USED_COMPAT` | Polymorphic settings models used by UploadersConfig migration/serialization. |
| `src/desktop/core/XerahS.Uploaders/Configuration/FtpConfig.cs` | YES | `USED_COMPAT` | Polymorphic settings models used by UploadersConfig migration/serialization. |
| `src/desktop/core/XerahS.Uploaders/Configuration/ImgurConfig.cs` | YES | `USED_COMPAT` | Polymorphic settings models used by UploadersConfig migration/serialization. |
| `src/desktop/core/XerahS.Uploaders/Configuration/S3Config.cs` | YES | `USED_COMPAT` | Polymorphic settings models used by UploadersConfig migration/serialization. |
| `src/desktop/core/XerahS.Uploaders/Configuration/UploaderType.cs` | YES | `USED_COMPAT` | Polymorphic settings models used by UploadersConfig migration/serialization. |
| `src/desktop/core/XerahS.Uploaders/CustomUploader/CustomUploaderExecutor.cs` | YES | `USED_RUNTIME` | Active runtime executor returned by CustomUploaderProvider.CreateInstance. |
| `src/desktop/core/XerahS.Uploaders/CustomUploader/CustomUploaderInput.cs` | YES | `USED_RUNTIME` | Used by active custom uploader executor/parser flow. |
| `src/desktop/core/XerahS.Uploaders/CustomUploader/CustomUploaderItem.cs` | YES | `USED_RUNTIME` | Core custom uploader model used by provider/executor/UI/config. |
| `src/desktop/core/XerahS.Uploaders/CustomUploader/CustomUploaderProvider.cs` | YES | `USED_RUNTIME` | ProviderCatalog loads this for .sxcu custom uploaders. |
| `src/desktop/core/XerahS.Uploaders/CustomUploader/CustomUploaderRepository.cs` | YES | `USED_RUNTIME` | ProviderCatalog uses this to discover/reload custom uploader files. |
| `src/desktop/core/XerahS.Uploaders/CustomUploader/CustomUploaderRequestExecutor.cs` | NO | `DEAD_LEGACY` | Only referenced by legacy Custom*Uploader services that depend on inactive UploaderFactory path. |
| `src/desktop/core/XerahS.Uploaders/CustomUploader/Functions/CustomUploaderFunction.cs` | YES | `USED_RUNTIME` | Loaded via ReflectionHelper in ShareXCustomUploaderSyntaxParser and used in custom uploader parsing. |
| `src/desktop/core/XerahS.Uploaders/CustomUploader/Functions/CustomUploaderFunctionBase64.cs` | YES | `USED_RUNTIME` | Loaded via ReflectionHelper in ShareXCustomUploaderSyntaxParser and used in custom uploader parsing. |
| `src/desktop/core/XerahS.Uploaders/CustomUploader/Functions/CustomUploaderFunctionFileName.cs` | YES | `USED_RUNTIME` | Loaded via ReflectionHelper in ShareXCustomUploaderSyntaxParser and used in custom uploader parsing. |
| `src/desktop/core/XerahS.Uploaders/CustomUploader/Functions/CustomUploaderFunctionHeader.cs` | YES | `USED_RUNTIME` | Loaded via ReflectionHelper in ShareXCustomUploaderSyntaxParser and used in custom uploader parsing. |
| `src/desktop/core/XerahS.Uploaders/CustomUploader/Functions/CustomUploaderFunctionInput.cs` | YES | `USED_RUNTIME` | Loaded via ReflectionHelper in ShareXCustomUploaderSyntaxParser and used in custom uploader parsing. |
| `src/desktop/core/XerahS.Uploaders/CustomUploader/Functions/CustomUploaderFunctionInputBox.cs` | YES | `USED_RUNTIME` | Loaded via ReflectionHelper in ShareXCustomUploaderSyntaxParser and used in custom uploader parsing. |
| `src/desktop/core/XerahS.Uploaders/CustomUploader/Functions/CustomUploaderFunctionJson.cs` | YES | `USED_RUNTIME` | Loaded via ReflectionHelper in ShareXCustomUploaderSyntaxParser and used in custom uploader parsing. |
| `src/desktop/core/XerahS.Uploaders/CustomUploader/Functions/CustomUploaderFunctionOutputBox.cs` | YES | `USED_RUNTIME` | Loaded via ReflectionHelper in ShareXCustomUploaderSyntaxParser and used in custom uploader parsing. |
| `src/desktop/core/XerahS.Uploaders/CustomUploader/Functions/CustomUploaderFunctionRandom.cs` | YES | `USED_RUNTIME` | Loaded via ReflectionHelper in ShareXCustomUploaderSyntaxParser and used in custom uploader parsing. |
| `src/desktop/core/XerahS.Uploaders/CustomUploader/Functions/CustomUploaderFunctionRegex.cs` | YES | `USED_RUNTIME` | Loaded via ReflectionHelper in ShareXCustomUploaderSyntaxParser and used in custom uploader parsing. |
| `src/desktop/core/XerahS.Uploaders/CustomUploader/Functions/CustomUploaderFunctionResponse.cs` | YES | `USED_RUNTIME` | Loaded via ReflectionHelper in ShareXCustomUploaderSyntaxParser and used in custom uploader parsing. |
| `src/desktop/core/XerahS.Uploaders/CustomUploader/Functions/CustomUploaderFunctionResponseURL.cs` | YES | `USED_RUNTIME` | Loaded via ReflectionHelper in ShareXCustomUploaderSyntaxParser and used in custom uploader parsing. |
| `src/desktop/core/XerahS.Uploaders/CustomUploader/Functions/CustomUploaderFunctionSelect.cs` | YES | `USED_RUNTIME` | Loaded via ReflectionHelper in ShareXCustomUploaderSyntaxParser and used in custom uploader parsing. |
| `src/desktop/core/XerahS.Uploaders/CustomUploader/Functions/CustomUploaderFunctionXml.cs` | YES | `USED_RUNTIME` | Loaded via ReflectionHelper in ShareXCustomUploaderSyntaxParser and used in custom uploader parsing. |
| `src/desktop/core/XerahS.Uploaders/CustomUploader/ParserSelectForm.cs` | YES | `USED_RUNTIME` | Used by CustomUploaderFunctionSelect via parser function execution. |
| `src/desktop/core/XerahS.Uploaders/CustomUploader/ShareXCustomUploaderSyntaxParser.cs` | YES | `USED_RUNTIME` | Active parser used by custom uploader execution; loads function set via reflection. |
| `src/desktop/core/XerahS.Uploaders/CustomUploader/ShareXSyntaxParser.cs` | YES | `USED_RUNTIME` | Base parser used by ShareXCustomUploaderSyntaxParser. |
| `src/desktop/core/XerahS.Uploaders/Enums.cs` | YES | `USED_RUNTIME` | Enums and flags consumed across core and plugins. |
| `src/desktop/core/XerahS.Uploaders/FileUploaders/AmazonS3Endpoint.cs` | NO | `DEAD_LEGACY` | Superseded by plugin-local AmazonS3Endpoint type; no explicit active reference to this core type. |
| `src/desktop/core/XerahS.Uploaders/FileUploaders/AmazonS3Settings.cs` | YES | `USED_COMPAT` | Legacy/compat settings object used by UploadersConfig and migration/mobile paths. |
| `src/desktop/core/XerahS.Uploaders/FileUploaders/AmazonS3StorageClass.cs` | YES | `USED_RUNTIME` | Referenced by active AmazonS3 plugin config model. |
| `src/desktop/core/XerahS.Uploaders/FileUploaders/Copy.cs` | NO | `DEAD_LEGACY` | Legacy concrete uploader implementation tied to inactive UploaderFactory service path. |
| `src/desktop/core/XerahS.Uploaders/FileUploaders/DropIO.cs` | NO | `DEAD_LEGACY` | Legacy concrete uploader implementation tied to inactive UploaderFactory service path. |
| `src/desktop/core/XerahS.Uploaders/FileUploaders/FileBin.cs` | NO | `DEAD_LEGACY` | Legacy concrete uploader implementation tied to inactive UploaderFactory service path. |
| `src/desktop/core/XerahS.Uploaders/FileUploaders/FileSonic.cs` | NO | `DEAD_LEGACY` | Legacy concrete uploader implementation tied to inactive UploaderFactory service path. |
| `src/desktop/core/XerahS.Uploaders/FileUploaders/FTPAccount.cs` | YES | `USED_COMPAT` | Legacy FTP account model still stored in UploadersConfig/workflow overrides. |
| `src/desktop/core/XerahS.Uploaders/FileUploaders/MegaAuthInfos.cs` | NO | `DEAD_LEGACY` | Legacy concrete uploader implementation tied to inactive UploaderFactory service path. |
| `src/desktop/core/XerahS.Uploaders/FileUploaders/PlikSettings.cs` | NO | `DEAD_LEGACY` | Legacy concrete uploader implementation tied to inactive UploaderFactory service path. |
| `src/desktop/core/XerahS.Uploaders/FileUploaders/PomfUploader.cs` | NO | `DEAD_LEGACY` | Legacy concrete uploader implementation tied to inactive UploaderFactory service path. |
| `src/desktop/core/XerahS.Uploaders/FileUploaders/SendSpace.cs` | NO | `DEAD_LEGACY` | Legacy concrete uploader implementation tied to inactive UploaderFactory service path. |
| `src/desktop/core/XerahS.Uploaders/FileUploaders/SendSpaceManager.cs` | NO | `DEAD_LEGACY` | Legacy concrete uploader implementation tied to inactive UploaderFactory service path. |
| `src/desktop/core/XerahS.Uploaders/FileUploaders/SFTP.cs` | NO | `DEAD_LEGACY` | Legacy concrete uploader implementation tied to inactive UploaderFactory service path. |
| `src/desktop/core/XerahS.Uploaders/FileUploaders/ShareCX.cs` | NO | `DEAD_LEGACY` | Legacy concrete uploader implementation tied to inactive UploaderFactory service path. |
| `src/desktop/core/XerahS.Uploaders/FileUploaders/Transfersh.cs` | NO | `DEAD_LEGACY` | Legacy concrete uploader implementation tied to inactive UploaderFactory service path. |
| `src/desktop/core/XerahS.Uploaders/FileUploaders/Uguu.cs` | NO | `DEAD_LEGACY` | Legacy concrete uploader implementation tied to inactive UploaderFactory service path. |
| `src/desktop/core/XerahS.Uploaders/FileUploaders/Vault_ooo.cs` | NO | `DEAD_LEGACY` | Legacy concrete uploader implementation tied to inactive UploaderFactory service path. |
| `src/desktop/core/XerahS.Uploaders/GlobalUsings.cs` | YES | `USED_RUNTIME` | Project-wide using aliases/globals required at compile time. |
| `src/desktop/core/XerahS.Uploaders/Helpers/AccountInfo.cs` | NO | `DEAD_LEGACY` | No active references found. |
| `src/desktop/core/XerahS.Uploaders/Helpers/Argument.cs` | NO | `DEAD_LEGACY` | No active references found to XerahS.Uploaders.Argument type. |
| `src/desktop/core/XerahS.Uploaders/Helpers/EscapeHelper.cs` | YES | `USED_RUNTIME` | Used by ShareXCustomUploaderSyntaxParser parsing flow. |
| `src/desktop/core/XerahS.Uploaders/Helpers/ProgressManager.cs` | YES | `USED_RUNTIME` | Used by TaskInfo/progress reporting and workflow orchestration. |
| `src/desktop/core/XerahS.Uploaders/Helpers/RequestHelpers.cs` | YES | `USED_RUNTIME` | Shared HTTP helper used by active Uploader base and plugin uploaders. |
| `src/desktop/core/XerahS.Uploaders/Helpers/ResponseInfo.cs` | YES | `USED_RUNTIME` | Response contract used by UploadResult/custom parser/uploader internals. |
| `src/desktop/core/XerahS.Uploaders/Helpers/TaskReferenceHelper.cs` | NO | `DEAD_LEGACY` | Only used by legacy service signatures; no active call sites. |
| `src/desktop/core/XerahS.Uploaders/Helpers/UploaderErrorInfo.cs` | YES | `USED_RUNTIME` | Error data model used by UploadResult/UploaderErrorManager. |
| `src/desktop/core/XerahS.Uploaders/Helpers/UploaderErrorManager.cs` | YES | `USED_RUNTIME` | Error aggregator used by UploadResult/Uploader and custom uploader parsing. |
| `src/desktop/core/XerahS.Uploaders/ImageUploaders/CheveretoUploader.cs` | YES | `USED_COMPAT` | Legacy settings model still present in UploadersConfig/import path. |
| `src/desktop/core/XerahS.Uploaders/ImageUploaders/CustomImageUploader.cs` | NO | `DEAD_LEGACY` | Legacy image uploader/service implementation not used by plugin runtime path. |
| `src/desktop/core/XerahS.Uploaders/ImageUploaders/ImageBin.cs` | NO | `DEAD_LEGACY` | Legacy image uploader/service implementation not used by plugin runtime path. |
| `src/desktop/core/XerahS.Uploaders/ImageUploaders/Img1Uploader.cs` | NO | `DEAD_LEGACY` | Legacy image uploader/service implementation not used by plugin runtime path. |
| `src/desktop/core/XerahS.Uploaders/ImageUploaders/ImmioUploader.cs` | NO | `DEAD_LEGACY` | Legacy image uploader/service implementation not used by plugin runtime path. |
| `src/desktop/core/XerahS.Uploaders/OAuth/GoogleOAuth2.cs` | NO | `DEAD_LEGACY` | No active desktop plugin references. |
| `src/desktop/core/XerahS.Uploaders/OAuth/IOAuth.cs` | NO | `DEAD_LEGACY` | Only used by legacy Copy uploader path. |
| `src/desktop/core/XerahS.Uploaders/OAuth/IOAuth2.cs` | YES | `USED_RUNTIME` | Implemented by active plugin uploaders (e.g., Imgur). |
| `src/desktop/core/XerahS.Uploaders/OAuth/IOAuth2Basic.cs` | YES | `USED_RUNTIME` | Implemented by active plugin uploaders (e.g., GitHubGist/Dropbox). |
| `src/desktop/core/XerahS.Uploaders/OAuth/IOauth2Loopback.cs` | NO | `DEAD_LEGACY` | Legacy loopback OAuth flow not used by active plugins. |
| `src/desktop/core/XerahS.Uploaders/OAuth/IOAuthBase.cs` | YES | `USED_RUNTIME` | Base auth contract used by active OAuth2 interfaces. |
| `src/desktop/core/XerahS.Uploaders/OAuth/OAuth2Info.cs` | YES | `USED_RUNTIME` | OAuth2 model used by active plugins and config viewmodels. |
| `src/desktop/core/XerahS.Uploaders/OAuth/OAuth2ProofKey.cs` | NO | `DEAD_LEGACY` | No active consumer path beyond model property; not exercised in desktop runtime. |
| `src/desktop/core/XerahS.Uploaders/OAuth/OAuth2Token.cs` | YES | `USED_RUNTIME` | OAuth2 token model used by active plugins/viewmodels. |
| `src/desktop/core/XerahS.Uploaders/OAuth/OAuthInfo.cs` | YES | `USED_COMPAT` | OAuth1-style settings model retained in UploadersConfig compatibility fields. |
| `src/desktop/core/XerahS.Uploaders/OAuth/OAuthListener.cs` | NO | `DEAD_LEGACY` | No active references from desktop plugin flow. |
| `src/desktop/core/XerahS.Uploaders/OAuth/OAuthManager.cs` | NO | `DEAD_LEGACY` | Used by legacy OAuth1 helpers and Copy path, not active plugin flow. |
| `src/desktop/core/XerahS.Uploaders/OAuth/OAuthUserInfo.cs` | YES | `USED_COMPAT` | Stored in legacy OAuth-related settings fields in UploadersConfig. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/ExplorerPage.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/ExplorerQuery.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/FileTypeScope.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/InstanceConfiguration.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/InstanceManager.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/IProviderContext.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/IProviderContextAware.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/ISecretStore.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/ISecretStoreInfo.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/IUploaderConfigViewModel.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/IUploaderExplorer.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/IUploaderPlugin.cs` | NO | `DEAD_LEGACY` | Legacy pre-provider plugin interface; no current call sites. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/IUploaderProvider.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/MediaItem.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/PluginConfigurationVerifier.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/PluginDiscovery.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/PluginLoadContext.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/PluginLoader.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/PluginManifest.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/PluginMetadata.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/PluginPackager.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/ProviderCatalog.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/ProviderIds.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/UploaderCategory.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/UploaderInstance.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/UploaderPluginBase.cs` | NO | `DEAD_LEGACY` | Legacy base class for IUploaderPlugin; not used by current providers. |
| `src/desktop/core/XerahS.Uploaders/PluginSystem/UploaderProviderBase.cs` | YES | `USED_RUNTIME` | Active plugin system surface used by ProviderCatalog/InstanceManager/UI/plugins. |
| `src/desktop/core/XerahS.Uploaders/SharingServices/BingVisualSearchSharingService.cs` | NO | `DEAD_LEGACY` | Legacy URL sharing service implementation not called by active desktop plugin flow. |
| `src/desktop/core/XerahS.Uploaders/SharingServices/CustomURLSharingService.cs` | NO | `DEAD_LEGACY` | Legacy URL sharing service implementation not called by active desktop plugin flow. |
| `src/desktop/core/XerahS.Uploaders/SharingServices/DeliciousSharingService.cs` | NO | `DEAD_LEGACY` | Legacy URL sharing service implementation not called by active desktop plugin flow. |
| `src/desktop/core/XerahS.Uploaders/SharingServices/FacebookSharingService.cs` | NO | `DEAD_LEGACY` | Legacy URL sharing service implementation not called by active desktop plugin flow. |
| `src/desktop/core/XerahS.Uploaders/SharingServices/GoogleLensSharingService.cs` | NO | `DEAD_LEGACY` | Legacy URL sharing service implementation not called by active desktop plugin flow. |
| `src/desktop/core/XerahS.Uploaders/SharingServices/LinkedInSharingService.cs` | NO | `DEAD_LEGACY` | Legacy URL sharing service implementation not called by active desktop plugin flow. |
| `src/desktop/core/XerahS.Uploaders/SharingServices/PinterestSharingService.cs` | NO | `DEAD_LEGACY` | Legacy URL sharing service implementation not called by active desktop plugin flow. |
| `src/desktop/core/XerahS.Uploaders/SharingServices/RedditSharingService.cs` | NO | `DEAD_LEGACY` | Legacy URL sharing service implementation not called by active desktop plugin flow. |
| `src/desktop/core/XerahS.Uploaders/SharingServices/SimpleURLSharingService.cs` | NO | `DEAD_LEGACY` | Legacy URL sharing service implementation not called by active desktop plugin flow. |
| `src/desktop/core/XerahS.Uploaders/SharingServices/StumbleUponSharingService.cs` | NO | `DEAD_LEGACY` | Legacy URL sharing service implementation not called by active desktop plugin flow. |
| `src/desktop/core/XerahS.Uploaders/SharingServices/TumblrSharingService.cs` | NO | `DEAD_LEGACY` | Legacy URL sharing service implementation not called by active desktop plugin flow. |
| `src/desktop/core/XerahS.Uploaders/SharingServices/VkSharingService.cs` | NO | `DEAD_LEGACY` | Legacy URL sharing service implementation not called by active desktop plugin flow. |
| `src/desktop/core/XerahS.Uploaders/TextUploaders/CustomTextUploader.cs` | NO | `DEAD_LEGACY` | Legacy text uploader/service implementation not called by active desktop plugin flow. |
| `src/desktop/core/XerahS.Uploaders/TextUploaders/Paste2.cs` | NO | `DEAD_LEGACY` | Legacy text uploader/service implementation not called by active desktop plugin flow. |
| `src/desktop/core/XerahS.Uploaders/TextUploaders/Pastebin_ca.cs` | NO | `DEAD_LEGACY` | Legacy text uploader/service implementation not called by active desktop plugin flow. |
| `src/desktop/core/XerahS.Uploaders/TextUploaders/Slexy.cs` | NO | `DEAD_LEGACY` | Legacy text uploader/service implementation not called by active desktop plugin flow. |
| `src/desktop/core/XerahS.Uploaders/UploaderFactory.cs` | NO | `DEAD_LEGACY` | No active desktop call site; legacy reflection factory path. |
| `src/desktop/core/XerahS.Uploaders/UploaderFilter.cs` | NO | `DEAD_LEGACY` | Only stored on TaskSettings; resolution depends on inactive UploaderFactory path. |
| `src/desktop/core/XerahS.Uploaders/UploadersConfig.cs` | YES | `USED_COMPAT` | Settings backbone for uploader/provider config, migration, and legacy compatibility. |
| `src/desktop/core/XerahS.Uploaders/UploadersConfigImporter.cs` | YES | `USED_COMPAT` | Used by DestinationSettingsViewModel to import ShareX UploadersConfig.json. |
| `src/desktop/core/XerahS.Uploaders/UploadersConfigValidator.cs` | NO | `DEAD_LEGACY` | No active call sites; depends on inactive UploaderFactory path. |
| `src/desktop/core/XerahS.Uploaders/UploadersLib/Properties/Resources.cs` | YES | `USED_COMPAT` | Compatibility resource strings used by legacy-compatible models/parsers/errors. |
| `src/desktop/core/XerahS.Uploaders/UploadersLib/Stubs.cs` | YES | `USED_COMPAT` | Compatibility stub types used for legacy ShareX config model compatibility and tests. |
| `src/desktop/core/XerahS.Uploaders/UploadResult.cs` | YES | `USED_RUNTIME` | Primary upload result contract used by processors/plugins. |
| `src/desktop/core/XerahS.Uploaders/URLShorteners/CustomURLShortener.cs` | NO | `DEAD_LEGACY` | Legacy URL shortener/service implementation; desktop processor still has TODO for this path. |
| `src/desktop/core/XerahS.Uploaders/URLShorteners/IsgdURLShortener.cs` | NO | `DEAD_LEGACY` | Legacy URL shortener/service implementation; desktop processor still has TODO for this path. |
| `src/desktop/core/XerahS.Uploaders/URLShorteners/NlcmURLShortener.cs` | NO | `DEAD_LEGACY` | Legacy URL shortener/service implementation; desktop processor still has TODO for this path. |
| `src/desktop/core/XerahS.Uploaders/URLShorteners/QRnetURLShortener.cs` | NO | `DEAD_LEGACY` | Legacy URL shortener/service implementation; desktop processor still has TODO for this path. |
| `src/desktop/core/XerahS.Uploaders/URLShorteners/TinyURLShortener.cs` | NO | `DEAD_LEGACY` | Legacy URL shortener/service implementation; desktop processor still has TODO for this path. |
| `src/desktop/core/XerahS.Uploaders/URLShorteners/TurlURLShortener.cs` | NO | `DEAD_LEGACY` | Legacy URL shortener/service implementation; desktop processor still has TODO for this path. |
| `src/desktop/core/XerahS.Uploaders/URLShorteners/TwoGPURLShortener.cs` | NO | `DEAD_LEGACY` | Legacy URL shortener/service implementation; desktop processor still has TODO for this path. |
| `src/desktop/core/XerahS.Uploaders/URLShorteners/VgdURLShortener.cs` | NO | `DEAD_LEGACY` | Legacy URL shortener/service implementation; desktop processor still has TODO for this path. |
| `src/desktop/core/XerahS.Uploaders/URLShorteners/VURLShortener.cs` | NO | `DEAD_LEGACY` | Legacy URL shortener/service implementation; desktop processor still has TODO for this path. |
