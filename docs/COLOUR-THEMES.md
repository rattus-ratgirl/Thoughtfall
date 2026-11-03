# Colour themes

`Thoughtfall.Desktop/ColourThemes.cs` lists the built-in colour themes. Each
theme has an ordered collection of palettes and two system assignments:
`SystemLightPaletteId` and `SystemDarkPaletteId`. Both assignments are required
and must reference palettes in that theme. They can reference the same palette,
so exactly one or two palettes participate in **Match System**. A theme with
only one palette uses it for both assignments.

Each palette has a stable ID, a display name, a Light or Dark fallback, and a
resource URI. IDs are unique within their theme and are saved in settings;
display names can change independently. The fallback supplies uncustomised
Fluent resources and the native window appearance. It does not determine the
palette's name or its system assignment.

To add a palette to an existing theme:

1. Add a `Palette(...)` entry to that theme's collection in `ColourThemes.cs`.
2. Add `Themes/Palettes/<theme>/<id>.axaml`, using `PaletteResources` as the root.
3. Set `FluentColours` and provide the application brushes found in the existing
   palette files. Put any palette-specific Fluent brush overrides alongside them.
4. Change the system assignments only if the new palette should participate in
   Match System.

The picker lists all palettes automatically. Each receives a custom Avalonia
`ThemeVariant`, keyed by theme and palette ID. All built-in palette dictionaries
are loaded once, and saving a choice switches the requested variant immediately.
Match System resolves the appropriate assignment again when the OS appearance
changes. An explicit palette selection remains fixed.

`PaletteResources` uses a fresh Fluent resource dictionary for each palette.
Its resource adapter resolves requests using that palette's Light/Dark fallback,
which allows Fluent's existing colour-to-brush definitions to resolve the
palette's colours. A private `ThemeVariantScope` owns each Fluent resource
dictionary so dynamic accent bindings also resolve within that palette. These
resource dictionaries have separate static brush instances. The application's
single installed `FluentTheme` supplies the shared
control styles; switching palettes never replaces those styles. This avoids
copying Fluent's brush definitions or using reflection into its internals.

When upgrading Avalonia, run the appearance resource tests. They check compiled
palette resources, live controls, dropdowns, and several custom variants with the
same fallback. The implementation relies on Fluent's public resources retaining
the base-colour and control-brush structure used by the adapter.
