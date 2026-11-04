namespace Thoughtfall.Desktop;

/// <summary>The available colour themes.</summary>
internal enum ColourScheme
{
    Forest,
    Hearth,
    Catppuccin,
}

/// <summary>The selected theme and palette; a null palette matches the system.</summary>
internal sealed record AppearancePreferences(ColourScheme ColourScheme = ColourScheme.Forest, string? PaletteId = null);
