namespace Thoughtfall.Desktop;

using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Thoughtfall.Application;

/// <summary>Coordinates desktop windows, tray actions, and orderly shutdown.</summary>
internal sealed class ApplicationController : IDisposable
{
    private readonly DiaryStore _store;
    private readonly IClassicDesktopStyleApplicationLifetime _desktop;
    private readonly DesktopInstance _instance;
    private readonly TrayMenu _tray;
    private readonly CaptureShortcutSettings? _shortcutSettings;
    private CaptureShortcut? _committedShortcut;
    private WindowsCaptureShortcut? _windowsShortcut;
    private CaptureShortcutDialog? _shortcutDialog;
    private string? _shortcutError;
    private ExportWindow? _export;
    private Task _quitTask = Task.CompletedTask;
    private bool _quitting;
    private bool _exiting;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="ApplicationController"/> class.</summary>
    /// <param name="app">The application hosting the tray.</param>
    /// <param name="store">The shared diary store.</param>
    /// <param name="desktop">The desktop lifetime to shut down.</param>
    /// <param name="instance">The activation listener to stop before exit.</param>
    internal ApplicationController(
        Avalonia.Application app,
        DiaryStore store,
        IClassicDesktopStyleApplicationLifetime desktop,
        DesktopInstance instance
    )
    {
        _store = store;
        _desktop = desktop;
        _instance = instance;
        Capture = new CaptureWindow(store);
        _tray = new TrayMenu(app);
        Capture.CanHide = _tray.CanHideCapture;
        Capture.ExportRequested += OnExport;
        Capture.OpenFolderRequested += OnOpenFolder;
        Capture.QuitRequested += OnQuit;
        Capture.CaptureShortcutRequested += OnCaptureShortcut;
        _tray.CaptureRequested += OnCapture;
        _tray.ExportRequested += OnExport;
        _tray.OpenFolderRequested += OnOpenFolder;
        _tray.QuitRequested += OnQuit;
        _tray.CaptureShortcutRequested += OnCaptureShortcut;
        desktop.ShutdownRequested += OnShutdownRequested;
        if (OperatingSystem.IsWindows())
        {
            _shortcutSettings = new CaptureShortcutSettings(store.Folder);
            _committedShortcut = _shortcutSettings.Load(out _shortcutError);
            Capture.Opened += OnCaptureOpened;
        }
    }

    /// <summary>Gets the application's capture window.</summary>
    internal CaptureWindow Capture { get; }

    /// <summary>Activates capture unless shutdown has begun.</summary>
    internal void ActivateCapture()
    {
        if (!_quitting && !_exiting && !_disposed)
        {
            if (_shortcutDialog is not null)
            {
                _shortcutDialog.Activate();
            }
            else if (Capture.IsEnabled && _export?.ActiveExport is not { IsCompleted: false })
            {
                // Do not take focus from a modal dialog or an export's native save picker.
                Capture.ShowCapture();
            }
        }
    }

    private void OnCaptureOpened(object? sender, EventArgs e)
    {
        Capture.Opened -= OnCaptureOpened;
        // Wait for the capture window to own a valid HWND. Hidden/minimized capture retains it.
        _windowsShortcut = new WindowsCaptureShortcut(Capture, ActivateCapture);
        var registrationError = _windowsShortcut.Register(_committedShortcut);
        if (registrationError is not null)
        {
            _shortcutError = string.IsNullOrEmpty(_shortcutError) ? registrationError : $"{_shortcutError}\n{registrationError}";
        }

        if (_shortcutError is not null)
        {
            Capture.SetStatus($"{_shortcutError}\nUse Capture shortcut… to change or disable it.");
        }
    }

    private async void OnCaptureShortcut(object? sender, EventArgs e)
    {
        if (_quitting || _exiting || _disposed || _shortcutSettings is null)
        {
            return;
        }

        if (_shortcutDialog is not null)
        {
            _shortcutDialog.Activate();
            return;
        }

        if (!Capture.IsEnabled || _export?.ActiveExport is { IsCompleted: false })
        {
            return;
        }

        Capture.ShowCapture();
        var suspensionError = _windowsShortcut!.Suspend();
        var dialog = new CaptureShortcutDialog(_committedShortcut, SaveCaptureShortcut, suspensionError ?? _shortcutError);
        _shortcutDialog = dialog;
        try
        {
            await dialog.ShowDialog(Capture);
        }
        finally
        {
            _shortcutDialog = null;
            if (!dialog.Saved && !_exiting && !_disposed)
            {
                // Cancellation abandons edits, but restoring the committed registration can still fail.
                var restorationError = _windowsShortcut.Register(_committedShortcut);
                if (restorationError is not null)
                {
                    _shortcutError = $"Couldn't restore the previous capture shortcut.\n{restorationError}";
                    Capture.SetStatus(_shortcutError);
                }
            }
        }
    }

