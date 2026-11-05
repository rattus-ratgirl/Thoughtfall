namespace Thoughtfall.Tests;

using System.Text.Json.Nodes;
using NUnit.Framework;
using Thoughtfall.Desktop;
using Thoughtfall.Desktop.Appearance;
using Thoughtfall.Desktop.Settings;
using Thoughtfall.Desktop.Shortcuts;

/// <summary>Checks settings migration and preserves unrelated desktop preferences.</summary>
internal sealed class AppearanceSettingsTests
{
    private string _folder = null!;
    private string SettingsPath => Path.Combine(_folder, "settings.json");

    [SetUp]
    public void CreateFolder()
    {
        _folder = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"appearance-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_folder);
    }

    [TearDown]
    public void RemoveFolder() => Directory.Delete(_folder, true);

    [TestCase("Forest", "Light", "light")]
    [TestCase("Forest", "Dark", "dark")]
    [TestCase("Forest", "System", null)]
    [TestCase("Forest", "unknown", null)]
    [TestCase("Hearth", "Light", "light")]
    [TestCase("Hearth", "Dark", "dark")]
    [TestCase("Hearth", "System", null)]
    public void LegacyAppearanceMigratesWithoutWriting(string scheme, string legacy, string? paletteId)
    {
        var json = new JsonObject { ["colourScheme"] = scheme, ["appearance"] = legacy }.ToJsonString();
        File.WriteAllText(SettingsPath, json);
        var result = new DesktopSettings(_folder).LoadAppearance(out var error);
        Assert.Multiple(() =>
        {
            Assert.That(error, Is.Null);
            Assert.That(result, Is.EqualTo(new AppearancePreferences(Enum.Parse<ColourScheme>(scheme), paletteId)));
            Assert.That(File.ReadAllText(SettingsPath), Is.EqualTo(json));
        });
    }

    [TestCase("\"light\"", "light")]
    [TestCase("\"dark\"", "dark")]
    [TestCase("null", null)]
    [TestCase("\"unknown\"", null)]
    [TestCase("42", null)]
    public void NewPaletteTakesPrecedenceOverLegacyAppearance(string jsonValue, string? paletteId)
    {
        File.WriteAllText(SettingsPath, "{\"colourScheme\":\"Forest\",\"appearance\":\"Dark\",\"palette\":" + jsonValue + "}");
        var result = new DesktopSettings(_folder).LoadAppearance(out var error);
        Assert.That(error, Is.Null);
        Assert.That(result, Is.EqualTo(new AppearancePreferences(ColourScheme.Forest, paletteId)));
    }

    [TestCase("{}", ColourScheme.Forest, null)]
    [TestCase("{\"colourScheme\":\"unknown\",\"palette\":\"dark\"}", ColourScheme.Forest, "dark")]
    [TestCase("{\"colourScheme\":\"Forest\",\"palette\":\"unknown\"}", ColourScheme.Forest, null)]
    [TestCase("{\"colourScheme\":\"Hearth\",\"palette\":\"dark\"}", ColourScheme.Hearth, "dark")]
    [TestCase("{\"colourScheme\":\"Catppuccin\",\"palette\":\"unknown\"}", ColourScheme.Catppuccin, null)]
    [TestCase("{\"colourScheme\":\"2\"}", ColourScheme.Forest, null)]
    public void MissingOrUnknownChoicesFallBackIndependently(string json, ColourScheme scheme, string? paletteId)
    {
        File.WriteAllText(SettingsPath, json);
        Assert.That(new DesktopSettings(_folder).LoadAppearance(out var error), Is.EqualTo(new AppearancePreferences(scheme, paletteId)));
        Assert.That(error, Is.Null);
    }

    [TestCase(ColourScheme.Forest, null)]
    [TestCase(ColourScheme.Forest, "light")]
    [TestCase(ColourScheme.Forest, "dark")]
    [TestCase(ColourScheme.Hearth, "dark")]
    [TestCase(ColourScheme.Catppuccin, null)]
    [TestCase(ColourScheme.Catppuccin, "latte")]
    [TestCase(ColourScheme.Catppuccin, "frappe")]
    [TestCase(ColourScheme.Catppuccin, "macchiato")]
    [TestCase(ColourScheme.Catppuccin, "mocha")]
    public void SavingRoundTripsAndPreservesOtherSettings(ColourScheme scheme, string? paletteId)
    {
        File.WriteAllText(SettingsPath, "{\"appearance\":\"Dark\",\"captureShortcut\":null,\"other\":{\"value\":17}}");
        var settings = new DesktopSettings(_folder);
        var preferences = new AppearancePreferences(scheme, paletteId);
        Assert.That(settings.SaveAppearance(preferences), Is.Null);
        var document = JsonNode.Parse(File.ReadAllText(SettingsPath))!.AsObject();
        Assert.Multiple(() =>
        {
            Assert.That(settings.LoadAppearance(out _), Is.EqualTo(preferences));
            Assert.That(document.ContainsKey("palette"), Is.True);
            Assert.That(document.ContainsKey("appearance"), Is.False);
            Assert.That(document.ContainsKey("captureShortcut"), Is.True);
            Assert.That(document["captureShortcut"], Is.Null);
            Assert.That((int)document["other"]!["value"]!, Is.EqualTo(17));
        });
        Assert.That(settings.SaveShortcut(CaptureShortcut.Default), Is.Null);
        Assert.That(settings.LoadAppearance(out _), Is.EqualTo(preferences));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void InvalidStoredShortcutCanBeReplacedOrDisabled(bool disable)
    {
        File.WriteAllText(SettingsPath, """{"captureShortcut":{"key":"unsupported"},"other":17}""");
        var settings = new DesktopSettings(_folder);
        var candidate = disable ? null : CaptureShortcut.Default;
        Assert.That(settings.LoadShortcut(out var error), Is.EqualTo(CaptureShortcut.Default));
        Assert.That(error, Is.Not.Null);
        Assert.That(settings.SaveShortcut(candidate), Is.Null);
        Assert.That(settings.LoadShortcut(out error), Is.EqualTo(candidate));
        Assert.That(error, Is.Null);
        Assert.That((int)JsonNode.Parse(File.ReadAllText(SettingsPath))!["other"]!, Is.EqualTo(17));
    }

    [Test]
    public void CorruptSettingsAreReportedAndNeverOverwritten()
    {
        File.WriteAllText(SettingsPath, "broken json");
        var settings = new DesktopSettings(_folder);
        Assert.That(settings.LoadAppearance(out var error), Is.EqualTo(new AppearancePreferences(ColourScheme.Forest)));
        Assert.That(error, Does.Contain("Forest"));
        Assert.That(settings.SaveAppearance(new AppearancePreferences(ColourScheme.Forest, "dark")), Is.Not.Null);
        Assert.That(File.ReadAllText(SettingsPath), Is.EqualTo("broken json"));
    }

    [Test]
    public void MissingSettingsUseForestWithoutCreatingAFile()
    {
        Assert.That(new DesktopSettings(_folder).LoadAppearance(out var error), Is.EqualTo(new AppearancePreferences(ColourScheme.Forest)));
        Assert.That(error, Is.Null);
        Assert.That(File.Exists(SettingsPath), Is.False);
    }
}
