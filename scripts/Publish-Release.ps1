param(
    [switch]$ValidateOnly
)

$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/Release-Version.ps1" -Version $env:VERSION

if ($env:GH_REPO -ine 'rattus-ratgirl/Thoughtfall') {
    throw 'Release publishing is restricted to rattus-ratgirl/Thoughtfall.'
}

$expectedTag = "v$env:VERSION"

if ($env:RELEASE_TAG -cne $expectedTag) {
    throw 'Release tag and version disagree.'
}

$name = "Thoughtfall-$env:VERSION-win-x64-setup.exe"
$installer = Join-Path (Get-Location) "release/$name"
$names = @(
    $name
    "Thoughtfall-$env:VERSION-linux-x64.deb"
    "Thoughtfall-$env:VERSION-linux-arm64.deb"
)

foreach ($asset in $names) {
    $file = Get-Item -LiteralPath (Join-Path (Get-Location) "release/$asset")

    if ($file.PSIsContainer -or $file.Length -eq 0) {
        throw "Missing or empty release asset: $asset"
    }
}

$versionInfo = (Get-Item $installer).VersionInfo

# Inno Setup pads these string resources with spaces.
if ($versionInfo.ProductName.Trim() -cne 'Thoughtfall' -or $versionInfo.ProductVersion.Trim() -cne $env:VERSION) {
    throw 'Installer product metadata does not match this release.'
}

if ($ValidateOnly) {
    Write-Output 'All three release assets are present; Windows product metadata matches. Linux metadata was checked on the native build runners.'
    return
}

$checksums = foreach ($asset in $names) {
    $hash = (Get-FileHash -LiteralPath (Join-Path (Get-Location) "release/$asset") -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $asset"
}

[IO.File]::WriteAllText((Join-Path (Get-Location) 'release/SHA256SUMS.txt'), ($checksums -join "`n") + "`n")

$description = "Windows x64: self-contained, per-user installation.`n`nLinux x64 and ARM64: self-contained Debian packages for Ubuntu 24.04/26.04 and Debian 12/13. Install the matching package with ``sudo apt install ./Thoughtfall-$env:VERSION-linux-x64.deb`` (use ``arm64`` on ARM devices). Requires a graphical desktop with X11 or XWayland; apt installs system-library dependencies. Other Debian-based distributions are best effort.`n`nQuit Thoughtfall before upgrading or uninstalling. Your diary is retained on removal, including Linux purge."
$description = "**Unsigned Windows installer:** This release is not code-signed. Windows may display an unknown-publisher or SmartScreen warning, and some security policies may block installation.`n`n$description"

$changes = Get-Content -LiteralPath (Join-Path (Get-Location) ".changes/$env:VERSION.md") -Raw

if ([string]::IsNullOrWhiteSpace($changes)) {
    throw 'The release changelog is empty.'
}

$description += "`n`n$($changes.Trim())"
$description += "`n`nDownload the installer for your platform below. SHA256SUMS.txt contains the SHA-256 checksums of all three installers.`n`nSource commit: $env:SOURCE_SHA"

[IO.File]::WriteAllText((Join-Path (Get-Location) 'release/notes.md'), $description)

$arguments = @(
    'release'
    'create'
    $env:RELEASE_TAG
    "release/$($names[0])"
    "release/$($names[1])"
    "release/$($names[2])"
    'release/SHA256SUMS.txt'
    '--repo'
    $env:GH_REPO
    '--target'
    $env:SOURCE_SHA
    '--title'
    "Thoughtfall $env:RELEASE_TAG"
    '--notes-file'
    'release/notes.md'
    '--draft'
)

# Creating a new draft fails if this tag already has a release. Never use upload
# --clobber: a retry must not replace files already referenced by winget.
& gh @arguments

if ($LASTEXITCODE -ne 0) {
    throw 'Could not create the release. Inspect existing releases before retrying.'
}

& gh release edit $env:RELEASE_TAG --repo $env:GH_REPO --draft=false

if ($LASTEXITCODE -ne 0) {
    throw 'Publishing failed; the draft is retained for inspection.'
}

"Published https://github.com/$env:GH_REPO/releases/tag/$env:RELEASE_TAG" | Write-Output

if ($env:GITHUB_STEP_SUMMARY) {
    "Published [$env:RELEASE_TAG](https://github.com/$env:GH_REPO/releases/tag/$env:RELEASE_TAG). Winget availability requires a separate accepted submission." >> $env:GITHUB_STEP_SUMMARY
}
