$ErrorActionPreference = 'Stop'

$packageName  = 'xerahs'
$packageVersion = $env:ChocolateyPackageVersion
$fileType     = 'exe'
$silentArgs   = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /NORUN'
$checksumType = 'sha256'
$x64Checksum  = '684b4f812b9198bbd79af66a5ccccfb6e5e553d62fdf6a0e2a76e04c08ef33b1'
$arm64Checksum = 'fdd6c1f7f857776cbc22bc21826b85a6181cd372e4f3965d2cd5bf26c8b5bf75'

if ([string]::IsNullOrWhiteSpace($packageVersion)) {
    throw "Chocolatey package version is not available."
}

$isArm64 = $env:PROCESSOR_ARCHITECTURE -eq 'ARM64' -or $env:PROCESSOR_ARCHITEW6432 -eq 'ARM64'
$assetSuffix = if ($isArm64) { 'win-arm64' } else { 'win-x64' }
$checksum = if ($isArm64) { $arm64Checksum } else { $x64Checksum }
$url = "https://github.com/ShareX/XerahS/releases/download/v$packageVersion/XerahS-$packageVersion-$assetSuffix.exe"

Install-ChocolateyPackage -PackageName $packageName `
                          -FileType $fileType `
                          -Url $url `
                          -Checksum $checksum `
                          -ChecksumType $checksumType `
                          -SilentArgs $silentArgs
