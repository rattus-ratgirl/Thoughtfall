namespace Thoughtfall.Tests;

using System.Text.Json.Nodes;
using NUnit.Framework;
using Thoughtfall.Desktop;

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

    [TestCase("Light", "light")]
    [TestCase("Dark", "dark")]
    [TestCase("System", null)]
    [TestCase("unknown", null)]
    public void LegacyAppearanceMigratesWithoutWriting(string legacy, string? paletteId)
    {
        var json = new JsonObject { ["colourScheme"] = "Forest", ["appearance"] = legacy }.ToJsonString();
        File.WriteAllText(SettingsPath, json);
        var result = new DesktopSettings(_folder).LoadAppearance(out var error);
        Assert.Multiple(() =>
        {
            Assert.That(error, Is.Null);
            Assert.That(result, Is.EqualTo(new AppearancePreferences(ColourScheme.Forest, paletteId)));
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

    [TestCase("{}", ColourScheme.Hearth, null)]
    [TestCase("{\"colourScheme\":\"unknown\",\"palette\":\"dark\"}", ColourScheme.Hearth, "dark")]
    [TestCase("{\"colourScheme\":\"Forest\",\"palette\":\"unknown\"}", ColourScheme.Forest, null)]
    public void MissingOrUnknownChoicesFallBackIndependently(string json, ColourScheme scheme, string? paletteId)
    {
        File.WriteAllText(SettingsPath, json);
        Assert.That(new DesktopSettings(_folder).LoadAppearance(out var error), Is.EqualTo(new AppearancePreferences(scheme, paletteId)));
        Assert.That(error, Is.Null);
    }

    [TestCase(null)]
    [TestCase("light")]
    [TestCase("dark")]
    public void SavingRoundTripsAndPreservesOtherSettings(string? paletteId)
    {
        File.WriteAllText(SettingsPath, "{\"appearance\":\"Dark\",\"captureShortcut\":null,\"other\":{\"value\":17}}");
        var settings = new DesktopSettings(_folder);
        var preferences = new AppearancePreferences(ColourScheme.Forest, paletteId);
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

    [Test]
    public void CorruptSettingsAreReportedAndNeverOverwritten()
    {
        File.WriteAllText(SettingsPath, "broken json");
        var settings = new DesktopSettings(_folder);
        Assert.That(settings.LoadAppearance(out var error), Is.EqualTo(new AppearancePreferences()));
        Assert.That(error, Is.Not.Null);
        Assert.That(settings.SaveAppearance(new AppearancePreferences(ColourScheme.Forest, "dark")), Is.Not.Null);
        Assert.That(File.ReadAllText(SettingsPath), Is.EqualTo("broken json"));
    }
}
