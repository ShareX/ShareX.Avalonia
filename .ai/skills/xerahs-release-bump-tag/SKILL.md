---
name: xerahs-release-bump-tag
description: "Orchestrate XerahS release flow in strict order: run maintenance-chores skill first, run update-changelog skill second, then execute Directory.Build.props version bump with git commit/push and matching vX.Y.Z tag creation. Use for release retry and 'bump + commit + push + tag' requests."
---

# XerahS Release Bump Tag

## Overview

Use this skill to run release steps in strict order:
- Step 1: Run `.ai/skills/maintenance-chores/SKILL.md` first
- Step 2: Run `.ai/skills/update-changelog/SKILL.md` second
- Step 3: Execute bump/commit/push/tag automation

Step 3 performs:
- Prompts for `x/y/z` bump type (major/minor/patch)
- Updates `Directory.Build.props` version
- Stages all current repo changes
- Commits with version-prefixed message
- Pushes branch and matching `vX.Y.Z` tag

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

## Behavior

1. Require completion of `maintenance-chores` first.
2. Require completion of `update-changelog` second.
3. Run `scripts/bump-version-commit-tag.sh` as the final step:
- Resolve current version from `Directory.Build.props`.
- Compute next version from bump type:
  - `x`/`major`: `X+1.0.0`
  - `y`/`minor`: `X.Y+1.0`
  - `z`/`patch`: `X.Y.Z+1`
- Verify tag `v<new-version>` does not already exist locally or on `origin`.
- Update `Directory.Build.props`.
- Run `git add -A`.
- Commit as: `[v<new-version>] [<type>] <summary>`.
- Push current branch to `origin`.
- Create and push annotated tag `v<new-version>`.

## Guardrails

- Do not skip the sequence unless user explicitly requests bypass flags.
- In the pre-release sequence, avoid committing/pushing during maintenance/changelog steps.
- Abort on detached HEAD.
- Abort if version format is not `X.Y.Z`.
- Abort if matching tag already exists locally or remotely.
- Support `--no-push` and `--no-tag` when partial flow is needed.

## Codex Usage Notes

When using this skill in a Codex run:
1. Execute this sequence exactly:
- `.ai/skills/maintenance-chores/SKILL.md`
- `.ai/skills/update-changelog/SKILL.md`
- `.ai/skills/xerahs-release-bump-tag/scripts/bump-version-commit-tag.sh`
2. If bump type is not specified, ask whether to bump `x`, `y`, or `z`.
3. Report resulting version, commit hash, branch push status, and tag push status.
