namespace Thoughfall.Desktop;

using Avalonia.Controls;
using Avalonia.Interactivity;

internal partial class DraftDialog : Window
{
    internal DraftDialog() => InitializeComponent();

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(DraftDecision.Cancel);

    private void OnDiscard(object? sender, RoutedEventArgs e) => Close(DraftDecision.Discard);

    private void OnSave(object? sender, RoutedEventArgs e) => Close(DraftDecision.Save);
}
