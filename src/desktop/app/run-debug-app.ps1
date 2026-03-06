#requires -Version 5.1
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

$ErrorActionPreference = "Stop"

$dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Source)

if (-not $dotnet) {
    $userDotnet = Join-Path $HOME ".dotnet\dotnet.exe"
    if (Test-Path $userDotnet) {
        $dotnet = $userDotnet
    }
}

if (-not $dotnet) {
    Write-Error "dotnet not found in PATH or at $HOME\.dotnet\dotnet.exe"
    exit 1
}

$project = Join-Path $PSScriptRoot "XerahS.App\XerahS.App.csproj"

& $dotnet run --project $project -c Debug @RemainingArgs
exit $LASTEXITCODE
