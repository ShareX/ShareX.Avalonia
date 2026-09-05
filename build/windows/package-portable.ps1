<#
.SYNOPSIS
Creates a portable ZIP from a clean, self-contained Windows publish directory.
.DESCRIPTION
Does not alter the publish directory, so it can also feed EXE/MSI installers.
The portable marker is added only to the ZIP; debug symbols are omitted.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PublishDirectory,
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+(?:[-+][0-9A-Za-z.-]+)?$')]
    [string]$Version,
    [Parameter(Mandatory = $true)]
    [ValidateSet('win-x64', 'win-arm64')]
    [string]$Runtime,
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$licensePath = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'LICENSE.txt'
if (!(Test-Path -LiteralPath $licensePath -PathType Leaf)) {
    throw "Repository license is missing: $licensePath"
}

$source = (Resolve-Path -LiteralPath $PublishDirectory).Path.TrimEnd('\', '/')
$destination = [System.IO.Path]::GetFullPath($OutputDirectory)
if ($destination.Equals($source, [StringComparison]::OrdinalIgnoreCase) -or
    $destination.StartsWith($source + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'OutputDirectory must be outside the publish directory.'
}

foreach ($required in @('XerahS.exe', 'coreclr.dll', 'xerahs-watchfolder-daemon.exe', 'frontend/dist/index.html')) {
    $requiredPath = Join-Path $source $required
    if (!(Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Incomplete publish payload: missing $required."
    }
    if ((Get-Item -LiteralPath $requiredPath).Length -eq 0) {
        throw "Incomplete publish payload: empty $required."
    }
}
$files = @(Get-ChildItem -LiteralPath $source -File -Recurse -Force)
$manifests = @($files | Where-Object { $_.Name -eq 'plugin.json' -and $_.FullName.StartsWith((Join-Path $source 'Plugins') + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) })
if ($manifests.Count -eq 0) {
    throw 'Incomplete publish payload: no bundled plugin manifests.'
}
foreach ($manifest in $manifests) {
    $assemblyName = (Get-Content -LiteralPath $manifest.FullName -Raw | ConvertFrom-Json).assemblyFileName
    if ([string]::IsNullOrWhiteSpace($assemblyName) -or $assemblyName -match '[/\\:]') {
        throw "Invalid plugin assembly filename in $($manifest.FullName)."
    }
    $assemblyPath = Join-Path $manifest.DirectoryName $assemblyName
    if (!(Test-Path -LiteralPath $assemblyPath -PathType Leaf) -or (Get-Item -LiteralPath $assemblyPath).Length -eq 0) {
        throw "Missing or empty plugin assembly: $assemblyPath."
    }
}
if (Test-Path -LiteralPath (Join-Path $source 'portable.txt')) {
    throw 'Publish directory already contains portable.txt; use a clean publish payload for installer isolation.'
}

[System.IO.Directory]::CreateDirectory($destination) | Out-Null
$archivePath = Join-Path $destination "XerahS-$Version-$Runtime-portable.zip"
# Build beside the destination and replace only after success, preserving any prior archive on failure.
$temporaryPath = Join-Path $destination ([System.IO.Path]::GetRandomFileName() + '.zip')
try {
    $archive = [System.IO.Compression.ZipFile]::Open($temporaryPath, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in $files) {
            if ($file.Extension -ieq '.pdb') { continue }
            $relative = $file.FullName.Substring($source.Length + 1).Replace('\', '/')
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $archive, $file.FullName, $relative, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
        if (!($files | Where-Object { $_.FullName -ieq (Join-Path $source 'LICENSE.txt') })) {
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $archive, $licensePath, 'LICENSE.txt', [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
        $archive.CreateEntry('portable.txt') | Out-Null
    }
    finally {
        $archive.Dispose()
    }
    Move-Item -LiteralPath $temporaryPath -Destination $archivePath -Force
}
finally {
    if (Test-Path -LiteralPath $temporaryPath) {
        Remove-Item -LiteralPath $temporaryPath -Force
    }
}
Write-Host "Success: Generated $archivePath"
