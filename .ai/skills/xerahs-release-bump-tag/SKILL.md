---
name: xerahs-release-bump-tag
description: "Orchestrate XerahS release flow in strict order: maintenance-chores first, update-changelog second (optional if no CHANGELOG), verify build, then bump Directory.Build.props, commit, push, and create/push vX.Y.Z tag. Use for release and 'bump + commit + push + tag'. Supports bash scripts or PowerShell/manual steps when bash is unavailable."
---

# XerahS Release Bump Tag

## Overview

Use this skill to run release steps in strict order:
- Step 1: Run `.ai/skills/maintenance-chores/SKILL.md` first
- Step 2: Run `.ai/skills/update-changelog/SKILL.md` second (optional if no `CHANGELOG.md` exists)
- Step 3: Verify build, then execute bump/commit/push/tag automation

Step 3 performs:
- **Pre-check**: Run `dotnet build src/desktop/XerahS.sln`; do not proceed if build fails.
- Prompts for `x/y/z` bump type (major/minor/patch) unless specified
- Updates **every** `Directory.Build.props` that defines `<Version>` (root; and `ImageEditor/Directory.Build.props` if kept in sync with main—see xerahs-workflow)
- Stages all current repo changes
- Commits with version-prefixed message
- Pushes current branch and creates/pushes annotated tag `vX.Y.Z`

## Primary Command

From repository root:

```bash
./.ai/skills/xerahs-release-bump-tag/scripts/run-release-sequence.sh
```

## Non-Interactive Examples

Patch bump, no prompts:

```bash
./.ai/skills/xerahs-release-bump-tag/scripts/run-release-sequence.sh --assume-maintenance-done --assume-changelog-done --bump z --yes
```

Minor bump with custom commit token/summary:

```bash
./.ai/skills/xerahs-release-bump-tag/scripts/run-release-sequence.sh --assume-maintenance-done --assume-changelog-done --bump y --type CI --summary "Prepare release artifacts" --yes
```

Preview only:

```bash
./.ai/skills/xerahs-release-bump-tag/scripts/run-release-sequence.sh --assume-maintenance-done --assume-changelog-done --bump z --dry-run --yes
```

## When bash is unavailable (e.g. Windows PowerShell)

On environments where `bash` is not in PATH (e.g. Windows PowerShell), execute the same sequence manually:

1. **Step 1 – Maintenance**  
   From repo root: `git pull --recurse-submodules`; then `git submodule update --init --recursive`.

2. **Step 2 – Changelog**  
   Run `.ai/skills/update-changelog/SKILL.md`. Skip if the repo has no `CHANGELOG.md` or the user confirms skip.

3. **Step 3 – Bump, commit, push, tag**  
   - Run `dotnet build src/desktop/XerahS.sln`; abort if it fails.
   - Read current version from root `Directory.Build.props` (e.g. `Select-String -Path Directory.Build.props -Pattern '<Version>(.+?)</Version>'`).
   - Compute next version: patch `Z+1`, minor `Y+1.0`, major `X+1.0.0`.
   - Ensure tag `v<new-version>` does not exist locally or on `origin`; abort if it exists.
   - Update **all** `Directory.Build.props` that contain `<Version>` (root and, if applicable, `ImageEditor/Directory.Build.props`).
   - `git add -A` → `git commit -m "[v<new-version>] [CI] Release v<new-version>"` → `git push origin <current-branch>` → `git tag -a v<new-version> -m "v<new-version>"` → `git push origin v<new-version>`.

Default bump when unspecified: **patch** (`z`). Default commit type token: `CI`.

## Behavior

1. Require completion of `maintenance-chores` first.
2. Require completion of `update-changelog` second (skip if no `CHANGELOG.md` or user confirms).
3. Before bump: run `dotnet build src/desktop/XerahS.sln`; abort on failure.
4. Run `scripts/bump-version-commit-tag.sh` (or the PowerShell/manual equivalent when bash is unavailable) as the final step:
- Resolve current version from root `Directory.Build.props`.
- Compute next version from bump type:
  - `x`/`major`: `X+1.0.0`
  - `y`/`minor`: `X.Y+1.0`
  - `z`/`patch`: `X.Y.Z+1`
- Verify tag `v<new-version>` does not already exist locally or on `origin`.
- Update every `Directory.Build.props` that defines `<Version>` (root; and `ImageEditor/Directory.Build.props` if kept in sync).
- Run `git add -A`.
- Commit as: `[v<new-version>] [<type>] <summary>`.
- Push current branch to `origin`.
- Create and push annotated tag `v<new-version>`.

## Guardrails

- Do not skip the sequence unless user explicitly requests bypass flags.
- In the pre-release sequence, avoid committing/pushing during maintenance/changelog steps.
- **Always verify build** (`dotnet build src/desktop/XerahS.sln`) before step 3; abort if build fails.
- Abort on detached HEAD.
- Abort if version format is not `X.Y.Z`.
- Abort if matching tag already exists locally or remotely.
- Support `--no-push` and `--no-tag` when partial flow is needed.
- When bash is unavailable, use the PowerShell/manual steps in "When bash is unavailable" rather than failing.

## Agent usage (Cursor / Codex)

When executing this skill:
1. Run the sequence in order: maintenance-chores → update-changelog (or skip if no CHANGELOG) → verify build → bump/commit/push/tag.
2. If the environment has **bash**, use `run-release-sequence.sh` and `bump-version-commit-tag.sh`; otherwise use the **"When bash is unavailable"** steps (e.g. PowerShell).
3. If bump type is not specified, default to **patch** (`z`) or ask the user.
4. Report at the end: new version, commit hash, branch push status, tag push status.

## Notes (lessons learnt)

- **Windows/PowerShell**: Many CI and dev environments (e.g. Cursor on Windows) do not have `bash` in PATH; the skill must document an equivalent flow so agents can complete the release without bash.
- **Build before bump**: Tagging a broken tree is costly; always run `dotnet build src/desktop/XerahS.sln` before step 3 and abort on failure.
- **Changelog optional**: Repositories may not yet have `CHANGELOG.md`; step 2 is optional in that case. Do not block the release on it unless the user requires it.
- **Version sync**: xerahs-workflow requires updating every `Directory.Build.props` that holds the app version (e.g. root and ImageEditor when kept in sync); the bash script only touches the root file—agents doing the flow manually or via a future PowerShell script should update all such files.
