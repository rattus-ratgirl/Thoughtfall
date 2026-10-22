$ErrorActionPreference = 'Stop'
& "$PSScriptRoot/Release-Version.ps1" -Version $env:VERSION
if ($env:CHANNEL -notin @('unsigned-preview', 'signed-stable')) { throw 'Unknown release channel.' }
if ($env:GH_REPO -ine 'rattus-ratgirl/Thoughtfall') { throw 'Release publishing is restricted to rattus-ratgirl/Thoughtfall.' }
$expectedTag = "v$env:VERSION"
if ($env:CHANNEL -eq 'unsigned-preview') { $expectedTag += '-preview' }
if ($env:RELEASE_TAG -cne $expectedTag) { throw 'Release tag and version disagree.' }
$name = "Thoughtfall-$env:VERSION-win-x64-setup.exe"
$installer = Join-Path (Get-Location) "release/$name"
$versionInfo = (Get-Item $installer).VersionInfo
# Inno Setup pads these string resources with spaces.
if ($versionInfo.ProductName.Trim() -cne 'Thoughtfall' -or $versionInfo.ProductVersion.Trim() -cne $env:VERSION) {
    throw 'Installer product metadata does not match this release.'
}
if ($env:CHANNEL -eq 'signed-stable') {
    $signature = Get-AuthenticodeSignature $installer
    if ($signature.Status -ne 'Valid' -or !$signature.TimeStamperCertificate -or $signature.SignerCertificate.Subject -notmatch 'SignPath Foundation') {
        throw 'Stable releases require a valid, timestamped SignPath Foundation signature.'
    }
}
$hash = (Get-FileHash $installer -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path (Get-Location) 'release/SHA256SUMS.txt'), "$hash  $name`n")
$description = "Windows x64, self-contained, per-user installation. Quit Thoughtfall before upgrading. Your diary is retained on uninstall."
if ($env:CHANNEL -eq 'unsigned-preview') {
    $description = "UNSIGNED PREVIEW: Windows may warn about an unknown publisher. This preview is not submitted to winget. SignPath acceptance is pending.`n`n$description"
} else {
    $description += "`n`nFree code signing provided by SignPath.io, certificate by SignPath Foundation."
}
$description += "`n`nSource commit: $env:SOURCE_SHA`n`n[Code signing policy](https://github.com/rattus-ratgirl/Thoughtfall/blob/$env:RELEASE_TAG/docs/CODE-SIGNING.md)"
[IO.File]::WriteAllText((Join-Path (Get-Location) 'release/notes.md'), $description)
$arguments = @('release', 'create', $env:RELEASE_TAG, $installer, 'release/SHA256SUMS.txt', '--repo', $env:GH_REPO,
    '--target', $env:SOURCE_SHA, '--title', "Thoughtfall $env:RELEASE_TAG", '--notes-file', 'release/notes.md', '--draft')
if ($env:CHANNEL -eq 'unsigned-preview') { $arguments += '--prerelease' }
# Creating a new draft fails if this tag already has a release. Never use upload
# --clobber: a retry must not replace files already referenced by winget.
& gh @arguments
if ($LASTEXITCODE -ne 0) { throw 'Could not create the release. Inspect existing releases before retrying.' }
& gh release edit $env:RELEASE_TAG --repo $env:GH_REPO --draft=false
if ($LASTEXITCODE -ne 0) { throw 'Publishing failed; the draft is retained for inspection.' }
"Published https://github.com/$env:GH_REPO/releases/tag/$env:RELEASE_TAG" | Write-Output
if ($env:GITHUB_STEP_SUMMARY) {
    "Published [$env:RELEASE_TAG](https://github.com/$env:GH_REPO/releases/tag/$env:RELEASE_TAG). Winget availability requires a separate accepted submission." >> $env:GITHUB_STEP_SUMMARY
}
