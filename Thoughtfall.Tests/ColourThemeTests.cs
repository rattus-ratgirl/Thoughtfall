namespace Thoughtfall.Tests;

using Avalonia.Platform;
using Avalonia.Styling;
using NUnit.Framework;
using Thoughtfall.Desktop;
using Thoughtfall.Desktop.Appearance;

/// <summary>Checks palette identities and unambiguous system assignments.</summary>
internal sealed class ColourThemeTests
{
    [Test]
    public void CatppuccinRegistersAllFourPalettesAndItsSystemAssignments()
    {
        var theme = ColourThemes.Get(ColourScheme.Catppuccin);
        Assert.Multiple(() =>
        {
            Assert.That(theme.Palettes.Select(palette => palette.Id), Is.EqualTo(new[] { "latte", "frappe", "macchiato", "mocha" }));
            Assert.That(theme.Palettes.Select(palette => palette.Name), Is.EqualTo(new[] { "Latte", "Frappé", "Macchiato", "Mocha" }));
            Assert.That(
                theme.Palettes.Select(palette => palette.BaseVariant),
                Is.EqualTo(new[] { ThemeVariant.Light, ThemeVariant.Dark, ThemeVariant.Dark, ThemeVariant.Dark })
            );
            Assert.That(theme.ResolveVariant(null, PlatformThemeVariant.Light), Is.SameAs(theme.GetVariant("latte")));
            Assert.That(theme.ResolveVariant(null, PlatformThemeVariant.Dark), Is.SameAs(theme.GetVariant("mocha")));
            Assert.That(ColourThemes.Get((ColourScheme)999).Id, Is.EqualTo(ColourScheme.Forest));
            Assert.That(new AppearancePreferences().ColourScheme, Is.EqualTo(ColourScheme.Forest));
        });
    }

    [Test]
    public void ArbitraryNamesAndMultiplePalettesResolveIndependentlyOfTheirFallbacks()
    {
        var theme = new ColourTheme(ColourScheme.Forest, "Fixture", [Palette("dawn"), Palette("mist"), Palette("dusk")], "dawn", "dusk");
        Assert.Multiple(() =>
        {
            Assert.That(theme.ResolveVariant("mist", PlatformThemeVariant.Light), Is.SameAs(theme.GetVariant("mist")));
            Assert.That(theme.ResolveVariant("mist", PlatformThemeVariant.Dark), Is.SameAs(theme.GetVariant("mist")));
            Assert.That(theme.ResolveVariant(null, PlatformThemeVariant.Light), Is.SameAs(theme.GetVariant("dawn")));
            Assert.That(theme.ResolveVariant(null, PlatformThemeVariant.Dark), Is.SameAs(theme.GetVariant("dusk")));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void OneSystemPaletteCanServeBothAppearances(bool extraPalette)
    {
        ColourPalette[] palettes = extraPalette ? [Palette("only"), Palette("alternative")] : [Palette("only")];
        var theme = new ColourTheme(ColourScheme.Forest, "Fixture", palettes, "only", "only");
        Assert.That(theme.ResolveVariant(null, PlatformThemeVariant.Light), Is.SameAs(theme.ResolveVariant(null, PlatformThemeVariant.Dark)));
    }

    [Test]
    public void InvalidDefinitionsAreRejected()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => new ColourTheme(ColourScheme.Forest, "Fixture", [], "missing", "missing"));
            Assert.Throws<ArgumentException>(() => new ColourTheme(ColourScheme.Forest, "Fixture", [Palette("one"), Palette("one")], "one", "one"));
            Assert.Throws<ArgumentException>(() => new ColourTheme(ColourScheme.Forest, "Fixture", [Palette("one")], "one", "missing"));
            Assert.Throws<ArgumentException>(() => new ColourTheme(ColourScheme.Forest, "Fixture", [Palette("one")], "missing", "one"));
            Assert.Throws<ArgumentException>(() => new ColourTheme(ColourScheme.Forest, "Fixture", [Palette("")], "", ""));
            Assert.Throws<ArgumentException>(() =>
                new ColourTheme(ColourScheme.Forest, "Fixture", [Palette("one") with { Name = " " }], "one", "one")
            );
            Assert.Throws<ArgumentException>(() =>
                new ColourTheme(ColourScheme.Forest, "Fixture", [Palette("one") with { BaseVariant = ThemeVariant.Default }], "one", "one")
            );
        });
    }

    private static ColourPalette Palette(string id) => new(id, id, ThemeVariant.Light, new Uri("avares://Thoughtfall.Tests/unused.axaml"));
}
