namespace Thoughtfall.Desktop;

using Avalonia.Styling;

/// <summary>The colour palette loaded at startup.</summary>
internal enum ColourScheme
{
    Forest,
    Hearth,
}

/// <summary>Whether windows follow the system or use a fixed light or dark appearance.</summary>
internal enum AppearanceMode
{
    System,
    Light,
    Dark,
}

/// <summary>The saved appearance choices, independent of the currently loaded palette.</summary>
internal sealed record AppearancePreferences(ColourScheme ColourScheme = ColourScheme.Hearth, AppearanceMode Appearance = AppearanceMode.System)
{
    internal ThemeVariant ThemeVariant =>
        Appearance switch
        {
            AppearanceMode.Light => ThemeVariant.Light,
            AppearanceMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
}
