# One-time: merge legacy XIP*.md (old naming) into GitHub issues (masters), then remove old files.
# Master = file name that matches GitHub issue (XIP####-slug.md). Old = any other XIP*.md under tasks/.
# Usage: run from repo root: .\.ai\skills\xip-sync\scripts\merge-old-xips.ps1

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

function Get-XipNumFromTitle($title) {
    if ($title -match 'XIP[\s\-]?(\d+)') { return $Matches[1].PadLeft(4, '0') }
    return $null
}
function Get-TitleWithoutXipPrefix($title) {
    $t = $title -replace '^\[?XIP[\s\-]?\d+\]?\s*', ''
    return $t.Trim()
}
function Get-Slug($titlePart) {
    $s = $titlePart -replace '[^\w\s\-]', ''
    $s = $s -replace '\s+', '-'
    $s = $s -replace '\-+', '-'
    $s = $s.Trim('-').ToLowerInvariant()
    if ([string]::IsNullOrWhiteSpace($s)) { $s = "untitled" }
    return $s
}
function Get-XipNumFromFilename($basename) {
    if ($basename -match '^XIP(\d+)') { return $Matches[1].PadLeft(4, '0') }
    return $null
}

# Build map: xipNum -> { issueNumber, body, canonicalFileName }
$json = gh issue list --label "xip" --state all --limit 500 --json number,title,body
$issues = $json | ConvertFrom-Json
$byXipNum = @{}
foreach ($issue in $issues) {
    $xipNum = Get-XipNumFromTitle $issue.title
    if (-not $xipNum) { continue }
    $titlePart = Get-TitleWithoutXipPrefix $issue.title
    $slug = Get-Slug $titlePart
    $canonical = "XIP$xipNum-$slug.md"
    $byXipNum[$xipNum] = @{ issueNumber = $issue.number; body = $issue.body; canonicalFileName = $canonical }
}

# Find all XIP*.md under tasks/ (recursive)
$allMd = Get-ChildItem -Path $tasksRoot -Recurse -Filter "XIP*.md" -File -ErrorAction SilentlyContinue
$oldFiles = @()
foreach ($f in $allMd) {
    $base = $f.Name
    $xipNum = Get-XipNumFromFilename $base
    if (-not $xipNum) { continue }
    $canonical = $byXipNum[$xipNum].canonicalFileName
    $isInRoot = ($f.DirectoryName -eq $tasksRoot)
    if ($base -eq $canonical -and $isInRoot) { continue }
    $oldFiles += @{ path = $f.FullName; base = $base; xipNum = $xipNum }
}

if ($oldFiles.Count -eq 0) {
    Write-Host "No legacy XIP files to merge. Exiting." -ForegroundColor Cyan
    exit 0
}

Write-Host "Found $($oldFiles.Count) legacy file(s) to merge into masters and remove." -ForegroundColor Cyan

# Group by xipNum
$byNum = @{}
foreach ($o in $oldFiles) {
    $n = $o.xipNum
    if (-not $byNum[$n]) { $byNum[$n] = @() }
    $byNum[$n] += $o
}

foreach ($xipNum in ($byNum.Keys | Sort-Object)) {
    $group = $byNum[$xipNum]
    $master = $byXipNum[$xipNum]

    if ($master) {
        $body = $master.body
        if ([string]::IsNullOrWhiteSpace($body)) { $body = "" }
        foreach ($o in $group) {
            $content = [System.IO.File]::ReadAllText($o.path, [System.Text.Encoding]::UTF8)
            $body = $body + "`n`n---`n`n## Legacy content from ``$($o.base)``" + "`n`n" + $content
        }
        Write-Host "  Merging into issue #$($master.issueNumber) (XIP$xipNum): $($group.Count) file(s)" -ForegroundColor Yellow
        $bodyFile = [System.IO.Path]::GetTempFileName()
        [System.IO.File]::WriteAllText($bodyFile, $body, [System.Text.UTF8Encoding]::new($false))
        try {
            gh issue edit $master.issueNumber --body-file $bodyFile
        } finally {
            Remove-Item -Path $bodyFile -Force -ErrorAction SilentlyContinue
        }
    } else {
        $first = $group[0]
        $content = [System.IO.File]::ReadAllText($first.path, [System.Text.Encoding]::UTF8)
        $lines = $content -split "`n"
        $titlePart = "XIP$xipNum"
        foreach ($line in $lines) {
            if ($line -match '^#\s+(.+)') {
                $titlePart = $Matches[1].Trim()
                $titlePart = $titlePart -replace '^XIP\d+[\s:\-]*', ''
                $titlePart = "XIP$xipNum " + $titlePart.Trim()
                break
            }
        }
        if ($titlePart -eq "XIP$xipNum") {
            $titlePart = "XIP$xipNum " + ($first.base -replace '^XIP\d+[_\-]?', '' -replace '\.md$', '' -replace '_', ' ')
        }
        Write-Host "  Creating new issue: $titlePart (from $($first.base))" -ForegroundColor Green
        $bodyFile = [System.IO.Path]::GetTempFileName()
        [System.IO.File]::WriteAllText($bodyFile, $content, [System.Text.UTF8Encoding]::new($false))
        try {
            gh issue create --title $titlePart --label "xip" --body-file $bodyFile
        } finally {
            Remove-Item -Path $bodyFile -Force -ErrorAction SilentlyContinue
        }
    }
}

Write-Host "Running sync-from-github.ps1 ..." -ForegroundColor Cyan
& (Join-Path $PSScriptRoot "sync-from-github.ps1")

Write-Host "Removing legacy files ..." -ForegroundColor Cyan
foreach ($o in $oldFiles) {
    if (Test-Path $o.path) {
        Remove-Item -Path $o.path -Force
        Write-Host "  Deleted: $($o.base)" -ForegroundColor Gray
    }
}

foreach ($sub in @("active", "complete", "parked")) {
    $dir = Join-Path $tasksRoot $sub
    if (Test-Path $dir) {
        $left = Get-ChildItem -Path $dir -Force -ErrorAction SilentlyContinue
        if (-not $left -or $left.Count -eq 0) {
            Remove-Item -Path $dir -Force
            Write-Host "  Removed empty folder: tasks/$sub/" -ForegroundColor Gray
        }
    }
}

Write-Host "Done. Legacy content merged into GitHub; backup synced to tasks/; old files removed." -ForegroundColor Green
