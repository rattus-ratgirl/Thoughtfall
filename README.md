# Thoughtfall

A small local diary for Windows and Linux, built with Avalonia on .NET 10.

## Windows releases

Windows installers are published through GitHub Actions to [GitHub Releases](https://github.com/rattus-ratgirl/Thoughtfall/releases).
The installer bundles .NET, installs for the current user, and adds a Start menu shortcut.
Quit Thoughtfall before upgrading or uninstalling; your diary is retained.

The Windows installer is unsigned; Windows may warn about an unknown publisher or block installation. The winget package `Ratgirl.Thoughtfall` becomes available after community repository acceptance.
See [Publishing](docs/PUBLISHING.md) for setup and release instructions, and [Code signing policy](docs/CODE-SIGNING.md) for unsigned distribution details.

## Run

Install the .NET 10 SDK, then run:

```sh
dotnet run --project Thoughtfall.Desktop
```

Open `Thoughtfall.slnx` in your IDE and select `Thoughtfall.Desktop` to debug. Linux needs a graphical desktop with Avalonia's X11/font dependencies. A tray indicator is optional.

- Thoughtfall starts quietly in the tray when supported. Choose **Capture a thought** to open capture. Desktops without usable tray support show capture on launch. **Ctrl+Enter** or **Save** saves the exact text; **Enter** inserts a newline.
- The editor is disabled while saving. Success clears it and hides or minimizes capture. Failure retains the draft and shows an error.
- **Close**, closing the capture window, or pressing **Escape** retains the draft and hides capture when tray support is available. Linux and desktops without a tray minimize it instead.
- Capture and the tray offer export and the data folder. Tray-menu **Quit** exits Thoughtfall; capture offers **Quit** when usable tray support is unavailable. A second ordinary launch leaves the running application as it is.
- On Windows, **Ctrl+Alt+J** opens capture from any application while Thoughtfall remains running. Choose **Capture shortcut…** in capture or the tray to record a different chord, restore the default, or **Clear** and **Save** to disable it. Global shortcuts and their controls are Windows-only.
- Choose **Appearance…** in capture or the tray on Windows or Linux. **Forest** (green) and **Hearth** (warm brown and peach) are available; scheme changes apply on the next launch, with a pending-restart reminder. **System**, **Light**, or **Dark** applies immediately after **Save** to the currently loaded scheme. System follows desktop appearance changes. **Cancel**, Escape, or closing the dialog discards edits. The default is Forest + System.
- Quit waits for an active save or export, including an open save picker. Complete or cancel the picker to continue. A failed operation keeps the application open. Remaining drafts offer Save, Discard, or Cancel.

Drafts exist only in memory until saved. The app does not start automatically at sign-in. A desktop shortcut may run the executable with `--capture` to open capture directly, including when Thoughtfall is already running. The Start menu shortcut starts Thoughtfall quietly in the tray when supported.

## Storage

Release builds store the SQLite database at **`~/.thoughtfall/data.db3`**, where `~` is the user's home directory (`%USERPROFILE%` on Windows).

Debug builds use the executable directory instead, keeping `data.db3`, `settings.json`, and the instance lock separate from Release data. This includes the default `dotnet run` and IDE Debug configurations. Existing data is not moved or copied between configurations.

Appearance preferences (`colourScheme` and `appearance`) and the Windows capture shortcut (`captureShortcut`) share `~/.thoughtfall/settings.json`, separately from diary data. Missing or unknown appearance preferences independently default to Forest and System. Saving either feature preserves the other settings; read and write failures are shown in the app.

The database is created on the first save or export. Schema version 1 contains a single `entries` table:

| Column | SQLite type | Meaning |
| --- | --- | --- |
| `id` | TEXT, primary key, not null | Entry UUID |
| `timestamp` | TEXT, not null | Round-trip ISO 8601 timestamp with original UTC offset |
| `text` | TEXT, not null | Exact submitted text |

Schema creation is transactional, entries use parameterized inserts, and connections use SQLite's FULL synchronous setting. Database access is serialized within the application and performed off the UI thread. Unknown schema versions are rejected rather than overwritten. There are no tags, synchronization, or encryption features.

The folder also contains `.instance.lock`. Ownership comes from its open file handle, not its existence; leave it in place after exit. SQLite may create journal files while writing. Quit the application before copying the data folder for a backup.

**Existing JSONL diaries are not imported, moved, or deleted.** The former `~/.thoughfall/data.jsonl` is not used as a fallback. This release starts a new SQLite diary at the location above.

## Export

Choose an inclusive date range and either Structured JSON or Markdown, then select a local destination outside the private data folder. The default range is the current month through today. A snapshot is read before the picker opens, so later saves and unsaved drafts are excluded.

Dates use each entry's original UTC offset. Entries are ordered by recorded local date/time, then instant, retaining insertion order for exact timestamp ties.

- JSON groups entries by year, month, and day. Each entry retains `id`, `timestamp`, and `text`; timestamps use seven fractional digits and the original offset.
- Markdown places note text beneath year/month/day headings formatted for your regional calendar. Text line endings are normalized to LF.

Exports are written to a temporary file alongside the destination, flushed, then moved into place. Failed formatting or writing leaves an existing destination intact. Export never writes into the private data folder, including through directory links.

## Code structure

| Project | Responsibility |
| --- | --- |
| `Thoughtfall.Desktop` | Startup, application lifetime, code-behind windows, tray, file picker, and single-instance activation |
| `Thoughtfall.Application` | Typed entries, SQLite storage, inclusive selection, and JSON/Markdown formatting |
| `Thoughtfall.Tests` | Public-method unit tests using in-memory inputs and outputs |

Startup constructs dependencies directly. `ApplicationController` owns desktop coordination; `TrayMenu` owns the tray; each window owns its UI interactions. `DiaryStore` owns database access and export destination policy. `DiaryEntries` and `DiaryExport` provide in-memory selection and formatting. There is no DI container, generic repository, or UI test framework.

## Build and unit tests

```sh
dotnet build Thoughtfall.slnx -c Release -warnaserror
dotnet test Thoughtfall.Tests -c Release --no-build --results-directory artifacts/test-results
```

Tests cover entry invariants, exact Unicode/large-text export, inclusive recorded-offset dates, clock-change ordering, JSON structure, localized Markdown headings/newlines, and caller-owned stream/writer lifetime. They call public members with in-memory values only.

There are no UI mocks, headless windows, filesystem/database tests, process launches, or environment simulations. Passing tests do **not** establish that native UI, SQLite integration, file replacement, or cross-process activation work on a particular desktop. Those areas require source review and separate user validation.

If a restricted environment prevents NUnit from initializing its working directory, run the same unit-test command with the necessary runner permissions. Avalonia's build-time telemetry can be disabled for a shell session with `AVALONIA_TELEMETRY_OPTOUT=1`; this does not disable diagnostics or runtime functionality.

## Formatting

```sh
dotnet tool restore
dotnet husky install
dotnet csharpier format .
```

CSharpier formats C# and XML/XAML. The pre-commit hook formats staged files. Write comments for meaningful contracts and non-obvious decisions; keep related methods together.

## Portable publishing

Publish self-contained folders; copy the entire output directory, including native SQLite and Avalonia libraries:

```sh
dotnet publish Thoughtfall.Desktop -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -o artifacts/publish/win-x64 -warnaserror
dotnet publish Thoughtfall.Desktop -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -o artifacts/publish/linux-x64 -warnaserror
```

Run `Thoughtfall.Desktop.exe` on Windows or `Thoughtfall.Desktop` on Linux. Linux still needs its desktop/native system dependencies; a cross-platform publish does not validate those. Windows installer builds are described in [Publishing](docs/PUBLISHING.md).
