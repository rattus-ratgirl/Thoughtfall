namespace Thoughtfall.Desktop.Shell;

using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Thoughtfall.Application;
using Thoughtfall.Desktop.Appearance;
using Thoughtfall.Desktop.Capture;
using Thoughtfall.Desktop.Diagnostics;
using Thoughtfall.Desktop.Export;
using Thoughtfall.Desktop.Settings;
using Thoughtfall.Desktop.Shortcuts;

/// <summary>Coordinates desktop windows, tray actions, and orderly shutdown.</summary>
internal sealed class ApplicationController : IDisposable
{
    private readonly DiaryStore _store;
    private readonly IClassicDesktopStyleApplicationLifetime _desktop;
    private readonly DesktopInstance _instance;
    private readonly TrayMenu _tray;
    private readonly App _app;
    private readonly DesktopSettings _settings;
    private string? _appearanceError;
    private Window? _activeDialog;
    private Task _dialogTask = Task.CompletedTask;
    private string? _activationError;
    private CaptureShortcut? _committedShortcut;
    private WindowsCaptureShortcut? _windowsShortcut;
    private string? _shortcutError;
    private ExportWindow? _export;
    private Task _quitTask = Task.CompletedTask;
    private Lifecycle _state;

    private enum Lifecycle
    {
        Running,
        Quitting,
        Exiting,
        Disposed,
    }

    /// <summary>Initialises a new instance of the <see cref="ApplicationController"/> class.</summary>
    /// <param name="app">The application hosting the tray.</param>
    /// <param name="store">The shared diary store.</param>
    /// <param name="desktop">The desktop lifetime to shut down.</param>
    /// <param name="instance">The activation listener to stop before exit.</param>
    /// <param name="settings">The shared desktop settings owner.</param>
    /// <param name="appearanceError">Any startup settings read error.</param>
    internal ApplicationController(
        App app,
        DiaryStore store,
        IClassicDesktopStyleApplicationLifetime desktop,
        DesktopInstance instance,
        DesktopSettings settings,
        string? appearanceError
    )
    {
        _store = store;
        _desktop = desktop;
        _instance = instance;
        _app = app;
        _settings = settings;
        _appearanceError = appearanceError;
        Capture = new CaptureWindow(store);
        RefreshWarnings();
        _tray = new TrayMenu(app);
        Capture.CanHide = _tray.CanHideCapture;
        Capture.ExportRequested += OnExport;
        Capture.OpenFolderRequested += OnOpenFolder;

        if (!Capture.CanHide)
            Capture.QuitRequested += OnQuit;

        Capture.CaptureShortcutRequested += OnCaptureShortcut;
        Capture.AppearanceRequested += OnAppearance;
        _tray.CaptureRequested += OnCapture;
        _tray.ExportRequested += OnExport;
        _tray.OpenFolderRequested += OnOpenFolder;
        _tray.QuitRequested += OnQuit;
        _tray.CaptureShortcutRequested += OnCaptureShortcut;
        _tray.AppearanceRequested += OnAppearance;
        desktop.ShutdownRequested += OnShutdownRequested;

        if (!OperatingSystem.IsWindows())
            return;

        _committedShortcut = _settings.LoadShortcut(out _shortcutError);
        RegisterCaptureShortcut();
    }

    /// <summary>Gets the application's capture window.</summary>
    private CaptureWindow Capture { get; }

    /// <summary>Opens capture only when requested or when no usable tray is available.</summary>
    internal void Start(bool captureRequested)
    {
        if (captureRequested || !Capture.CanHide)
            ActivateCapture();
    }

    /// <summary>Activates capture unless shutdown has begun.</summary>
    internal void ActivateCapture()
    {
        if (CanNavigate())
            Capture.ShowCapture();
    }

    private bool CanNavigate()
    {
        if (_state != Lifecycle.Running)
            return false;
        if (_activeDialog is not null)
            _activeDialog.Activate();
        else if (_export?.IsExporting == true)
            _export.ShowExport();
        else if (Capture.IsSaving)
            Capture.ShowCapture();
        else
            return true;
        return false;
    }

