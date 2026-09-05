---
name: sync-xips
description: Synchronize XIP GitHub issues and local backups when requested. Do not trigger for reading or implementing an XIP.
---

# XIP Sync Skill

**Source of truth**: GitHub issues (label `xip`).  
**Backup**: `docs/proposals/xip/` folder (generated from GitHub via sync).

For requested issue creation or updates, edit XIPs in GitHub and sync the local backup. Reading, auditing, or implementing an XIP does not authorize issue edits, label repairs, or recovery uploads. Apply the mutation steps below only when requested; otherwise report differences.

For XIP structure, templates, and writing patterns, use [XIP writing reference](../write-xip/SKILL.md). This skill is the operational workflow for issue creation, issue editing, sync, and recovery.

---

## Principles

1. **Create and edit XIPs in GitHub** – Use `gh issue create` / `gh issue edit` (or the GitHub UI). The issue body holds the full XIP markdown. Status (open/closed/parked) lives only in GitHub via issue state and labels.
2. **Sync GitHub → docs/proposals/xip** – Run the sync script to write one `.md` file per XIP under `docs/proposals/xip/` (single folder). Files do not move; status is not reflected in paths.
3. **docs/proposals/xip folder is read-only for XIP content** – Do not edit XIP content in the local folder; edit the GitHub issue, then sync.
4. **Recovery** – If the only copy of a XIP is in `docs/proposals/xip/`, create a new issue with that content and then sync.

---

## Workflows

### Audit local XIPs against GitHub issues

Use this when the user says the local `docs/proposals/xip/` folder has XIPs that are missing from GitHub.

1. **Compare by canonical ID only**
   - Extract local IDs from filenames matching `XIP####`.
   - Fetch **all** GitHub issues, not just issues with the `xip` label, because existing XIP issues can be missing the label.
   - Extract `XIP####` from issue titles and ignore all other title text differences.
   - Treat a GitHub issue as present if any title contains the same `XIP####`, even if punctuation, casing, or wording differs.

   ```powershell
   $repo = "ShareX/XerahS"
   $xipDir = "docs/proposals/xip"
   $issues = gh issue list --repo $repo --state all --limit 500 --json number,title,url,state,labels | ConvertFrom-Json
   $issueIds = New-Object "System.Collections.Generic.HashSet[string]"
   foreach ($issue in $issues) {
       foreach ($match in [regex]::Matches($issue.title, "XIP[0-9]{4}")) {
           [void]$issueIds.Add($match.Value)
       }
   }
   $localIds = Get-ChildItem -LiteralPath $xipDir -Filter "XIP*.md" |
       ForEach-Object { if ($_.Name -match "^(XIP[0-9]{4})") { $Matches[1] } } |
       Sort-Object -Unique
   $missing = $localIds | Where-Object { -not $issueIds.Contains($_) }
   $missing
   ```

2. **Repair labels before running label-based sync**
   - If an issue title contains `XIP####` but does not have the `xip` label, add the label instead of creating a duplicate issue.
   - This matters because `sync-from-github.ps1` reads only issues with label `xip`.

   ```powershell
   foreach ($issue in $issues) {
       if ($issue.title -match "XIP[0-9]{4}") {
           $labelNames = $issue.labels | ForEach-Object { $_.name }
           if (-not ($labelNames -contains "xip")) {
               gh issue edit $issue.number --repo $repo --add-label "xip"
           }
       }
   }
   ```

3. **Create only true missing issues**
   - For each local-only `XIP####`, create one issue using the matching local file as the body and label it `xip`.
   - Build the issue title from the file's first markdown heading when available.
   - Normalize the title to `XIP#### Short Descriptive Title`: single space after the ID; no brackets, colon, or dash after the ID.

4. **Verify after creation**
   - Re-fetch all issues and repeat the ID-only comparison.
   - Confirm local ID count and GitHub issue ID count match.
   - Confirm every issue whose title contains `XIP####` has the `xip` label.
   - Check `git status --short`; uploading missing issues should not leave local file changes unless you intentionally ran the sync script.

5. **Be deliberate with local sync**
   - `sync-from-github.ps1` removes and rewrites `docs/proposals/xip/XIP*.md` from label-matched GitHub issues. It can rename files and rewrite old bodies, causing broad local churn.
   - Do not run the sync script just to upload missing local XIPs unless the user also wants the backup folder normalized from GitHub.
   - If you do run it to verify label-based sync, inspect `git status --short` and `git diff --stat -- docs/proposals/xip` afterwards. Keep or discard the generated backup churn deliberately.

