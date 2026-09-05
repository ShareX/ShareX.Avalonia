# Astra instruction audit — 2026-09-06

Applied the user's supplied astra-ready guidance. This is a review record, not an instruction entry point.

## Changes

- Preserved the mandatory four-person wrapper table, whoami before push, and verified fallback. Restored Windows PowerShell sequencing guidance.
- Consolidated build, version, and Git rules. Defined documentation-only verification and the previously missing version-only exception.
- Removed mandatory graph reading from Cursor and kickoff prompts, fixed host pointer files, and kept skill loading task-specific.
- Narrowed skill descriptions, separated XIP reading from GitHub mutations, and kept diagnosis from automatically becoming implementation.
- Shortened generic frontend and refactoring guidance in place. Retained all 24 skill entry paths to preserve callers; no skill deletion was performed.
- Removed blanket process kills, sample-PID termination, and mandatory submodule updates from packaging instructions. Executable scripts and product code were not changed.

## Inventory

Sizes are approximate current line counts. Skill folder names identify the workflow; exact routing descriptions live only in the corresponding frontmatter.

| Path | Loading | Lines | Trigger / audience | Verdict |
|---|---|---:|---|---|
| `.ai/README.md` | On-demand | 12 | Named workflow | Slim / consolidate |
| `.ai/instructions.md` | Always-on | 1 | Every task on matching host | Slim / consolidate |
| `.ai/skills/architecture-guidelines/SKILL.md` | On-demand | 101 | architecture-guidelines | Keep; narrow routing |
| `.ai/skills/audit-refactoring/SKILL.md` | On-demand | 12 | audit-refactoring | Keep; narrow routing |
| `.ai/skills/avalonia-guidelines/SKILL.md` | On-demand | 1378 | avalonia-guidelines | Keep; narrow routing |
| `.ai/skills/build-android/SKILL.md` | On-demand | 282 | build-android | Keep; narrow routing |
| `.ai/skills/build-common/SKILL.md` | On-demand | 24 | build-common | Keep; narrow routing |
| `.ai/skills/build-linux-binary/SKILL.md` | On-demand | 220 | build-linux-binary | Keep; narrow routing |
| `.ai/skills/build-windows-exe/SKILL.md` | On-demand | 209 | build-windows-exe | Keep; narrow routing |
| `.ai/skills/coding-standards/SKILL.md` | On-demand | 93 | coding-standards | Keep; narrow routing |
| `.ai/skills/create-uploader-plugin/SKILL.md` | On-demand | 91 | create-uploader-plugin | Keep; narrow routing |
| `.ai/skills/design-ui-window/SKILL.md` | On-demand | 208 | design-ui-window | Keep; narrow routing |
| `.ai/skills/draft-blog-post/SKILL.md` | On-demand | 190 | draft-blog-post | Keep; narrow routing |
| `.ai/skills/feature-specifications/SKILL.md` | On-demand | 217 | feature-specifications | Keep; narrow routing |
| `.ai/skills/frontend-design/SKILL.md` | On-demand | 13 | frontend-design | Keep; narrow routing |
| `.ai/skills/git-workflow/SKILL.md` | On-demand | 33 | git-workflow | Keep; narrow routing |
| `.ai/skills/graphify/SKILL.md` | On-demand | 82 | graphify | Keep; narrow routing |
| `.ai/skills/mobile-android-ios-parity/SKILL.md` | On-demand | 175 | mobile-android-ios-parity | Keep; narrow routing |
| `.ai/skills/mobile-experimental-ios-parity/SKILL.md` | On-demand | 145 | mobile-experimental-ios-parity | Keep; narrow routing |
| `.ai/skills/port-imageeditor/SKILL.md` | On-demand | 688 | port-imageeditor | Keep; narrow routing |
| `.ai/skills/publish-release/SKILL.md` | On-demand | 338 | publish-release | Keep; narrow routing |
| `.ai/skills/run-maintenance/SKILL.md` | On-demand | 135 | run-maintenance | Keep; narrow routing |
| `.ai/skills/sync-xips/SKILL.md` | On-demand | 174 | sync-xips | Keep; narrow routing |
| `.ai/skills/triage-runtime-logs/SKILL.md` | On-demand | 78 | triage-runtime-logs | Keep; narrow routing |
| `.ai/skills/update-changelog/SKILL.md` | On-demand | 355 | update-changelog | Keep; narrow routing |
| `.ai/skills/write-xip/SKILL.md` | On-demand | 406 | write-xip | Keep; narrow routing |
| `.ai/workflows/sync-submodules.md` | On-demand | 14 | Named workflow | Slim / consolidate |
| `.antigravity/rules.md` | Always-on | 1 | Every task on matching host | Slim / consolidate |
| `.autoresearch/missions/xerahs-bugfix.md` | On-demand | 38 | Named workflow | Keep; task-specific or inert |
| `.autoresearch/missions/xerahs-filter-heat-haze-refraction.md` | On-demand | 42 | Named workflow | Keep; task-specific or inert |
| `.autoresearch/missions/xerahs-filter-luminance-contour-lines.md` | On-demand | 45 | Named workflow | Keep; task-specific or inert |
| `.autoresearch/missions/xerahs-filter-nebula-starfield.md` | On-demand | 42 | Named workflow | Keep; task-specific or inert |
| `.autoresearch/missions/xerahs-filter-paper-stencil-mask.md` | On-demand | 44 | Named workflow | Keep; task-specific or inert |
| `.autoresearch/missions/xerahs-filter-riso-print.md` | On-demand | 43 | Named workflow | Keep; task-specific or inert |
| `.autoresearch/missions/xerahs-smart-post-upload.md` | On-demand | 33 | Named workflow | Keep; task-specific or inert |
| `.codex` | On-demand | 0 | Named workflow | Keep; task-specific or inert |
| `.cursor/rules/graphify.mdc` | On-demand | 6 | Named workflow | Slim / consolidate |
| `.cursorrules` | Always-on | 1 | Every task on matching host | Slim / consolidate |
| `.github/copilot-instructions.md` | Always-on | 1 | Every task on matching host | Slim / consolidate |
| `.windsurfrules` | Always-on | 1 | Every task on matching host | Slim / consolidate |
| `AGENTS.md` | Always-on | 34 | Every task on matching host | Slim / consolidate |
| `CLAUDE.md` | Always-on | 3 | Every task on matching host | Slim / consolidate |
| `developers/guidelines/AGENT_WORKFLOW.md` | On-demand | 27 | Named workflow | Slim / consolidate |
| `developers/guidelines/GRAPHIFY_AGENT_PROMPT.md` | On-demand | 13 | Named workflow | Slim / consolidate |
| `developers/linux/debug_prompt.md` | On-demand | 41 | Named workflow | Keep; task-specific or inert |
| `developers/prompts/derive-goal-from-session.md` | On-demand | 24 | Named workflow | Slim / consolidate |
| `developers/prompts/feature-systems-thinking.md` | On-demand | 12 | Named workflow | Slim / consolidate |
| `developers/prompts/first-party-cli-or-plugin.md` | On-demand | 61 | Named workflow | Keep; task-specific or inert |
| `developers/prompts/implement-xip-end-to-end.md` | On-demand | 20 | Named workflow | Slim / consolidate |
| `docs/architecture/MULTI_AGENT_COORDINATION.md` | On-demand | 13 | Named workflow | Slim / consolidate |

## Coverage and retained material

Tracked host rules, repository skills, developer prompts, workflow docs, issue templates, and CI agent references were inventoried. Generated autoresearch worktrees, vendored dependencies, binaries, and independent shared-library checkouts were excluded from rewriting.

Skill scripts/assets and supporting references were retained; operational side effects were inspected where relevant, but no release/deploy/sync helper was executed. Large Avalonia, ImageEditor-port, release, and XIP references retain domain details; this audit does not certify every historical example as current.

The feature-specifications skill remains available as an explicitly requested contract reference; its PROJECT_STATUS link was repaired. Generic frontend guidance was shortened in place rather than deleting a publicly referenced skill path. Historical debug and autoresearch mission prompts remain task-specific and are not linked from always-on instructions.

## Verification

- Git diff whitespace check.
- Repository Markdown hygiene checker against changed Markdown.
- New relative-link targets and required wrapper mapping checks.
- Static inspection of documented build commands and project paths; removed destructive examples were not executed.
- No application build or full test suite: changes are instruction text, routing metadata, and documentation.

Unrelated pre-existing memory-management source/test changes were preserved. No commit, push, release, or issue mutation was performed.
