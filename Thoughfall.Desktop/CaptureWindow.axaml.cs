namespace Thoughfall.Desktop;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Thoughfall.Application;

internal partial class CaptureWindow : Window
{
    private readonly DiaryStore _store;
    private bool _closingForExit;
    private bool _saving;
    private bool _acceptingOperations = true;

    internal CaptureWindow(DiaryStore store)
    {
        _store = store;
        InitializeComponent();
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        EntryDate.Text = DateTime.Now.ToString("ddd, d MMM");
    }

    internal event EventHandler? ExportRequested;

    internal event EventHandler? OpenFolderRequested;

    internal event EventHandler? QuitRequested;

    internal bool CanHide { get; set; }

    internal bool HasDraft => !string.IsNullOrWhiteSpace(Editor.Text);

    internal Task<bool>? ActiveSave { get; private set; }

    internal void ShowCapture()
    {
        EntryDate.Text = DateTime.Now.ToString("ddd, d MMM");
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Dispatcher.UIThread.Post(() => Editor.Focus(), DispatcherPriority.Input);
    }

    internal void SetAcceptingOperations(bool accepting)
    {
        _acceptingOperations = accepting;
        UpdateControls();
    }

    internal Task<bool> SaveEntryAsync()
    {
        if (ActiveSave is { IsCompleted: false })
        {
            return ActiveSave;
        }

        ActiveSave = SaveCoreAsync();
        return ActiveSave;
    }

    private async Task<bool> SaveCoreAsync()
    {
        if (!HasDraft)
        {
            return false;
        }

        var text = Editor.Text!;
        _saving = true;
        UpdateControls();
        try
        {
            if (!await _store.SaveAsync(text))
            {
                return false;
            }

            Editor.Clear();
            SetStatus(null);
            HideCapture();
            return true;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            SetStatus($"Couldn't save to {_store.FilePath}.\n{ex.Message}\nYour text is still here. Fix the problem and try again.");
            ShowCapture();
            return false;
        }
        catch (Exception ex)
        {
            ErrorReporting.Unexpected("saving an entry", ex);
            SetStatus("An unexpected error interrupted saving. Your text is still here.");
            ShowCapture();
            return false;
        }
        finally
        {
            _saving = false;
            UpdateControls();
        }
    }

    internal void SetStatus(string? text)
    {
        StatusText.Text = text;
        StatusBanner.IsVisible = !string.IsNullOrEmpty(text);
    }

    internal void CloseForExit()
    {
        _closingForExit = true;
        Close();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!_closingForExit)
        {
            e.Cancel = true;
            HideCapture();
        }

        base.OnClosing(e);
    }

    private void HideCapture()
    {
        if (CanHide)
        {
            Hide();
        }
        else
        {
            WindowState = WindowState.Minimized;
        }
    }

    private void UpdateControls()
    {
        Editor.IsEnabled = !_saving && _acceptingOperations;
        SaveButton.IsEnabled = !_saving && _acceptingOperations && HasDraft;
    }

    private async void OnSave(object? sender, RoutedEventArgs e)
    {
        if (_acceptingOperations)
        {
            await SaveEntryAsync();
        }
    }

    private void OnExport(object? sender, RoutedEventArgs e) => ExportRequested?.Invoke(this, EventArgs.Empty);

    private void OnOpenFolder(object? sender, RoutedEventArgs e) => OpenFolderRequested?.Invoke(this, EventArgs.Empty);

    private void OnQuit(object? sender, RoutedEventArgs e) => QuitRequested?.Invoke(this, EventArgs.Empty);

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (SaveButton is not null)
        {
            UpdateControls();
        }
    }

    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.Control)
        {
            e.Handled = true;
            if (_acceptingOperations)
            {
                await SaveEntryAsync();
            }
        }
        else if (e.Key == Key.Escape && e.KeyModifiers == KeyModifiers.None)
        {
            e.Handled = true;
            HideCapture();
        }
    }
}
