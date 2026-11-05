namespace Thoughtfall.Desktop.Appearance;

using Avalonia.Controls;
using Avalonia.Interactivity;

/// <summary>Keeps appearance edits local until Save succeeds.</summary>
internal partial class AppearanceDialog : Window
{
    private readonly Func<AppearancePreferences, string?>? _save;

    /// <summary>Initialises appearance controls without settings access for the XAML designer.</summary>
    public AppearanceDialog()
    {
        InitializeComponent();
        SchemePicker.ItemsSource = ColourThemes.All;
        SchemePicker.SelectedItem = ColourThemes.Get(new AppearancePreferences().ColourScheme);
        SetPalettes(null);
        SchemePicker.SelectionChanged += (_, _) => SetPalettes(null);
    }

    internal AppearanceDialog(AppearancePreferences saved, Func<AppearancePreferences, string?> save, string? error)
        : this()
    {
        _save = save;
        SchemePicker.SelectedItem = ColourThemes.Get(saved.ColourScheme);
        SetPalettes(saved.PaletteId);
        SetError(error);
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        if (_save is null || SchemePicker.SelectedItem is not ColourTheme theme || ModePicker.SelectedItem is not PaletteChoice palette)
            return;

        var preferences = new AppearancePreferences(theme.Id, palette.Id);
        var error = _save(preferences);
        if (error is not null)
        {
            SetError(error);
            return;
        }

        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();

    private void SetPalettes(string? selectedId)
    {
        if (SchemePicker.SelectedItem is not ColourTheme theme)
            return;

        PaletteChoice[] choices = [new(null, "Match System"), .. theme.Palettes.Select(palette => new PaletteChoice(palette.Id, palette.Name))];
        ModePicker.ItemsSource = choices;
        ModePicker.SelectedItem = choices.FirstOrDefault(choice => choice.Id == selectedId) ?? choices[0];
    }

    private void SetError(string? error)
    {
        ErrorText.Text = error;
        ErrorBanner.IsVisible = !string.IsNullOrEmpty(error);
    }

    private sealed record PaletteChoice(string? Id, string Name)
    {
        public override string ToString() => Name;
    }
}
