namespace Thoughtfall.Desktop.Capture;

using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Thoughtfall.Desktop.Diagnostics;

internal partial class CaptureWindow
{
    private const int HistoryPageSize = 30;
    private readonly ObservableCollection<CaptureHistoryEntry> _history = [];
    private readonly Stopwatch _historyScrollDelay = new();
    private readonly DispatcherTimer _historyHintTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private bool _historyReady;
    private bool _refreshingHistory;
    private bool _loadingOlder;
    private bool _hasOlder;
    private bool _historyUnlocked;
    private int _historyGeneration;
    private string? _historyError;

    internal Task HistoryLoad { get; private set; } = Task.CompletedTask;

    private void InitialiseHistory()
    {
        History.ItemsSource = _history;
        EntryScroller.ScrollChanged += OnEntryScrollChanged;
        EntryScroller.AddHandler(PointerWheelChangedEvent, OnHistoryWheel, RoutingStrategies.Tunnel);
        _historyHintTimer.Tick += (_, _) => ResetHistoryGate();
    }

    private void ResetHistoryGate()
    {
        _historyHintTimer.Stop();
        _historyScrollDelay.Reset();
        _historyUnlocked = false;
        HistoryHint.IsVisible = false;
    }

    private void OnHistoryWheel(object? sender, PointerWheelEventArgs e)
    {
        if (e.Delta.Y <= 0 || _historyUnlocked || _history.Count == 0)
            return;

        if (EntryScroller.Offset.Y > Editor.Bounds.Top + 1)
        {
            // A large wheel delta must not jump straight from a long draft into history.
            EntryScroller.Offset = new Vector(0, Math.Max(Editor.Bounds.Top, EntryScroller.Offset.Y - e.Delta.Y * 50));
            e.Handled = true;
            return;
        }

        _historyHintTimer.Stop();
        if (!_historyScrollDelay.IsRunning)
            _historyScrollDelay.Start();

        if (_historyScrollDelay.Elapsed >= TimeSpan.FromSeconds(1))
        {
            _historyUnlocked = true;
            HistoryHint.IsVisible = false;
            return;
        }

        HistoryHint.IsVisible = true;
        _historyHintTimer.Start();
        e.Handled = true;
    }

    internal async Task RefreshHistoryAsync()
    {
        if (_store is null)
            return;

        var generation = ++_historyGeneration;
        _refreshingHistory = true;
        ResetHistoryGate();
        try
        {
            var entries = await _store.ReadPreviousAsync(null, HistoryPageSize);
            if (generation != _historyGeneration)
                return;

            _history.Clear();
            foreach (var entry in entries)
                _history.Add(new CaptureHistoryEntry(entry));
            _hasOlder = entries.Count == HistoryPageSize;
            _historyReady = true;
            _historyError = null;
            EntryStream.UpdateLayout();
            EntryScroller.Offset = new Vector(0, Editor.Bounds.Top);
            RenderStatus();
        }
        catch (Exception ex)
        {
            if (generation == _historyGeneration)
                ShowHistoryError(ex);
        }
        finally
        {
            if (generation == _historyGeneration)
                _refreshingHistory = false;
        }
    }

    private void OnEntryScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_refreshingHistory || _loadingOlder)
            return;

        if (e.OffsetDelta.Y > 0 && EntryScroller.Offset.Y >= Editor.Bounds.Top - 1)
            ResetHistoryGate();

        if (_hasOlder && EntryScroller.Offset.Y < 100 && EntryScroller.Offset.Y < Editor.Bounds.Top)
            HistoryLoad = LoadOlderHistoryAsync();
    }

    private async Task LoadOlderHistoryAsync()
    {
        if (_store is null || _history.Count == 0)
            return;

        _loadingOlder = true;
        var generation = _historyGeneration;
        try
        {
            var entries = await _store.ReadPreviousAsync(_history[0].Entry.Id, HistoryPageSize);
            if (generation != _historyGeneration)
                return;

            // Keep the entry under the pointer in place while prepending the older page.
            var height = EntryScroller.Extent.Height;
            var offset = EntryScroller.Offset;
            for (var i = entries.Count - 1; i >= 0; i--)
                _history.Insert(0, new CaptureHistoryEntry(entries[i]));
            _hasOlder = entries.Count == HistoryPageSize;
            EntryStream.UpdateLayout();
            EntryScroller.Offset = new Vector(0, offset.Y + EntryScroller.Extent.Height - height);
            _historyError = null;
            RenderStatus();
        }
        catch (Exception ex)
        {
            if (generation == _historyGeneration)
            {
                _hasOlder = false;
                ShowHistoryError(ex);
            }
        }
        finally
        {
            _loadingOlder = false;
        }
    }

    private void ShowHistoryError(Exception ex)
    {
        if (ex is not (IOException or InvalidDataException or UnauthorizedAccessException or System.Security.SecurityException))
            ErrorReporting.Unexpected("loading previous entries", ex);
        _historyError = "Couldn't load previous entries. Reopen capture to try again. Your draft is still here.";
        RenderStatus();
    }
}
