# Code signing policy

SignPath Foundation onboarding is pending. Unsigned previews are labelled as such and are never submitted to winget. Signed stable releases are blocked until the service is configured and approves the request.

After acceptance, signed releases use free code signing provided by [SignPath.io](https://signpath.io), certificate by [SignPath Foundation](https://signpath.org). The certificate identifies SignPath Foundation; Thoughtfall's application publisher is ratgirl. The pipeline signs the installer, not individual bundled executables or third-party libraries. Signing does not guarantee that Windows SmartScreen will never display a warning.

## Responsibilities

- Author, committer, and reviewer: [rattus-ratgirl](https://github.com/rattus-ratgirl).
- Signing approver: [rattus-ratgirl](https://github.com/rattus-ratgirl).
- Contributors' changes require maintainer review. Maintainers must use multi-factor authentication on GitHub and SignPath.
- Every stable signing request requires manual approval of its source revision, checks, and installer. All preceding build jobs run on GitHub-hosted runners.
- Only the installer produced by the trusted release workflow may be signed. Third-party binaries retain their original signatures and notices.

## Privacy

Thoughtfall stores diary entries locally and exports only to a destination chosen by the user. The application does not transfer diary content to network services. It has no application update service or runtime analytics. Build-time Avalonia telemetry is disabled in CI. GitHub, SignPath, and winget participate in building or distributing the software, not processing diary entries.
