namespace Thoughfall.Desktop;

using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Thoughfall.Application;

/// <summary>Coordinates desktop windows, tray actions, and orderly shutdown.</summary>
internal sealed class ApplicationController : IDisposable
{
    private readonly DiaryStore _store;
    private readonly IClassicDesktopStyleApplicationLifetime _desktop;
    private readonly DesktopInstance _instance;
    private readonly TrayMenu _tray;
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
        _tray.CaptureRequested += OnCapture;
        _tray.ExportRequested += OnExport;
        _tray.OpenFolderRequested += OnOpenFolder;
        _tray.QuitRequested += OnQuit;
        desktop.ShutdownRequested += OnShutdownRequested;
    }

    /// <summary>Gets the application's capture window.</summary>
    internal CaptureWindow Capture { get; }

    /// <summary>Activates capture unless shutdown has begun.</summary>
    internal void ActivateCapture()
    {
        if (!_quitting && !_exiting && !_disposed)
        {
            Capture.ShowCapture();
        }
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
        _desktop.ShutdownRequested -= OnShutdownRequested;
        try
        {
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