    private string? SaveCaptureShortcut(CaptureShortcut? candidate)
    {
        var registrationError = _windowsShortcut!.Register(candidate);
        if (registrationError is not null)
        {
            return registrationError;
        }

        var settingsError = _shortcutSettings!.Save(candidate);
        if (settingsError is not null)
        {
            // Leave the committed preference untouched and release the uncommitted candidate.
            var releaseError = _windowsShortcut.Suspend();
            return releaseError is null ? settingsError : $"{settingsError}\n{releaseError}";
        }

        _committedShortcut = candidate;
        if (_shortcutError is not null)
        {
            Capture.SetStatus(null);
        }

        _shortcutError = null;
        return null;
    }

    /// <summary>Reports that shortcut activation has stopped working.</summary>
    internal void ShowActivationError()
    {
        if (!_exiting && !_disposed)
        {
            Capture.SetStatus("Desktop shortcut activation stopped unexpectedly. You can still open capture from the tray or taskbar.");
            ActivateCapture();
        }
    }

    private Task QuitAsync()
    {
        if (_quitTask.IsCompleted && !_exiting)
        {
            _quitTask = QuitCoreAsync();
        }

        return _quitTask;
    }

    private async Task QuitCoreAsync()
    {
        _quitting = true;
        _shortcutDialog?.Close();
        Capture.SetAcceptingOperations(false);
        _export?.SetAcceptingOperations(false);
        try
        {
            // Retain the tasks before awaiting: their windows may finish or close in the meantime.
            var save = Capture.ActiveSave is { IsCompleted: false } activeSave ? activeSave : null;
            var export = _export?.ActiveExport is { IsCompleted: false } activeExport ? activeExport : null;
            if (export is not null)
            {
                _export!.ShowExport();
            }

            var saved = save is null || await save;
            var exported = export is null || await export;
            if (!saved || !exported)
            {
                if (!exported)
                {
                    _export?.ShowExport();
                }
                else
                {
                    Capture.ShowCapture();
                }

                return;
            }

            if (Capture.HasDraft)
            {
                Capture.ShowCapture();
                var answer = await new DraftDialog().ShowDialog<DraftDecision>(Capture);
                if (answer == DraftDecision.Cancel || (answer == DraftDecision.Save && !await Capture.SaveEntryAsync()))
                {
                    return;
                }
            }

            await _instance.StopAsync();
            _exiting = true;
            _desktop.Shutdown();
        }
        catch (Exception ex)
        {
            ErrorReporting.Unexpected("quitting", ex);
            Capture.SetStatus("An unexpected error interrupted quitting. The application is still open.");
            Capture.ShowCapture();
        }
        finally
        {
            _quitting = false;
            if (!_exiting && !_disposed)
            {
                Capture.SetAcceptingOperations(true);
                _export?.SetAcceptingOperations(true);
            }
        }
    }

    private void OnCapture(object? sender, EventArgs e) => ActivateCapture();

    private void OnExport(object? sender, EventArgs e)
    {
        if (_quitting || _exiting)
        {
            return;
        }

        if (_export is null)
        {
            _export = new ExportWindow(_store);
            _export.Closed += (_, _) => _export = null;
        }

        _export.ShowExport();
    }

    private async void OnOpenFolder(object? sender, EventArgs e)
    {
        if (_quitting || _exiting)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(_store.Folder);
            if (!await Capture.Launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(_store.Folder)))
            {
                throw new IOException("No file manager accepted the data folder.");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            Capture.SetStatus($"Couldn't open {_store.Folder}.\n{ex.Message}");
            ActivateCapture();
        }
        catch (Exception ex)
        {
            ErrorReporting.Unexpected("opening the data folder", ex);
            Capture.SetStatus("An unexpected error prevented opening the data folder.");
            ActivateCapture();
        }
    }

    private async void OnQuit(object? sender, EventArgs e) => await QuitAsync();

    private async void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        if (!_exiting)
        {
            e.Cancel = true;
            await QuitAsync();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Capture.Opened -= OnCaptureOpened;
        _desktop.ShutdownRequested -= OnShutdownRequested;
        try
        {
            _shortcutDialog?.Close();
            _windowsShortcut?.Dispose();
            _tray.Dispose();
        }
        finally
        {
            try
            {
                _export?.Close();
            }
            finally
            {
                Capture.CloseForExit();
            }
        }
    }
}
