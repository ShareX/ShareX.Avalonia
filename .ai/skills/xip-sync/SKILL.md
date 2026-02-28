---
name: xip-sync
description: Create and maintain XerahS Improvement Proposals (XIPs) with GitHub as source of truth and tasks folder as backup. Use when creating or editing XIPs, syncing XIPs between GitHub issues and the tasks folder, or when the user mentions XIP, GitHub issues for XIP, or tasks backup.
---

# XIP Sync Skill

**Source of truth**: GitHub issues (label `xip`).  
**Backup**: `tasks/` folder (generated from GitHub via sync).

Create and edit XIPs in GitHub; keep a local backup in `tasks/` by running sync. Do not treat the tasks folder as the primary place to write XIPs.

---

## Principles

1. **Create and edit XIPs in GitHub** – Use `gh issue create` / `gh issue edit` (or the GitHub UI). The issue body holds the full XIP markdown. Status (open/closed/parked) lives only in GitHub via issue state and labels.
2. **Sync GitHub → tasks** – Run the sync script to write one `.md` file per XIP under `tasks/` (single folder). Files do not move; status is not reflected in paths.
3. **Tasks folder is read-only for XIP content** – Do not edit XIP content in the tasks folder; edit the GitHub issue, then sync.
4. **Recovery** – If the only copy of a XIP is in tasks, create a new issue with that content and then sync.

---

## Workflows

### Create a new XIP

1. **Choose the next XIP number**  
   - List existing: `gh issue list --label xip --limit 500 --json number,title`  
   - Or check highest in `tasks/**/*.md` (e.g. XIP0044).

2. **Draft the XIP body**  
   - Use the structure in [XIP writing reference](.ai/skills/xip-writing/SKILL.md): Overview, Prerequisites, Implementation Phases, Non-Negotiable Rules, Deliverables, Affected Components.  
   - **Title format**: `XIP0044 Short Descriptive Title` (4-digit zero-padded number, single space, no brackets, no colon, no dash).

3. **Create the GitHub issue**  
   - Title: `XIP0044 Short Descriptive Title`  
   - Body: full XIP markdown (no wrapper; the body is the XIP).  
   - Label: `xip`. Add `parked` if the XIP is parked.

   ```powershell
   gh issue create --title "XIP0044 Your Title" --label "xip" --body-file path/to/draft.md
   ```

4. **Sync to tasks**  
   - Run: `./.ai/skills/xip-sync/scripts/sync-from-github.ps1`  
   - The new XIP appears as `tasks/XIP####-Title-Slug.md`.

### Edit an existing XIP

1. **Edit on GitHub**  
   - `gh issue edit <number> --title "XIP0044 New Title" --body-file path/to/updated.md`  
   - Or edit title/body in the GitHub issue in the browser.

2. **Sync to tasks**  
   - Run `./.ai/skills/xip-sync/scripts/sync-from-github.ps1` so the backup in `tasks/` is updated.

### Sync GitHub → tasks (backup)

Run from repo root:

```powershell
.\.ai\skills\xip-sync\scripts\sync-from-github.ps1
```

- Reads all issues with label `xip`.
- Writes/overwrites one `.md` file per XIP under **`tasks/`** (single folder). Status is not synced to paths; it stays in GitHub (issue state and labels).
- Filename: `XIP####-Title-Slug.md` (number from title, rest from slug of title).
- File content: issue body only (no extra “issue” wrapper). If the body contains a “XIP Document” block from an old migration, the script strips it and uses the actual XIP content.

### Recovery: tasks → GitHub

If the only good copy of a XIP is in the tasks folder:

1. Create a new issue with that file as the body and label `xip`:
   ```powershell
   gh issue create --title "XIP0044 Title From File" --label "xip" --body-file "tasks/XIP0044-Something.md"
   ```
2. Run sync so the backup is consistent with GitHub.

---

## Backup layout

- All XIP backup files live in **`tasks/`** as `XIP####-Title-Slug.md`. Status (open/closed/parked) is **not** reflected in folder structure; it lives only in GitHub issues (state and labels). This avoids moving files and breaking links when status changes.

---

## XIP naming (quick reference)

- **Issue title and first heading**: `XIP0044 Short Descriptive Title`  
  - 4-digit zero-padded number, single space, no `[ ]`, no `:`, no `-` between number and title.
- **File name**: `XIP0044-Short-Descriptive-Title.md` (number + slug with hyphens).

Full structure, templates, and patterns: [.ai/skills/xip-writing/SKILL.md](.ai/skills/xip-writing/SKILL.md).

---

## Script location

- **Sync (GitHub → tasks)**: `.ai/skills/xip-sync/scripts/sync-from-github.ps1`
- **One-time merge of legacy files**: `.ai/skills/xip-sync/scripts/merge-old-xips.ps1` – merges old-named `XIP*.md` (e.g. in `tasks/complete/`) into the corresponding GitHub issue body, runs sync, then deletes the old files. Use after migrating to single-folder backup or when cleaning duplicates.

Run from repo root; requires `gh` CLI and PowerShell.

---

## Key takeaways

1. **GitHub first** – Create and edit XIPs as issues (label `xip`); issue body = full XIP.
2. **Tasks = backup** – One folder (`tasks/`); status only in GitHub. Run `sync-from-github.ps1` after changes.
3. **Don’t edit XIP content in tasks** – Edit the issue, then sync.
4. **Naming** – `XIP0044 Title` (no brackets/colon/dash); file `XIP0044-Title-Slug.md`.
