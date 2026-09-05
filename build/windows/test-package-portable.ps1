$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$fixtureRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('xerahs-portable-test-' + [Guid]::NewGuid().ToString('N'))
$source = Join-Path $fixtureRoot 'publish'
$output = Join-Path $fixtureRoot 'dist'
$packager = Join-Path $PSScriptRoot 'package-portable.ps1'

function Assert-True($Condition, [string]$Message) {
    if (!$Condition) { throw $Message }
}

function Assert-Rejected([scriptblock]$Action, [string]$ExpectedMessage) {
    try { & $Action }
    catch {
        Assert-True ($_.Exception.Message -like "*$ExpectedMessage*") "Unexpected failure: $_"
        return
    }
    throw "Expected rejection: $ExpectedMessage"
}

try {
    foreach ($relative in @('XerahS.exe', 'coreclr.dll', 'xerahs-watchfolder-daemon.exe', 'frontend/dist/index.html',
            'Plugins/example/plugin.json', 'Plugins/example/example.dll', 'XerahS.pdb', 'Plugins/example/example.pdb', '.hidden')) {
        $file = Join-Path $source $relative
        [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($file)) | Out-Null
        [System.IO.File]::WriteAllText($file, "fixture: $relative")
    }
    [System.IO.File]::WriteAllText((Join-Path $source 'Plugins/example/plugin.json'), '{"assemblyFileName":"example.dll"}')

    foreach ($runtime in @('win-x64', 'win-arm64')) {
        & $packager -PublishDirectory $source -Version '1.2.3' -Runtime $runtime -OutputDirectory $output
        $zipPath = Join-Path $output "XerahS-1.2.3-$runtime-portable.zip"
        $zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
        try {
            $names = @($zip.Entries | ForEach-Object { $_.FullName })
            foreach ($required in @('XerahS.exe', 'xerahs-watchfolder-daemon.exe', 'portable.txt',
                    'frontend/dist/index.html', 'Plugins/example/plugin.json', 'Plugins/example/example.dll', '.hidden', 'LICENSE.txt')) {
                Assert-True ($names -contains $required) "Archive missing root-relative entry $required"
            }
            Assert-True (@($names | Where-Object { $_ -like '*.pdb' }).Count -eq 0) 'Archive contains debug symbols.'
            $reader = [System.IO.StreamReader]::new($zip.GetEntry('Plugins/example/example.dll').Open())
            try { Assert-True ($reader.ReadToEnd() -eq 'fixture: Plugins/example/example.dll') 'Nested payload changed.' }
            finally { $reader.Dispose() }
        }
        finally { $zip.Dispose() }
    }
    Assert-True (!(Test-Path -LiteralPath (Join-Path $source 'portable.txt'))) 'Installer payload was contaminated.'
    Assert-True (Test-Path -LiteralPath (Join-Path $source 'XerahS.pdb')) 'Source payload was modified.'
    Assert-Rejected { & $packager -PublishDirectory $source -Version '1.2.3' -Runtime win-x64 -OutputDirectory (Join-Path $source 'dist') } 'outside the publish directory'

    $oldHash = (Get-FileHash -LiteralPath (Join-Path $output 'XerahS-1.2.3-win-x64-portable.zip')).Hash
    Remove-Item -LiteralPath (Join-Path $source 'XerahS.exe')
    Assert-Rejected { & $packager -PublishDirectory $source -Version '1.2.3' -Runtime win-x64 -OutputDirectory $output } 'missing XerahS.exe'
    Assert-True ((Get-FileHash -LiteralPath (Join-Path $output 'XerahS-1.2.3-win-x64-portable.zip')).Hash -eq $oldHash) 'Failure replaced prior artifact.'
    [System.IO.File]::WriteAllText((Join-Path $source 'XerahS.exe'), 'app')
    [System.IO.File]::WriteAllText((Join-Path $source 'portable.txt'), '')
    Assert-Rejected { & $packager -PublishDirectory $source -Version '1.2.3' -Runtime win-x64 -OutputDirectory $output } 'already contains portable.txt'
    Write-Host 'Portable packaging fixture tests passed (both architectures, layout, payload isolation, rejection and prior artifact preservation).'
}
finally {
    $resolvedFixture = [System.IO.Path]::GetFullPath($fixtureRoot)
    $tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    if (!$resolvedFixture.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -or
        [System.IO.Path]::GetFileName($resolvedFixture) -notlike 'xerahs-portable-test-*') {
        throw "Refusing cleanup outside test temporary directory: $resolvedFixture"
    }
    if (Test-Path -LiteralPath $resolvedFixture) {
        Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
    }
}
