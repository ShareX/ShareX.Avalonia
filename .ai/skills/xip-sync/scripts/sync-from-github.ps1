# Sync XIP GitHub issues -> tasks folder (backup). Single folder; status stays in GitHub.
# Source of truth: GitHub. Backup: tasks/*.md (one folder, no status-based paths).
# Usage: run from repo root: .\.ai\skills\xip-sync\scripts\sync-from-github.ps1

$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot
for ($i = 0; $i -lt 5; $i++) {
    $repoRoot = Split-Path -Parent $repoRoot
    if (Test-Path (Join-Path $repoRoot ".git")) { break }
}
if (-not (Test-Path (Join-Path $repoRoot ".git"))) {
    Write-Error "Repo root not found. Run from XerahS repo."
}
$tasksRoot = Join-Path $repoRoot "tasks"
if (-not (Test-Path $tasksRoot)) { New-Item -ItemType Directory -Path $tasksRoot -Force | Out-Null }
Get-ChildItem -Path $tasksRoot -Filter "XIP*.md" -File | Remove-Item -Force

# Fetch all issues with label xip
$json = gh issue list --label "xip" --state all --limit 500 --json number,title,body,state,labels
$issues = $json | ConvertFrom-Json

function Get-XipNumberFromTitle($title) {
    if ($title -match 'XIP[\s\-]?(\d+)') { return $Matches[1].PadLeft(4, '0') }
    return $null
}

function Get-TitleWithoutXipPrefix($title) {
    $t = $title -replace '^\[?XIP[\s\-]?\d+\]?\s*', ''
    $t = $t.Trim()
    return $t
}

function Get-Slug($titlePart) {
    # ASCII-only slug to avoid encoding/mojibake in filenames (e.g. apostrophe -> gamma c circumflex)
    $s = $titlePart -replace '[^\x20-\x7E]', '-'   # non-ASCII -> hyphen
    $s = $s -replace '[^a-zA-Z0-9\s\-]', ''        # keep only letters, digits, space, hyphen
    $s = $s -replace '\s+', '-'
    $s = $s -replace '\-+', '-'
    $s = $s.Trim('-').ToLowerInvariant()
    if ([string]::IsNullOrWhiteSpace($s)) { $s = "untitled" }
    return $s
}

function Get-BodyContent($body) {
    if ([string]::IsNullOrWhiteSpace($body)) { return $body }
    # Old migration wrapped content with "## XIP Document" and "---"
    if ($body -match '(?s)##\s*XIP\s*Document\s*.*?^---\s*') {
        $body = $body -replace '(?s)^.*?^---\s*', '', 1
    }
    return $body.Trim()
}

$written = 0
$skipped = 0

foreach ($issue in $issues) {
    $num = Get-XipNumberFromTitle $issue.title
    $titlePart = Get-TitleWithoutXipPrefix $issue.title
    if (-not $num) {
        Write-Host "Skipping #$($issue.number): title has no XIP number - $($issue.title)" -ForegroundColor Yellow
        $skipped++
        continue
    }

    $slug = Get-Slug $titlePart
    $fileName = "XIP$num-$slug.md"
    $outPath = Join-Path $tasksRoot $fileName
    $content = Get-BodyContent $issue.body

    # Ensure first line is # XIP#### Title (canonical)
    $firstLine = "# XIP$num $titlePart"
    $lines = $content -split "`n"
    if ($lines.Count -gt 0 -and $lines[0] -match '^#\s+') {
        $rest = if ($lines.Count -gt 1) { $lines[1..($lines.Count - 1)] -join "`n" } else { "" }
        $content = $firstLine + "`n" + $rest.TrimStart()
    } else {
        $content = $firstLine + "`n`n" + $content
    }

    [System.IO.File]::WriteAllText($outPath, $content, [System.Text.UTF8Encoding]::new($false))
    Write-Host "  $fileName -> tasks/" -ForegroundColor Green
    $written++
}

Write-Host ""
Write-Host "Synced $written XIP(s) to tasks. Skipped $skipped." -ForegroundColor Cyan
