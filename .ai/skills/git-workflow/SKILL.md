---
name: git-workflow
description: Commit, push, or version XerahS changes. Use only when the task includes these operations.
---

# Git and versioning

Follow the wrapper and fallback guidance in [AGENTS.md](../../../AGENTS.md).

## Version and commit format

- Root `Directory.Build.props` supplies the app version. Before an app commit, compare it with the highest existing XerahS release tag; the version must be strictly greater. If needed, bump it to the next unreleased version.
- App commits use `[vX.Y.Z] [Type] Concise description`, with types such as `Fix`, `Feature`, `Build`, `Docs`, or `Refactor`.
- Shared-library/submodule commits, including `ShareX.ImageEditor`, use `[Type] Concise description` without the app version.
- For requested version increments: fixes increment patch; features increment minor and reset patch; breaking changes increment major and reset minor/patch.
- Do not put app versions in individual project files. Synchronize other tracked props only if they intentionally carry the XerahS app version; do not overwrite independent library versions.
- Release automation synchronizes derived Chocolatey metadata and validates the generated package.

## Verification and integration

Inspect status, the task diff, tracking branch, and submodule state before staging. Preserve unrelated edits. Pull/rebase or update submodules when integration requires it, after checking that local work will be preserved; do not unconditionally pull into a dirty checkout.

Before pushing code or build configuration, run the desktop solution build required by `AGENTS.md` and relevant tests. Do not disable warnings-as-errors.

Exceptions:
- Documentation-only changes: check the diff, links, and consistency. Run relevant script checks if executable instructions or scripts changed.
- Version-only root `Directory.Build.props` bump: verify that the task diff contains only the intended version change and that the commit prefix matches.

## Finish

For authorized commit/push work, stage the task's explicit paths, inspect the staged diff, commit, then push to the verified remote and branch. Do not stage unrelated work with `add .`. Include related documentation in the same logical change.

Preserve configured hooks; fix hook failures and retry. Do not bypass hooks unless explicitly requested. Never create a branch or tag implicitly.
