$ErrorActionPreference = 'Stop'

$packageName = 'xerahs'
$appId = '7B28B84B-3D6B-4198-8424-95C4F6298517'
$silentArgs = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART'
$validExitCodes = @(0, 1605, 1614, 1641, 3010)

function Get-ExecutableAndArguments {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CommandLine
    )

    if ($CommandLine -match '^\s*"(?<exe>[^"]+)"\s*(?<args>.*)$') {
        return @{
            Executable = $matches.exe
            Arguments = $matches.args
        }
    }

    if ($CommandLine -match '^\s*(?<exe>.+?\.exe)\s*(?<args>.*)$') {
        return @{
            Executable = $matches.exe
            Arguments = $matches.args
        }
    }

    throw "Unable to parse uninstall command line: $CommandLine"
}

$registryPaths = @(
    'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*',
    'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*',
    'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*'
)

$uninstallEntries = foreach ($path in $registryPaths) {
    Get-ItemProperty -Path $path -ErrorAction SilentlyContinue |
        Where-Object {
            $_.PSChildName -like "*$appId*" -or $_.DisplayName -eq 'XerahS'
        }
}

$uninstallEntry = $uninstallEntries | Select-Object -First 1

if ($null -eq $uninstallEntry) {
    Write-Warning "XerahS uninstall information was not found. The package may already be removed."
    return
}

$commandLine = if (-not [string]::IsNullOrWhiteSpace($uninstallEntry.QuietUninstallString)) {
    $uninstallEntry.QuietUninstallString
} else {
    "$($uninstallEntry.UninstallString) $silentArgs"
}

if ([string]::IsNullOrWhiteSpace($commandLine)) {
    throw "Unable to locate an uninstall command for $packageName."
}

$command = Get-ExecutableAndArguments -CommandLine $commandLine

Start-ChocolateyProcessAsAdmin -ExeToRun $command.Executable `
                               -Statements $command.Arguments `
                               -ValidExitCodes $validExitCodes
