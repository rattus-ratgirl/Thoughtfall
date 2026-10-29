# Publishing Thoughtfall

The public repository is https://github.com/rattus-ratgirl/Thoughtfall. Windows and Linux installers are published as assets on **GitHub Releases**, not GitHub Packages. The Windows installer is unsigned, and each release prominently states this. Windows may display an unknown-publisher or SmartScreen warning, and some security policies may block installation.

Each release contains a Windows x64 installer and Linux x64/ARM64 `.deb` packages. Linux compatibility checks cover Ubuntu 24.04/26.04 and Debian 12/13 on both architectures. Other Debian-based distributions are best effort when dependencies match. Future distro versions require validation before being added to this list. An APT repository, AUR, RPM, AppImage, Flatpak, Snap, and automatic application updates are not included.

## Publish a release

1. Merge changes into the default branch (`master`) and check CI.
2. Open **Actions → Release Thoughtfall → Run workflow** on the default branch. Select a **patch**, **minor**, or **major** bump from the latest version in `.changes`; patch is the default. There is no signing setup or channel selection.
3. The workflow calculates and validates one version, then builds and runs unit tests on Windows and native Linux x64/ARM64 runners, all at the same source SHA. It creates the self-contained Inno Setup installer and both Debian packages. Windows installation checks run on a disposable runner; Linux package checks run in clean Ubuntu and Debian containers without .NET preinstalled. All six downloads must pass before publication can proceed.
4. Only after those checks succeed, Changie collects the unreleased fragments into `.changes/X.Y.Z.md`, removes the collected fragments, and merges the version notes into `CHANGELOG.md`. The workflow commits and pushes those files to the default branch. If that branch advanced during testing, the workflow stops rather than releasing untested changes.
5. Linux runners verify package identity, version, architecture, ownership, permissions, contents, and native dependencies. Before changing the changelog, the publisher requires all six nonempty assets and matching Windows product/version metadata. It computes one checksum file, uploads all assets to a draft, and publishes a regular release tagged `vX.Y.Z` at the new changelog commit. Installers were built from its tested parent; the release commit changes only the changelog and fragments.
6. Download `Thoughtfall-X.Y.Z-win-x64-setup.exe`, `Thoughtfall-X.Y.Z-linux-x64.deb`, or `Thoughtfall-X.Y.Z-linux-arm64.deb` for your platform. `SHA256SUMS.txt` lists all six downloads. Release notes include installation guidance, the exact collected version notes, the unsigned Windows notice, and the release commit.

The workflow's `GITHUB_TOKEN` needs permission to push the changelog commit to the default branch. Repository branch rules must allow this; a rejected push stops publication. The workflow never force-pushes or bypasses branch rules. No changelog commit is made when a build or test fails, and a release with no unreleased fragments is rejected.

Versions must be three integers without leading zeros or a `v` prefix, each at most 65534. Stable versions must increase. Existing tags and releases, including drafts, are never overwritten. Historical previews remain available; preview users should explicitly install the regular installer when both have the same numeric version.

CI runs for pull requests and pushes to `main` or `master`. Release workflows only run from the repository's default branch. Build/test success alone does not publish a release.

## Submit to winget

Winget receives a manifest pointing at the exact public installer URL and SHA-256. Code signing is not required by this pipeline. Microsoft validates and reviews the pull request before `winget install --exact --id Ratgirl.Thoughtfall` becomes available.

