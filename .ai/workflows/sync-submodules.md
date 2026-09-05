---
description: Update explicitly requested submodules to a chosen remote revision.
---

# Sync submodules

Use the operator's Git wrapper and [Git workflow](../skills/git-workflow/SKILL.md).

1. Inspect parent and requested submodule status, configured remotes, branch tracking, and local changes. A detached submodule HEAD is normal for a pinned dependency.
2. Resolve the requested revision or tracked branch tip. Preserve local submodule work before updating; do not update every submodule implicitly.
3. Update only the named submodule, inspect the resulting gitlink and source diff, and run the relevant integration build/tests.
4. For authorized commit/push work, stage only the changed gitlink and related task files, use the app's next unreleased version prefix, and push through the verified wrapper.

Building a pinned checkout does not require updating submodules to remote HEAD.
