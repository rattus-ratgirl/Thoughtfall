namespace Thoughtfall.Desktop.Appearance;

using Avalonia.Styling;

/// <summary>The built-in themes. Palette names and system assignments are independent.</summary>
internal static class ColourThemes
{
    internal static IReadOnlyList<ColourTheme> All { get; } =
        Array.AsReadOnly<ColourTheme>([
            new ColourTheme(
                ColourScheme.Forest,
                "Forest",
                [Palette("Forest", "light", "Light", ThemeVariant.Light), Palette("Forest", "dark", "Dark", ThemeVariant.Dark)],
                "light",
                "dark"
            ),
            new ColourTheme(
                ColourScheme.Hearth,
                "Hearth",
                [Palette("Hearth", "light", "Light", ThemeVariant.Light), Palette("Hearth", "dark", "Dark", ThemeVariant.Dark)],
                "light",
                "dark"
            ),
            new ColourTheme(
                ColourScheme.Catppuccin,
                "Catppuccin",
                [
                    Palette("Catppuccin", "latte", "Latte", ThemeVariant.Light),
                    Palette("Catppuccin", "frappe", "Frappé", ThemeVariant.Dark),
                    Palette("Catppuccin", "macchiato", "Macchiato", ThemeVariant.Dark),
                    Palette("Catppuccin", "mocha", "Mocha", ThemeVariant.Dark),
                ],
                "latte",
                "mocha"
            ),
        ]);

    internal static ColourTheme Get(ColourScheme id) =>
        All.FirstOrDefault(theme => theme.Id == id) ?? All.Single(theme => theme.Id == ColourScheme.Forest);

    private static ColourPalette Palette(string theme, string id, string name, ThemeVariant baseVariant) =>
        new(id, name, baseVariant, new Uri($"avares://Thoughtfall.Desktop/Appearance/Themes/Palettes/{theme}/{id}.axaml"));
}