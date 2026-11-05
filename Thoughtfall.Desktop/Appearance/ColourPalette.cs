namespace Thoughtfall.Desktop.Appearance;

using Avalonia.Styling;

/// <summary>A named palette with an explicit fallback for uncustomised Fluent resources.</summary>
internal sealed record ColourPalette(string Id, string Name, ThemeVariant BaseVariant, Uri Source);
