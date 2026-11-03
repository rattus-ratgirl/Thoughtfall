namespace Thoughtfall.Desktop;

using Avalonia.Platform;
using Avalonia.Styling;

/// <summary>A named palette with an explicit fallback for uncustomised Fluent resources.</summary>
internal sealed record ColourPalette(string Id, string Name, ThemeVariant BaseVariant, Uri Source);

/// <summary>A theme's palettes and the one or two palettes used to match the system.</summary>
internal sealed class ColourTheme
{
    private readonly Dictionary<string, ThemeVariant> _variants;

    internal ColourTheme(ColourScheme id, string name, IReadOnlyList<ColourPalette> palettes, string systemLightPaletteId, string systemDarkPaletteId)
    {
        if (palettes.Count == 0)
            throw new ArgumentException("A theme must contain at least one palette.", nameof(palettes));

        Id = id;
        Name = name;
        Palettes = Array.AsReadOnly(palettes.ToArray());
        _variants = new Dictionary<string, ThemeVariant>(StringComparer.Ordinal);
        foreach (var palette in Palettes)
        {
            if (
                string.IsNullOrWhiteSpace(palette.Id)
                || string.IsNullOrWhiteSpace(palette.Name)
                || palette.BaseVariant != ThemeVariant.Light && palette.BaseVariant != ThemeVariant.Dark
                || !_variants.TryAdd(palette.Id, new ThemeVariant($"{id}/{palette.Id}", palette.BaseVariant))
            )
                throw new ArgumentException("Palettes need unique non-empty IDs, names, and a Light or Dark fallback.", nameof(palettes));
        }

        SystemLightPaletteId = systemLightPaletteId;
        SystemDarkPaletteId = systemDarkPaletteId;
        if (!HasPalette(systemLightPaletteId) || !HasPalette(systemDarkPaletteId))
            throw new ArgumentException("Both system appearances must reference a palette in this theme.");
    }

    internal ColourScheme Id { get; }

    internal string Name { get; }

    internal IReadOnlyList<ColourPalette> Palettes { get; }

    internal string SystemLightPaletteId { get; }

    internal string SystemDarkPaletteId { get; }

    internal bool HasPalette(string? id) => id is not null && _variants.ContainsKey(id);

    internal ThemeVariant GetVariant(string paletteId) => _variants[paletteId];

    internal ThemeVariant ResolveVariant(string? paletteId, PlatformThemeVariant systemAppearance) =>
        GetVariant(
            HasPalette(paletteId) ? paletteId!
            : systemAppearance == PlatformThemeVariant.Dark ? SystemDarkPaletteId
            : SystemLightPaletteId
        );

    public override string ToString() => Name;
}

/// <summary>The built-in themes. Palette names and system assignments are independent.</summary>
internal static class ColourThemes
{
    internal static IReadOnlyList<ColourTheme> All { get; } =
        Array.AsReadOnly<ColourTheme>([
            new(
                ColourScheme.Forest,
                "Forest",
                [Palette("Forest", "light", "Light", ThemeVariant.Light), Palette("Forest", "dark", "Dark", ThemeVariant.Dark)],
                "light",
                "dark"
            ),
            new(
                ColourScheme.Hearth,
                "Hearth",
                [Palette("Hearth", "light", "Light", ThemeVariant.Light), Palette("Hearth", "dark", "Dark", ThemeVariant.Dark)],
                "light",
                "dark"
            ),
        ]);

    internal static ColourTheme Get(ColourScheme id) =>
        All.FirstOrDefault(theme => theme.Id == id) ?? All.Single(theme => theme.Id == ColourScheme.Hearth);

    private static ColourPalette Palette(string theme, string id, string name, ThemeVariant baseVariant) =>
        new(id, name, baseVariant, new Uri($"avares://Thoughtfall.Desktop/Themes/Palettes/{theme}/{id}.axaml"));
}