### Create a new XIP

1. **Choose the next XIP number**  
   - List existing across all issues when choosing or auditing IDs: `gh issue list --repo ShareX/XerahS --state all --limit 500 --json number,title,labels`
   - Do not rely only on `--label xip` for ID discovery; it can miss an existing XIP issue whose label was accidentally omitted.
   - Or check highest in `docs/proposals/xip/*.md` (e.g. XIP0044).

2. **Draft the XIP body**  
   - Use the structure in [XIP writing reference](../write-xip/SKILL.md): Overview, Prerequisites, Implementation Phases, Non-Negotiable Rules, Deliverables, Affected Components.  
   - **Title format**: `XIP0044 Short Descriptive Title` (4-digit zero-padded number, single space, no brackets, no colon, no dash).

3. **Create the GitHub issue**  
   - Title: `XIP0044 Short Descriptive Title`  
   - Body: full XIP markdown (no wrapper; the body is the XIP).  
   - Label: `xip`. Add `parked` if the XIP is parked.

   ```powershell
   gh issue create --title "XIP0044 Your Title" --label "xip" --body-file path/to/draft.md
   ```

4. **Sync to docs/proposals/xip**  
   - Run: `./.ai/skills/sync-xips/scripts/sync-from-github.ps1`  
   - The new XIP appears as `docs/proposals/xip/XIP####-title-slug.md`.

### Edit an existing XIP

1. **Edit on GitHub**  
   - `gh issue edit <number> --title "XIP0044 New Title" --body-file path/to/updated.md`  
   - Or edit title/body in the GitHub issue in the browser.

2. **Sync to docs/proposals/xip**  
   - Run `./.ai/skills/sync-xips/scripts/sync-from-github.ps1` so the backup in `docs/proposals/xip/` is updated.

### Sync GitHub → docs/proposals/xip (backup)

Run from repo root:

```powershell
.\.ai\skills\sync-xips\scripts\sync-from-github.ps1
```

- Reads all issues with label `xip`.
- Writes/overwrites one `.md` file per XIP under **`docs/proposals/xip/`** (single folder). Status is not synced to paths; it stays in GitHub (issue state and labels).
- Filename: `XIP####-title-slug.md` (number from title, rest from lower-case slug of title).
- File content: issue body only (no extra “issue” wrapper). If the body contains a “XIP Document” block from an old migration, the script strips it and uses the actual XIP content.

### Recovery: docs/proposals/xip → GitHub

If the only good copy of a XIP is in the local folder:

1. Create a new issue with that file as the body and label `xip`:
   ```powershell
   gh issue create --title "XIP0044 Title From File" --label "xip" --body-file "docs/proposals/xip/XIP0044-Something.md"
   ```
2. Run sync so the local copy is consistent with GitHub.

---

## Backup layout

- All XIP backup files live in **`docs/proposals/xip/`** as `XIP####-title-slug.md`. Status (open/closed/parked) is **not** reflected in folder structure; it lives only in GitHub issues (state and labels). This avoids moving files and breaking links when status changes.

---

## XIP naming (quick reference)

- **Issue title and first heading**: `XIP0044 Short Descriptive Title`  
  - 4-digit zero-padded number, single space, no `[ ]`, no `:`, no `-` between number and title.
- **File name**: `XIP0044-short-descriptive-title.md` (number + lower-case slug with hyphens).

Full structure, templates, and patterns: [XIP writing reference](../write-xip/SKILL.md).

---

## Script location

- **Sync (GitHub → docs/proposals/xip)**: `.ai/skills/sync-xips/scripts/sync-from-github.ps1`
- **One-time merge of legacy files**: `.ai/skills/sync-xips/scripts/merge-old-xips.ps1` – merges old-named `XIP*.md` (e.g. in `docs/proposals/xip/`) into the corresponding GitHub issue body, runs sync, then deletes the old files. Use after migrating to single-folder backup or when cleaning duplicates.

Run from repo root; requires `gh` CLI and PowerShell.

---

## Key takeaways

1. **GitHub first** – Create and edit XIPs as issues (label `xip`); issue body = full XIP.
2. **docs/proposals/xip = backup** – One folder (`docs/proposals/xip/`); status only in GitHub. Run `sync-from-github.ps1` after changes.
3. **Don't edit XIP content locally** – Edit the issue, then sync.
4. **Naming** – `XIP0044 Title` (no brackets/colon/dash); file `XIP0044-title-slug.md`.
