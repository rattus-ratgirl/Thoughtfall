# Code signing policy

Thoughtfall's Windows installer is **unsigned**. Release notes disclose this for every release. Windows may display an unknown-publisher or SmartScreen warning, and some security policies may block installation.

Releases are built and tested on GitHub-hosted runners. Each release includes a SHA-256 checksum and the source commit used to build it. Checksums detect changed bytes; they do not establish a code-signing identity. Third-party libraries retain their original signatures and licence notices.

Winget submissions reference the published installer and checksum. Acceptance into the winget catalogue does not add a signature to the installer.

## Privacy

Thoughtfall stores diary entries locally and exports only to a destination chosen by the user. The application does not transfer diary content to network services. It has no application update service or runtime analytics. Build-time Avalonia telemetry is disabled in CI. GitHub and winget participate in building or distributing the software, not processing diary entries.
