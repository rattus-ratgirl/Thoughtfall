# Publishing Thoughtfall

The public repository is https://github.com/rattus-ratgirl/Thoughtfall. Windows installers are hosted in GitHub Releases. Winget receives a manifest pointing at a signed installer; Microsoft reviews its pull request before `winget install --exact --id Ratgirl.Thoughtfall` becomes available.

This pipeline targets Windows x64. apt, AUR, ARM64, Linux packages, and automatic application updates are not included.

## Initial repository setup

The repository currently has starter files on `main`, while this local checkout has separate history on `master` and no remote. Reconcile those histories before pushing: preserve both histories with a normal merge, resolve the starter README/licence in favour of the reviewed implementation, and use a pull request into `main`. Do not force-push over the remote. This implementation does not commit or push your working tree.

CI runs for pull requests and pushes to `main` or `master`. Release workflows only run from the repository's default branch (currently `main`). After merging, use **Actions → Release Windows → Run workflow** on that branch.

## First unsigned preview

1. Run **Release Windows** with version `0.1.0` and channel `unsigned-preview`.
2. The workflow builds, runs unit tests, creates the self-contained Inno Setup installer, and tests silent installation/uninstall on a disposable runner.
3. It publishes `v0.1.0-preview` as a prerelease with an unsigned warning, installer, and `SHA256SUMS.txt`. It does not submit anything to winget.
4. Test the installer on a disposable Windows desktop using the checklist below, then use the public source and release links in your [SignPath application](https://signpath.org/apply). Acceptance is external and is not guaranteed.

Versions must be three integers without leading zeros or a `v` prefix, each at most 65534. Stable versions must increase. The preview suffix belongs to the Git tag and release title; Windows installer metadata uses the numeric version. A preview and stable release may share `0.1.0`, but preview users should explicitly install the stable installer; do not rely on winget discovering a same-version upgrade.

## SignPath onboarding

Follow the [Foundation conditions](https://signpath.org/terms) and [GitHub integration documentation](https://docs.signpath.io/trusted-build-systems/github). Install the SignPath GitHub App for this repository, connect its trusted GitHub build source, and assign rattus-ratgirl as signing approver. Require manual approval; do not enable automatic approval.

Configure the artifact as the ZIP archive produced by `actions/upload-artifact`, containing exactly `Thoughtfall-${version}-win-x64-setup.exe`. Sign that installer with Authenticode and timestamping. Define a `version` parameter; require the installer product name to be `Thoughtfall`, product version to equal the supplied version, and file version to equal that version plus `.0`. Inno Setup pads string metadata with trailing spaces; account for that in restrictions without allowing other product names or versions. Restrict signing to this repository's default-branch release workflow on GitHub-hosted runners. Have SignPath review the artifact configuration during onboarding.

Add these repository Actions settings; never put credentials in source files:

| Kind | Name | Value |
| --- | --- | --- |
| Secret | `SIGNPATH_API_TOKEN` | Token allowed to submit to the configured signing policy |
| Variable | `SIGNPATH_ORGANIZATION_ID` | Assigned organisation UUID |
| Variable | `SIGNPATH_PROJECT_SLUG` | Thoughtfall project slug |
| Variable | `SIGNPATH_SIGNING_POLICY_SLUG` | Production policy with manual approval |
| Variable | `SIGNPATH_ARTIFACT_CONFIGURATION_SLUG` | Reviewed installer ZIP configuration |

Once accepted, update the pending status in [Code signing policy](CODE-SIGNING.md). Signed release notes include SignPath attribution. This pipeline signs the outer installer; the installed application and uninstaller are not separately signed.

## Signed release and winget bootstrap

1. Run **Release Windows** with a version and `signed-stable`.
2. Inspect the build results and approve the SignPath request. The workflow waits up to one hour. Missing credentials, rejection, timeout, or an invalid/unexpected signature stop publication.
3. The pipeline validates the signed installer's product/version and timestamped signature, computes its SHA-256, then publishes `vX.Y.Z`. Review the installer using the checklist below before the first winget submission.
4. Configure `WINGET_CREATE_GITHUB_TOKEN` as a repository secret using a dedicated credential supported by [WingetCreate](https://github.com/microsoft/winget-create#github-personal-access-token-classic-permissions). It must be able to fork/push the community manifest repository and open pull requests; the repository's automatic `GITHUB_TOKEN` is not sufficient. Do not paste credentials into workflow inputs or logs.
5. Run **Submit winget release** manually for that version. It downloads the existing release, verifies its checksum and signature, generates `Ratgirl.Thoughtfall` manifests, runs `winget validate`, and submits a PR through WingetCreate. Download the manifest workflow artifact for inspection. Only submit after a real desktop/local-manifest installation check.
6. Follow the PR's validation and review, responding to requested corrections. After acceptance, check `winget source update`, then `winget show --exact --id Ratgirl.Thoughtfall` and install in a clean environment.
7. Set repository variable `WINGET_ENABLED` to `true`. Future signed releases automatically invoke the same submission workflow. Preview releases never do.

To prepare the first manifest locally before submission:

```powershell
$manifest = ./scripts/New-WingetManifest.ps1 -Version 0.1.0 -InstallerPath ./Thoughtfall-0.1.0-win-x64-setup.exe -OutputDirectory ./artifacts/manifest-review
winget validate --manifest $manifest
# In a disposable Windows desktop, enable local manifests if necessary:
winget settings --enable LocalManifestFiles
winget install --manifest $manifest
```

Local-manifest configuration may require elevation. The installer itself is per-user and does not require administrator privileges.

## Failure and retry behaviour

- Failed build, test, packaging, or signing: fix the cause and rerun before publication. Never publish an unsigned installer as stable.
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
- Check the final signed installer using `Get-AuthenticodeSignature`, verify `SHA256SUMS.txt`, and validate/install its winget manifest.

For a local package build, install Python 3 and Inno Setup 7.1.0, then run:

```powershell
./scripts/Build-Windows.ps1 -Version 0.1.0 -IsccPath 'C:/Program Files/Inno Setup 7/ISCC.exe'
```

The build refuses to reuse its output directories. `Get-ReleaseTool.ps1 -Tool InnoSetup` downloads a pinned, checksum-verified compiler and extracts it in portable mode under `artifacts/tools`, without registering an installed application. External Actions are pinned to commit SHAs. Tool versions and hashes live in that script; `Install-WingetForCI.ps1` pins the official winget bootstrap module and client used on disposable runners.

The installer includes NuGet licence metadata, full licence texts/notices, the runtime pack's notices, and Inter's OFL. The vendored Apache text comes from SPDX license-list-data v3.27.0; Inter's OFL comes from rsms/inter v4.1. The collector fails on new unhandled licence expressions so dependency changes receive review.
