param(
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$InstallerPath,
    [Parameter(Mandatory)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
& "$PSScriptRoot/Release-Version.ps1" -Version $Version
$hash = (Get-FileHash $InstallerPath -Algorithm SHA256).Hash
$repository = 'https://github.com/rattus-ratgirl/Thoughtfall'
$directory = Join-Path $OutputDirectory "manifests/r/Ratgirl/Thoughtfall/$Version"
New-Item $directory -ItemType Directory -Force | Out-Null
$encoding = New-Object System.Text.UTF8Encoding $false
$files = @{
    'Ratgirl.Thoughtfall.yaml' = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.version.1.6.0.schema.json
PackageIdentifier: Ratgirl.Thoughtfall
PackageVersion: $Version
DefaultLocale: en-US
ManifestType: version
ManifestVersion: 1.6.0
"@
    'Ratgirl.Thoughtfall.locale.en-US.yaml' = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.defaultLocale.1.6.0.schema.json
PackageIdentifier: Ratgirl.Thoughtfall
PackageVersion: $Version
PackageLocale: en-US
Publisher: ratgirl
PublisherUrl: $repository
PublisherSupportUrl: $repository/issues
PackageName: Thoughtfall
PackageUrl: $repository
License: MIT
LicenseUrl: $repository/blob/v$Version/LICENSE
Copyright: Copyright (c) 2026 ratgirl
ShortDescription: A quiet local diary for thoughts as they happen.
Moniker: thoughtfall
Tags:
- diary
- journal
ReleaseNotesUrl: $repository/releases/tag/v$Version
ManifestType: defaultLocale
ManifestVersion: 1.6.0
"@
    'Ratgirl.Thoughtfall.installer.yaml' = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.installer.1.6.0.schema.json
PackageIdentifier: Ratgirl.Thoughtfall
PackageVersion: $Version
InstallerType: inno
Scope: user
UpgradeBehavior: install
AppsAndFeaturesEntries:
- DisplayName: Thoughtfall
  Publisher: ratgirl
  DisplayVersion: $Version
  ProductCode: '{D43775B7-822E-4C71-8A54-041708E0EE2D}_is1'
Installers:
- Architecture: x64
  InstallerUrl: $repository/releases/download/v$Version/Thoughtfall-$Version-win-x64-setup.exe
  InstallerSha256: $hash
ManifestType: installer
ManifestVersion: 1.6.0
"@
}
foreach ($name in $files.Keys) { [IO.File]::WriteAllText((Join-Path $directory $name), $files[$name] + "`n", $encoding) }
return $directory
