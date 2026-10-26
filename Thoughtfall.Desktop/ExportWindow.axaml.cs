namespace Thoughtfall.Desktop;

using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Thoughtfall.Application;

/// <summary>Collects export options and writes a saved-entry snapshot.</summary>
internal partial class ExportWindow : Window
{
    private readonly DiaryStore? _store;
    private bool _exporting;
    private bool _acceptingOperations = true;

    /// <summary>Initializes the export UI without diary access for the XAML designer.</summary>
    public ExportWindow()
    {
        InitializeComponent();
        FromDate.DateValidationError += OnInvalidDate;
        ToDate.DateValidationError += OnInvalidDate;
        var today = DateTime.Today;
        FromDate.SelectedDate = new DateTime(today.Year, today.Month, 1);
        ToDate.SelectedDate = today;
    }

    /// <summary>Initializes a new instance of the <see cref="ExportWindow"/> class.</summary>
    /// <param name="store">The store providing snapshots and export writes.</param>
    internal ExportWindow(DiaryStore store)
        : this() => _store = store;

    /// <summary>Gets the most recent export task, including picker interaction, or null before the first export.</summary>
    internal Task<bool>? ActiveExport { get; private set; }

    /// <summary>Restores and activates the export window.</summary>
    internal void ShowExport()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    /// <summary>Enables or blocks new export requests without interrupting current work.</summary>
    /// <param name="accepting">Whether new exports are allowed.</param>
    internal void SetAcceptingOperations(bool accepting)
    {
        _acceptingOperations = accepting;
        UpdateControls();
    }

    /// <inheritdoc />
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (_exporting)
        {
            e.Cancel = true;
        }

        base.OnClosing(e);
    }

    private async void OnExport(object? sender, RoutedEventArgs e)
    {
        if (_exporting || !_acceptingOperations || !TryGetRange(out var from, out var to))
        {
            return;
        }

        var format = FormatPicker.SelectedIndex == 1 ? ExportFormat.Markdown : ExportFormat.Json;
        ActiveExport = ExportAsync(from, to, format);
        await ActiveExport;
    }

    private async Task<bool> ExportAsync(DateOnly from, DateOnly to, ExportFormat format)
    {
        if (_store is null)
            return false;

        _exporting = true;
        UpdateControls();
        SetStatus("Reading entries…");
        try
        {
            var entries = await _store.ReadRangeAsync(from, to);
            if (entries.Count == 0)
            {
                SetStatus("No entries found in this date range.");
                return true;
            }

            if (!StorageProvider.CanSave)
            {
                throw new IOException("A save file picker is unavailable on this desktop.");
            }

            var options = CreateSaveOptions(from, to, format);
            options.SuggestedStartLocation = await StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
            using var file = await StorageProvider.SaveFilePickerAsync(options);
            if (file is null)
            {
                SetStatus(null);
                return true;
            }

            var destination = file.TryGetLocalPath() ?? throw new IOException("Choose a local file for your export.");
            SetStatus("Exporting entries…");
            await _store.ExportAsync(destination, entries, format);
            SetStatus($"Exported {entries.Count} {(entries.Count == 1 ? "entry" : "entries")} to:\n{destination}");
            return true;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            SetStatus($"Couldn't export entries.\n{ex.Message}\nYour saved entries have not been changed. You can retry.");
            return false;
        }
        catch (Exception ex)
        {
            ErrorReporting.Unexpected("exporting entries", ex);
            SetStatus("An unexpected error interrupted the export. Your saved entries have not been changed.");
            return false;
        }
        finally
        {
            _exporting = false;
            UpdateControls();
        }
    }

    private static FilePickerSaveOptions CreateSaveOptions(DateOnly from, DateOnly to, ExportFormat format)
    {
        var extension = format == ExportFormat.Json ? "json" : "md";
        return new FilePickerSaveOptions
        {
            Title = "Export Thoughtfall",
            SuggestedFileName = FormattableString.Invariant($"Thoughtfall-{from:yyyy-MM-dd}-to-{to:yyyy-MM-dd}.{extension}"),
            DefaultExtension = extension,
            FileTypeChoices =
            [
                new FilePickerFileType(format == ExportFormat.Json ? "Structured JSON" : "Markdown") { Patterns = [$"*.{extension}"] },
            ],
            ShowOverwritePrompt = true,
        };
    }

    private bool TryGetRange(out DateOnly from, out DateOnly to)
    {
        from = default;
        to = default;
        // Typed text must agree with the selection; an invalid edit must not export the old date.
        if (!TryGetDate(FromDate, out var first) || !TryGetDate(ToDate, out var last))
        {
            SetStatus("Choose a valid From date and To date.");
            return false;
        }

        from = DateOnly.FromDateTime(first);
        to = DateOnly.FromDateTime(last);
        if (from > to)
        {
            SetStatus("The From date must be on or before the To date.");
            return false;
        }

        return true;
    }

    private static bool TryGetDate(CalendarDatePicker picker, out DateTime date)
    {
        date = default;
        return picker.SelectedDate is not null
            && DateTime.TryParse(picker.Text, CultureInfo.CurrentCulture, DateTimeStyles.None, out date)
            && date.Date == picker.SelectedDate.Value.Date;
    }

    private void UpdateControls()
    {
        DateFields.IsEnabled = FormatPicker.IsEnabled = ExportButton.IsEnabled = !_exporting && _acceptingOperations;
        CloseButton.IsEnabled = !_exporting;
    }

    private void SetStatus(string? text)
    {
        StatusText.Text = text;
        StatusBanner.IsVisible = !string.IsNullOrEmpty(text);
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close();

    private void OnInvalidDate(object? sender, CalendarDatePickerDateValidationErrorEventArgs e)
    {
        e.ThrowException = false;
        ((CalendarDatePicker)sender!).SelectedDate = null;
        SetStatus("Enter a valid date, or choose one from the calendar.");
    }
}
