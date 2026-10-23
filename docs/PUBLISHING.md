# Publishing Thoughtfall

The public repository is https://github.com/rattus-ratgirl/Thoughtfall. Windows installers are published as assets on **GitHub Releases**, not GitHub Packages. The installer is unsigned, and each release prominently states this. Windows may display an unknown-publisher or SmartScreen warning, and some security policies may block installation.

This pipeline targets Windows x64. apt, AUR, ARM64, Linux packages, and automatic application updates are not included.

## Publish a release

1. Merge changes into the default branch (`master`) and check CI.
2. Open **Actions ? Release Windows ? Run workflow** on the default branch. Enter a version such as `0.1.0`; there is no signing setup or channel selection.
3. The workflow builds, runs unit tests, creates the self-contained Inno Setup installer, and tests silent installation/uninstall on a disposable runner.
4. It verifies installer product/version metadata, computes the checksum, uploads both assets to a draft, and publishes a regular release tagged `vX.Y.Z` at the exact source commit.
5. Download `Thoughtfall-X.Y.Z-win-x64-setup.exe` and `SHA256SUMS.txt` from the release. The release notes explain that the installer is unsigned and identify the source commit.

Versions must be three integers without leading zeros or a `v` prefix, each at most 65534. Stable versions must increase. Existing tags and releases, including drafts, are never overwritten. Historical previews remain available; preview users should explicitly install the regular installer when both have the same numeric version.

CI runs for pull requests and pushes to `main` or `master`. Release workflows only run from the repository's default branch. Build/test success alone does not publish a release.

## Submit to winget

Winget receives a manifest pointing at the exact public installer URL and SHA-256. Code signing is not required by this pipeline. Microsoft validates and reviews the pull request before `winget install --exact --id Ratgirl.Thoughtfall` becomes available.

For Actions automation, configure a dedicated `WINGET_CREATE_GITHUB_TOKEN` repository secret using a credential supported by [WingetCreate](https://github.com/microsoft/winget-create#github-personal-access-token-classic-permissions). It must be able to fork/push the community manifest repository and open pull requests; the automatic repository `GITHUB_TOKEN` is insufficient. Never put credentials in source files, workflow inputs, or logs.

Run **Submit winget release** for an existing regular release. It downloads the published installer and checksum, checks their integrity and product/version metadata, generates the `Ratgirl.Thoughtfall` manifests, validates them, and opens a PR. Signing checks are not performed. The generated manifests are retained as a workflow artifact.

Alternatively, run `./scripts/Submit-Winget.ps1 -Version 0.1.0` locally with `WINGET_CREATE_GITHUB_TOKEN` supplied through the process environment and an authenticated `gh` session. This does not upload the credential to GitHub Actions.

After the first package is accepted and the dedicated secret is configured, set repository variable `WINGET_ENABLED` to `true` to submit future releases automatically. Without that setting, the release workflow publishes the installer and checksum but skips winget submission. Submission is separately retryable and never rebuilds the installer.

Follow the PR's checks and review, including any contributor agreement required by Microsoft. After acceptance, run `winget source update`, then `winget show --exact --id Ratgirl.Thoughtfall` and install in a clean environment.

To inspect a manifest before submission:

```powershell
$manifest = ./scripts/New-WingetManifest.ps1 -Version 0.1.0 -InstallerPath ./Thoughtfall-0.1.0-win-x64-setup.exe -OutputDirectory ./artifacts/manifest-review
winget validate --manifest $manifest
# In a disposable Windows desktop, enable local manifests if necessary:
winget settings --enable LocalManifestFiles
winget install --manifest $manifest
```

Local-manifest configuration may require elevation. The installer itself is per-user and does not require administrator privileges.

## Failure and retry behaviour

- Failed build, test, or packaging: fix the cause and rerun before publication.
- Failed publication may leave a draft. Inspect its exact source and assets before completing it; the release workflow intentionally refuses to reuse an existing tag or draft.
- A published installer is immutable by policy: no `--clobber`, deleting/recreating releases, or rebuilding the same stable version. Release a higher version for corrections.
- Winget failure does not undo a GitHub release. Check for an open or merged PR for that version before rerunning **Submit winget release**. Retry only submission; it downloads the same published bytes. Update an existing PR when appropriate rather than creating duplicates.
- GitHub release publication and winget availability are separate milestones. A green release job does not establish Microsoft acceptance.

## Validation checklist

Automated CI builds/tests the solution, checks release-version validation, compiles the installer, and exercises silent install, reinstall, uninstall, registry metadata, shortcut creation, native dependencies, running-app mutex refusal, and retention of a sentinel file outside the installation directory. It never opens or mocks a diary database. Reinstall is not a cross-version upgrade test.

Before the first public stable release and after installer changes, use a disposable Windows desktop:

- Install without .NET preinstalled; inspect installer appearance, shortcut, and Apps & Features registration.
- Launch, save a test diary entry, reopen it through export, and check tray and second-launch activation.
- Leave an unsaved draft open; verify install and uninstall refuse to proceed until you quit through the app, with Save/Discard/Cancel working normally.
- Upgrade from a previous version and verify one installation entry remains, the new version runs, and diary/export contents are unchanged.
- Uninstall; verify the app and shortcut disappear while `~/.thoughtfall/data.db3` remains intact.
- Verify the unsigned notice in the release notes, verify `SHA256SUMS.txt`, and validate/install the winget manifest.

For a local package build, install Python 3 and Inno Setup 7.1.0, then run:

```powershell
./scripts/Build-Windows.ps1 -Version 0.1.0 -IsccPath 'C:/Program Files/Inno Setup 7/ISCC.exe'
```

The build refuses to reuse its output directories. `Get-ReleaseTool.ps1 -Tool InnoSetup` downloads a pinned, checksum-verified compiler and extracts it in portable mode under `artifacts/tools`, without registering an installed application. External Actions are pinned to commit SHAs. Tool versions and hashes live in that script; `Install-WingetForCI.ps1` pins the official winget bootstrap module and client used on disposable runners.

The installer includes NuGet licence metadata, full licence texts/notices, the runtime pack's notices, and Inter's OFL. The vendored Apache text comes from SPDX license-list-data v3.27.0; Inter's OFL comes from rsms/inter v4.1. The collector fails on new unhandled licence expressions so dependency changes receive review.
