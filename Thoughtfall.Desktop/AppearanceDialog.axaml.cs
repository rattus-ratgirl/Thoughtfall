namespace Thoughtfall.Desktop;

using Avalonia.Controls;
using Avalonia.Interactivity;

/// <summary>Keeps appearance edits local until Save succeeds.</summary>
internal partial class AppearanceDialog : Window
{
    private readonly Func<AppearancePreferences, string?>? _save;

    /// <summary>Initializes appearance controls without settings access for the XAML designer.</summary>
    public AppearanceDialog()
    {
        InitializeComponent();
        SchemePicker.SelectedIndex = 0;
        ModePicker.SelectedIndex = 0;
        SchemeStatus.Text = "Currently using Forest.";
    }

    internal AppearanceDialog(AppearancePreferences saved, ColourScheme loadedScheme, Func<AppearancePreferences, string?> save, string? error)
        : this()
    {
        _save = save;
        SchemePicker.SelectedIndex = saved.ColourScheme == ColourScheme.Hearth ? 1 : 0;
        ModePicker.SelectedIndex = saved.Appearance switch
        {
            AppearanceMode.Light => 1,
            AppearanceMode.Dark => 2,
            _ => 0,
        };
        SchemeStatus.Text =
            saved.ColourScheme == loadedScheme
                ? $"Currently using {loadedScheme}."
                : $"Restart pending: {saved.ColourScheme} is saved. Currently using {loadedScheme} until the next launch.";
        SetError(error);
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        if (_save is null)
            return;

        var preferences = new AppearancePreferences(
            SchemePicker.SelectedIndex == 1 ? ColourScheme.Hearth : ColourScheme.Forest,
            ModePicker.SelectedIndex switch
            {
                1 => AppearanceMode.Light,
                2 => AppearanceMode.Dark,
                _ => AppearanceMode.System,
            }
        );
        var error = _save(preferences);
        if (error is not null)
        {
            SetError(error);
            return;
        }

        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();

    private void SetError(string? error)
    {
        ErrorText.Text = error;
        ErrorBanner.IsVisible = !string.IsNullOrEmpty(error);
    }
}
