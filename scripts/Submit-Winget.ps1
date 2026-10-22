param([Parameter(Mandatory)][string]$Version)
$ErrorActionPreference = 'Stop'
& "$PSScriptRoot/Release-Version.ps1" -Version $Version
if (!$env:WINGET_CREATE_GITHUB_TOKEN) { throw 'Set the WINGET_CREATE_GITHUB_TOKEN secret before submitting.' }
$repository = 'rattus-ratgirl/Thoughtfall'
$json = & gh release view "v$Version" --repo $repository --json isDraft,isPrerelease,assets
if ($LASTEXITCODE -ne 0) { throw 'Could not read the published release.' }
$release = $json | ConvertFrom-Json
if ($release.isDraft -or $release.isPrerelease) { throw 'Only published stable releases can enter winget.' }
$directory = Join-Path (Split-Path $PSScriptRoot -Parent) "artifacts/winget-download/$Version"
New-Item $directory -ItemType Directory -Force | Out-Null
$name = "Thoughtfall-$Version-win-x64-setup.exe"
& gh release download "v$Version" --repo $repository --dir $directory --pattern $name --pattern SHA256SUMS.txt
if ($LASTEXITCODE -ne 0) { throw 'Release asset download failed. Retry in a fresh workspace.' }
$installer = Join-Path $directory $name
$expected = "$( (Get-FileHash $installer -Algorithm SHA256).Hash.ToLowerInvariant() )  $name"
if ((Get-Content (Join-Path $directory 'SHA256SUMS.txt')) -cnotcontains $expected) { throw 'Release checksum mismatch.' }
$signature = Get-AuthenticodeSignature $installer
if ($signature.Status -ne 'Valid' -or !$signature.TimeStamperCertificate -or $signature.SignerCertificate.Subject -notmatch 'SignPath Foundation') {
    throw 'The installer must have a valid, timestamped SignPath Foundation signature.'
}
$tool = & "$PSScriptRoot/Get-ReleaseTool.ps1" -Tool WingetCreate
$manifest = & "$PSScriptRoot/New-WingetManifest.ps1" -Version $Version -InstallerPath $installer -OutputDirectory (Join-Path $directory 'generated')
& winget validate --manifest $manifest
if ($LASTEXITCODE -ne 0) { throw 'Manifest validation failed.' }
# Initial submissions and updates use the same explicit metadata. The token is
# read by wingetcreate from the environment, never passed on its command line.
& $tool submit --no-open --prtitle "New version: Ratgirl.Thoughtfall version $Version" $manifest
if ($LASTEXITCODE -ne 0) { throw 'Winget submission failed. Check for an existing PR before retrying.' }
