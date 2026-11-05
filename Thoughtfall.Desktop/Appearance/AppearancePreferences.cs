namespace Thoughtfall.Desktop.Appearance;

/// <summary>The selected theme and palette; a null palette matches the system.</summary>
internal sealed record AppearancePreferences(ColourScheme ColourScheme = ColourScheme.Forest, string? PaletteId = null);