    private void UpdateAvailability()
    {
        var accepting = _state == Lifecycle.Running && _activeDialog is null;
        Capture.SetAcceptingOperations(accepting);
        _export?.SetAcceptingOperations(accepting);
    }

    private void RefreshWarnings() =>
        Capture.SetWarnings(
            string.Join(
                "\n\n",
                new[]
                {
                    _appearanceError,
                    _shortcutError is null ? null : $"{_shortcutError}\nUse Capture shortcut? to change or disable it.",
                    _activationError,
                }.Where(text => !string.IsNullOrEmpty(text))
            )
        );

    private void RegisterCaptureShortcut()
    {
        // Construction creates the native window; registration does not need to show capture.
        _windowsShortcut = new WindowsCaptureShortcut(Capture, ActivateCapture);
        var registrationError = _windowsShortcut.Register(_committedShortcut);

        if (registrationError is not null)
            _shortcutError = string.IsNullOrEmpty(_shortcutError) ? registrationError : $"{_shortcutError}\n{registrationError}";

        RefreshWarnings();
    }

    private async void OnCaptureShortcut(object? sender, EventArgs e)
    {
        if (!CanNavigate() || !OperatingSystem.IsWindows())
            return;
        _dialogTask = RunDialogAsync(ShowCaptureShortcutAsync);
        await _dialogTask;
    }

    private async Task ShowCaptureShortcutAsync()
    {
        CaptureShortcutDialog? dialog = null;
        try
        {
            Capture.ShowCapture();
            var suspensionError = _windowsShortcut!.Suspend();
            dialog = new CaptureShortcutDialog(_committedShortcut, SaveCaptureShortcut, suspensionError ?? _shortcutError);
            _activeDialog = dialog;
            UpdateAvailability();
            await dialog.ShowDialog(Capture);
        }
        finally
        {
            _activeDialog = null;
            if (dialog?.Saved != true && _state is Lifecycle.Running or Lifecycle.Quitting)
            {
                var restorationError = _windowsShortcut!.Register(_committedShortcut);
                if (restorationError is not null)
                {
                    var message = $"Couldn't restore the previous capture shortcut.\n{restorationError}";
                    _shortcutError = _shortcutError is null ? message : $"{_shortcutError}\n{message}";
                    RefreshWarnings();
                }
            }
            if (_state != Lifecycle.Disposed)
                UpdateAvailability();
        }
    }

    private string? SaveCaptureShortcut(CaptureShortcut? candidate)
    {
        var registrationError = _windowsShortcut!.Register(candidate);

        if (registrationError is not null)
            return registrationError;

        var settingsError = _settings.SaveShortcut(candidate);

        if (settingsError is not null)
        {
            // Leave the committed preference untouched and release the uncommitted candidate.
            var releaseError = _windowsShortcut.Suspend();
            return releaseError is null ? settingsError : $"{settingsError}\n{releaseError}";
        }

        _committedShortcut = candidate;

        _shortcutError = null;
        RefreshWarnings();
        return null;
    }

    private async void OnAppearance(object? sender, EventArgs e)
    {
        if (!CanNavigate())
            return;
        _dialogTask = RunDialogAsync(ShowAppearanceAsync);
        await _dialogTask;
    }

    private async Task RunDialogAsync(Func<Task> show)
    {
        try
        {
            await show();
        }
        catch (Exception ex)
        {
            ErrorReporting.Unexpected("opening settings", ex);
            if (_state is Lifecycle.Exiting or Lifecycle.Disposed)
                return;
            Capture.SetOperationStatus("An unexpected error interrupted settings. Please try again.");
            Capture.ShowCapture();
        }
        finally
        {
            _activeDialog = null;
            if (_state != Lifecycle.Disposed)
                UpdateAvailability();
        }
    }

    private async Task ShowAppearanceAsync()
    {
        Capture.ShowCapture();
        var dialog = new AppearanceDialog(_app.CurrentAppearance, SaveAppearance, _appearanceError);
        _activeDialog = dialog;
        UpdateAvailability();

        try
        {
            await dialog.ShowDialog(Capture);
        }
        finally
        {
            _activeDialog = null;
            if (_state != Lifecycle.Disposed)
                UpdateAvailability();
        }
    }