For Actions automation, configure a dedicated `WINGET_CREATE_GITHUB_TOKEN` repository secret using a credential supported by [WingetCreate](https://github.com/microsoft/winget-create#github-personal-access-token-classic-permissions). It must be able to fork/push the community manifest repository and open pull requests; the automatic repository `GITHUB_TOKEN` is insufficient. Never put credentials in source files, workflow inputs, or logs.

Run **Submit winget release** for an existing regular release. It downloads the published installer and checksum, checks their integrity and product/version metadata, generates the `Ratgirl.Thoughtfall` manifests, validates them, and opens a PR. Signing checks are not performed. The generated manifests are retained as a workflow artifact.

Alternatively, run `./scripts/Submit-Winget.ps1 -Version 0.1.0` locally with `WINGET_CREATE_GITHUB_TOKEN` supplied through the process environment and an authenticated `gh` session. This does not upload the credential to GitHub Actions.

After the first package is accepted and the dedicated secret is configured, set repository variable `WINGET_ENABLED` to `true` to submit future releases automatically. Without that setting, the release workflow publishes the installers and checksum file but skips winget submission. Submission is separately retryable and never rebuilds the installer. It downloads only the Windows installer and checks its matching line in the shared checksum file.

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

## Install on Linux

Select x64 for `dpkg --print-architecture` output `amd64`, or ARM64 for `arm64`. Replace `X.Y.Z` with the downloaded version and use the matching filename:

```sh
sudo apt install ./Thoughtfall-X.Y.Z-linux-x64.deb
```

Installation requires administrator permission and is system-wide. The package bundles .NET and declares its system-library dependencies, including the ICU and OpenSSL alternatives used by the supported distributions. No Microsoft repository is needed. It installs under `/usr/lib/thoughtfall`, adds a `thoughtfall` command, and adds a menu entry that runs `thoughtfall --capture`. Both ordinary command-line launch and arguments such as `--capture` retain the app's existing behavior.

The package requires a graphical desktop with X11 or XWayland. Tray support is optional; it does not install an indicator extension or a display server. It does not launch the app, enable autostart, terminate running instances, or change diary data. Quit Thoughtfall before an upgrade or uninstall. Install a downloaded newer package with the same `apt install ./...` command to upgrade.

Use `sudo apt remove thoughtfall` to uninstall. `sudo apt purge thoughtfall` also leaves every user's `~/.thoughtfall` intact. Keep that directory when backing up your diary. Later versions must be downloaded from GitHub Releases; installing the package does not configure an APT repository.

To check just the downloaded Linux package against the shared checksums:

```sh
grep '  Thoughtfall-X.Y.Z-linux-x64.deb$' SHA256SUMS.txt | sha256sum --check --strict
```

## Failure and retry behaviour

- Failed build, test, or packaging: fix the cause and rerun before publication.
- Failed publication may leave a draft. Inspect its exact source and assets before completing it; the release workflow intentionally refuses to reuse an existing tag or draft.
- A failure after the changelog push leaves that commit in the default branch. Inspect the generated version notes and any draft before retrying publication; starting a new workflow selects the next version and requires new fragments.
- Published installers are immutable by policy: no `--clobber`, deleting/recreating releases, or rebuilding the same stable version. Release a higher version for corrections.
- Winget failure does not undo a GitHub release. Check for an open or merged PR for that version before rerunning **Submit winget release**. Retry only submission; it downloads the same published bytes. Update an existing PR when appropriate rather than creating duplicates.
- GitHub release publication and winget availability are separate milestones. A green release job does not establish Microsoft acceptance.

## Validation checklist

Automated CI builds/tests the solution on Windows and both native Linux architectures and checks release-version validation. Windows checks cover silent install, reinstall, uninstall, registry metadata, shortcut creation, native dependencies, running-app mutex refusal, and retention of a sentinel file outside the installation directory.

`Test-LinuxPackages.sh` checks both Ubuntu LTS versions and both Debian versions in fresh containers on the target CPU, without emulation. The package checks cover dependency resolution, identity/version/architecture, root ownership, file permissions, required runtime/SQLite/rendering files, ELF architecture and library resolution, desktop entry/icon, install, reinstall, removal, purge, and sentinel retention. The only tolerated unresolved library is the obsolete LTTng ABI used by .NET's optional tracing provider; application dependencies must all resolve. They reject packages with maintainer scripts and verify installation does not start Thoughtfall or create diary data. The source mount is read-only, and the installation script refuses to operate outside a disposable container explicitly marked for this test.

Packaging checks are separate shell scripts; application unit tests still use only public members and in-memory inputs. No automated installer check launches the GUI or opens/mocks a diary database. Reinstall is not a cross-version upgrade test. Native desktop behavior still requires manual validation.

Before the first public stable release and after installer changes, use a disposable Windows desktop:

- Install without .NET preinstalled; inspect installer appearance, shortcut, and Apps & Features registration.
- Launch, save a test diary entry, reopen it through export, and check tray and second-launch activation.
- Leave an unsaved draft open; verify install and uninstall refuse to proceed until you quit through the app, with Save/Discard/Cancel working normally.
- Upgrade from a previous version and verify one installation entry remains, the new version runs, and diary/export contents are unchanged.
- Uninstall; verify the app and shortcut disappear while `~/.thoughtfall/data.db3` remains intact.
- Verify the unsigned notice in the release notes, verify `SHA256SUMS.txt`, and validate/install the winget manifest.

Before the first Linux release and after packaging changes, use disposable x64 and ARM64 graphical desktops:

- Install without .NET preinstalled. Check the menu icon, launch capture from the menu and command line, save/export a test entry, and activate capture through a second launch with `--capture`.
- Check tray and no-tray behavior, appearance and file pickers, and a Wayland session through XWayland.
- Once a previous Linux package exists, quit the app and upgrade across versions. Verify package version, diary contents, settings, and export remain correct.
- Remove and purge the package. Verify application/menu files disappear and `~/.thoughtfall` remains intact.

## Local builds

For Windows, install Python 3 and Inno Setup 7.1.0, then run:

```powershell
./scripts/Build-Windows.ps1 -Version 0.1.0 -IsccPath 'C:/Program Files/Inno Setup 7/ISCC.exe'
```

For Linux, use a matching native Ubuntu/Debian host with the .NET 10 SDK, Python 3, and `dpkg-deb` installed. Choose `x64` or `arm64`:

```sh
bash scripts/Build-Linux.sh 0.1.0 x64
# Optional package checks: requires Docker with access to the four distro images.
bash scripts/Test-LinuxPackages.sh 0.1.0 x64
```

Linux output is `artifacts/installers/X.Y.Z/Thoughtfall-X.Y.Z-linux-ARCH.deb`; its package architecture is `amd64` for x64 or `arm64` for ARM64. Build scripts refuse to reuse their own publish/staging/package output. Linux uses `dpkg-deb --root-owner-group` and needs no packaging service or signing secret. The PNG menu icon is the existing 256-pixel PNG extracted verbatim from the application's ICO.

`Get-ReleaseTool.ps1 -Tool InnoSetup` downloads a pinned, checksum-verified compiler and extracts it in portable mode under `artifacts/tools`, without registering an installed application. The same script supplies pinned, checksum-verified Changie 1.26.0 for version calculation and changelog collection. External Actions are pinned to commit SHAs. Tool versions and hashes live in that script; `Install-WingetForCI.ps1` pins the official winget bootstrap module and client used on disposable runners.

The installer includes NuGet licence metadata, full licence texts/notices, the runtime pack's notices, and Inter's OFL. The vendored Apache text comes from SPDX license-list-data v3.27.0; Inter's OFL comes from rsms/inter v4.1. The collector fails on new unhandled licence expressions so dependency changes receive review.

CLI archives are built alongside installers for Windows x64 and Linux x64/ARM64 and included in release validation and SHA256SUMS.txt. The desktop packages contain the CLI in a separate runtime directory. Linux launchers are thoughtfall (CLI) and thoughtfall-gui (desktop). The Windows installer offers user PATH integration.
