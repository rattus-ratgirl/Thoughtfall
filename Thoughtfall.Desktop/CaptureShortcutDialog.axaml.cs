namespace Thoughtfall.Desktop;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

/// <summary>Records a pending chord and keeps edits local until Save succeeds.</summary>
internal partial class CaptureShortcutDialog : Window
{
    private readonly Func<CaptureShortcut?, string?> _save;
    private CaptureShortcut? _pending;

    internal CaptureShortcutDialog(CaptureShortcut? committed, Func<CaptureShortcut?, string?> save, string? error)
    {
        _save = save;
        _pending = committed;
        InitializeComponent();
        Recorder.Content = _pending?.DisplayText ?? "Disabled";
        SetError(error);
        AddHandler(KeyDownEvent, OnRecorderKeyDown, RoutingStrategies.Tunnel);
        Opened += (_, _) => Recorder.Focus();
    }

    internal bool Saved { get; private set; }

    private void OnRecorderKeyDown(object? sender, KeyEventArgs e)
    {
        if (!Recorder.IsKeyboardFocusWithin || e.Key is Key.Tab or Key.Escape)
            return;

        // Consume recorder input before Button handles Enter/Space or the dialog handles access keys.
        e.Handled = true;
        var shortcut = CaptureShortcut.FromKey(e.Key, e.KeyModifiers);

        if (shortcut is null)
        {
            SaveButton.IsEnabled = false;
            Recorder.Content = "Press a supported combination";

            SetError(
                e.Key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift
                    ? "Add a letter, top-row digit or F1–F11 key to the modifiers."
                    : "Use Control or Alt, optionally Shift, plus a letter, top-row digit or F1–F11. Windows keys are not supported."
            );

            return;
        }

        SelectShortcut(shortcut);
    }

    private void SelectShortcut(CaptureShortcut? shortcut)
    {
        _pending = shortcut;
        Recorder.Content = shortcut?.DisplayText ?? "Disabled";
        SaveButton.IsEnabled = true;
        SetError(null);
    }

    private void OnClear(object? sender, RoutedEventArgs e) => SelectShortcut(null);

    private void OnRestoreDefault(object? sender, RoutedEventArgs e) => SelectShortcut(CaptureShortcut.Default);

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        var error = _save(_pending);

        if (error is not null)
        {
            SetError(error);
            return;
        }

        Saved = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();

    private void SetError(string? error)
    {
        ErrorText.Text = error;
        ErrorBanner.IsVisible = !string.IsNullOrEmpty(error);
    }
}
