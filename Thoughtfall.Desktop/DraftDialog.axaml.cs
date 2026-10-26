namespace Thoughtfall.Desktop;

using Avalonia.Controls;
using Avalonia.Interactivity;

/// <summary>Asks whether to save, discard, or keep an unsaved draft.</summary>
internal partial class DraftDialog : Window
{
    /// <summary>Initializes a new instance of the <see cref="DraftDialog"/> class.</summary>
    public DraftDialog() => InitializeComponent();

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(DraftDecision.Cancel);

    private void OnDiscard(object? sender, RoutedEventArgs e) => Close(DraftDecision.Discard);

    private void OnSave(object? sender, RoutedEventArgs e) => Close(DraftDecision.Save);
}
