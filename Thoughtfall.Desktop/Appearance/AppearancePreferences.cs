namespace Thoughtfall.Desktop.Appearance;

/// <summary>The selected theme, palette and editor font size; a null palette matches the system.</summary>
internal sealed record AppearancePreferences(ColourScheme ColourScheme = ColourScheme.Forest, string? PaletteId = null, int EditorFontSize = 16)
{
    internal static IReadOnlyList<int> EditorFontSizes { get; } = Array.AsReadOnly<int>([10, 11, 12, 14, 16, 18, 20, 24]);
}