    private string? SaveAppearance(AppearancePreferences preferences)
    {
        var error = _settings.SaveAppearance(preferences);

        if (error is not null)
            return error;

        _app.ApplyAppearance(preferences);

        _appearanceError = null;
        RefreshWarnings();
        return null;
    }

    /// <summary>Reports that shortcut activation has stopped working.</summary>
    internal void ShowActivationError()
    {
        if (_state is Lifecycle.Exiting or Lifecycle.Disposed)
            return;

        _activationError = "Desktop shortcut activation stopped unexpectedly. You can still open capture from the tray or taskbar.";
        RefreshWarnings();
        ActivateCapture();
    }

    private Task QuitAsync()
    {
        if (_quitTask.IsCompleted && _state == Lifecycle.Running)
            _quitTask = QuitCoreAsync();

        return _quitTask;
    }

    private async Task QuitCoreAsync()
    {
        _state = Lifecycle.Quitting;
        UpdateAvailability();
        try
        {
            // Retain the tasks before awaiting: their windows may finish or close in the meantime.
            var save = Capture.ActiveSave is { IsCompleted: false } activeSave ? activeSave : null;
            var exportWindow = _export;
            var export = exportWindow?.ActiveExport is { IsCompleted: false } activeExport ? activeExport : null;

            var dialogTask = _dialogTask;
            _activeDialog?.Close();
            await dialogTask;

            if (export is not null)
                exportWindow!.ShowExport();

            var saved = save is null || await save;
            var exported = export is null || await export != ExportOutcome.Failed;

            if (!saved || !exported)
            {
                if (!exported)
                    exportWindow?.ShowExport();
                else
                    Capture.ShowCapture();

                return;
            }

            if (Capture.HasDraft)
            {
                Capture.ShowCapture();
                var answer = await new DraftDialog().ShowDialog<DraftDecision>(Capture);

                if (answer == DraftDecision.Cancel || (answer == DraftDecision.Save && !await Capture.SaveEntryAsync()))
                    return;
            }

            await _instance.StopAsync();
            _state = Lifecycle.Exiting;
            _desktop.Shutdown();
        }
        catch (Exception ex)
        {
            ErrorReporting.Unexpected("quitting", ex);
            if (_state == Lifecycle.Disposed)
                return;
            _state = Lifecycle.Quitting;
            Capture.SetOperationStatus("An unexpected error interrupted quitting. The application is still open.");
            Capture.ShowCapture();
        }
        finally
        {
            if (_state == Lifecycle.Quitting)
            {
                _state = Lifecycle.Running;
                UpdateAvailability();
            }
        }
    }

    private void OnCapture(object? sender, EventArgs e) => ActivateCapture();

    private void OnExport(object? sender, EventArgs e)
    {
        if (!CanNavigate())
            return;

        if (_export is null)
        {
            _export = new ExportWindow(_store);
            _export.Closed += (_, _) => _export = null;
        }

        _export.ShowExport();
    }

    private async void OnOpenFolder(object? sender, EventArgs e)
    {
        if (_state != Lifecycle.Running)
            return;

        try
        {
            Directory.CreateDirectory(_store.Folder);

            if (!await Capture.Launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(_store.Folder)))
                throw new IOException("No file manager accepted the data folder.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            if (_state != Lifecycle.Running)
                return;
            Capture.SetOperationStatus($"Couldn't open {_store.Folder}.\n{ex.Message}");
            ActivateCapture();
        }
        catch (Exception ex)
        {
            ErrorReporting.Unexpected("opening the data folder", ex);
            if (_state != Lifecycle.Running)
                return;
            Capture.SetOperationStatus("An unexpected error prevented opening the data folder.");
            ActivateCapture();
        }
    }

    private async void OnQuit(object? sender, EventArgs e) => await QuitAsync();

    private async void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        if (_state is Lifecycle.Exiting or Lifecycle.Disposed)
            return;

        e.Cancel = true;
        await QuitAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_state == Lifecycle.Disposed)
        {
            return;
        }

        _state = Lifecycle.Disposed;
        _desktop.ShutdownRequested -= OnShutdownRequested;

        try
        {
            _activeDialog?.Close();
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
