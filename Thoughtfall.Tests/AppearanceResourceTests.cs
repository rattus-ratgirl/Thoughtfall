namespace Thoughtfall.Tests;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using Thoughtfall.Desktop;
using Thoughtfall.Desktop.Appearance;

/// <summary>Exercises the compiled application resources and live variant switching.</summary>
[NonParallelizable]
internal sealed class AppearanceResourceTests
{
    [OneTimeSetUp]
    public void StartAvalonia() =>
        AppBuilder
            .Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .WithInterFont()
            .SetupWithoutStarting();

    [TearDown]
    public void RestoreAppearance() => ((App)Application.Current!).ApplyAppearance(new AppearancePreferences());

    [TestCase(ColourScheme.Hearth, "light", "#F8F5EF", "#40332E", "#9B503A")]
    [TestCase(ColourScheme.Hearth, "dark", "#24211F", "#E9DED0", "#E3A58B")]
    [TestCase(ColourScheme.Forest, "light", "#F5F2EA", "#233F35", "#206454")]
    [TestCase(ColourScheme.Forest, "dark", "#174B3F", "#F5F2EA", "#E7B88A")]
    public void PaletteContainsApplicationAndFluentBrushes(ColourScheme scheme, string paletteId, string page, string text, string accent)
    {
        var app = (App)Application.Current!;
        app.ApplyAppearance(new AppearancePreferences(scheme, paletteId));
        var variant = app.RequestedThemeVariant;
        Assert.Multiple(() =>
        {
            Assert.That(((ISolidColorBrush)app.FindResource(variant, "PageBrush")!).Color, Is.EqualTo(Color.Parse(page)));
            Assert.That(((ISolidColorBrush)app.FindResource(variant, "TextBrush")!).Color, Is.EqualTo(Color.Parse(text)));
            Assert.That(((ISolidColorBrush)app.FindResource(variant, "TextControlForeground")!).Color, Is.EqualTo(Color.Parse(text)));
            Assert.That(((ISolidColorBrush)app.FindResource(variant, "AccentButtonBackground")!).Color, Is.EqualTo(Color.Parse(accent)));
            Assert.That(
                ((ISolidColorBrush)app.FindResource(variant, "AccentButtonBackgroundPointerOver")!).Color,
                Is.EqualTo(app.FindResource(variant, "SystemAccentColorLight1"))
            );
            Assert.That(
                ((ISolidColorBrush)app.FindResource(variant, "AccentButtonBackgroundPressed")!).Color,
                Is.EqualTo(app.FindResource(variant, "SystemAccentColorDark1"))
            );
        });
    }

    [TestCase("latte", "#eff1f5", "#4c4f69", "#8839ef", "#823be2", "#7c3dd4")]
    [TestCase("frappe", "#303446", "#c6d0f5", "#ca9ee6", "#caa3e8", "#c9a8e9")]
    [TestCase("macchiato", "#24273a", "#cad3f5", "#c6a0f6", "#c6a5f6", "#c7aaf6")]
    [TestCase("mocha", "#1e1e2e", "#cdd6f4", "#cba6f7", "#cbabf7", "#cbb0f6")]
    public void CatppuccinUsesItsColoursAndLegibleAccentStates(
        string paletteId,
        string page,
        string text,
        string accent,
        string hover,
        string pressed
    )
    {
        var app = (App)Application.Current!;
        app.ApplyAppearance(new AppearancePreferences(ColourScheme.Catppuccin, paletteId));
        var variant = app.RequestedThemeVariant;
        Color Brush(string key) => ((ISolidColorBrush)app.FindResource(variant, key)!).Color;
        Assert.Multiple(() =>
        {
            Assert.That(Brush("PageBrush"), Is.EqualTo(Color.Parse(page)));
            Assert.That(Brush("TextBrush"), Is.EqualTo(Color.Parse(text)));
            Assert.That(Brush("TextControlForeground"), Is.EqualTo(Color.Parse(text)));
            Assert.That(Brush("AccentButtonBackground"), Is.EqualTo(Color.Parse(accent)));
            Assert.That(Brush("AccentButtonBackgroundPointerOver"), Is.EqualTo(Color.Parse(hover)));
            Assert.That(Brush("AccentButtonBackgroundPressed"), Is.EqualTo(Color.Parse(pressed)));
            Assert.That(Brush("SelectionBrush").A, Is.EqualTo(64));
            foreach (
                var foreground in new[]
                {
                    "AccentButtonForeground",
                    "AccentButtonForegroundPointerOver",
                    "AccentButtonForegroundPressed",
                    "CalendarViewTodayForeground",
                    "ComboBoxItemForegroundSelected",
                    "ComboBoxItemForegroundSelectedPointerOver",
                    "ComboBoxItemForegroundSelectedPressed",
                }
            )
                Assert.That(Brush(foreground), Is.EqualTo(Color.Parse(page)), foreground);
            foreach (var background in new[] { "PageBrush", "SurfaceBrush" })
            {
                Assert.That(Contrast(Brush("TextBrush"), Brush(background)), Is.GreaterThanOrEqualTo(4.5), background);
                Assert.That(Contrast(Brush("MutedTextBrush"), Brush(background)), Is.GreaterThanOrEqualTo(4.5), background);
            }
            Assert.That(Contrast(Brush("TextBrush"), Brush("NoticeBrush")), Is.GreaterThanOrEqualTo(4.5), "notice text");
            foreach (var background in new[] { "PrimaryBrush", "PrimaryHoverBrush", "PrimaryPressedBrush" })
                Assert.That(Contrast(Brush("PrimaryTextBrush"), Brush(background)), Is.GreaterThanOrEqualTo(4.5), background);
        });
    }

    [Test]
    public void ExistingControlsAndPopupsSwitchAcrossAllPalettes()
    {
        var app = (App)Application.Current!;
        var input = new TextBox { Text = "A quiet thought" };
        var picker = new ComboBox { ItemsSource = new[] { "JSON", "Markdown" }, SelectedIndex = 0 };
        var calendar = new CalendarDatePicker { SelectedDate = new DateTime(2026, 11, 3) };
        var button = new Button { Content = "Save", Classes = { "primary" } };
        var window = new Window
        {
            Title = "Appearance resource check",
            Width = 420,
            Height = 420,
            Content = new StackPanel
            {
                Margin = new Thickness(28),
                Spacing = 18,
                Children =
                {
                    new TextBlock { Text = "A thought, in every palette", FontSize = 24 },
                    input,
                    picker,
                    calendar,
                    button,
                },
            },
        };
        try
        {
            window.Show();
            foreach (var theme in ColourThemes.All.Reverse())
            {
                foreach (var palette in theme.Palettes.Reverse())
                {
                    app.ApplyAppearance(new AppearancePreferences(theme.Id, palette.Id));
                    Dispatcher.UIThread.RunJobs();
                    var variant = theme.GetVariant(palette.Id);
                    Assert.Multiple(() =>
                    {
                        Assert.That(window.ActualThemeVariant, Is.EqualTo(variant));
                        Assert.That(window.Background, Is.SameAs(app.FindResource(variant, "PageBrush")));
                        Assert.That(input.Foreground, Is.SameAs(app.FindResource(variant, "TextControlForeground")));
                        Assert.That(picker.Background, Is.SameAs(app.FindResource(variant, "ComboBoxBackground")));
                        Assert.That(app.FindResource(variant, "ControlCornerRadius"), Is.EqualTo(new CornerRadius(8)));
                        Assert.That(app.FindResource(variant, "OverlayCornerRadius"), Is.EqualTo(new CornerRadius(12)));
                    });

                    picker.IsDropDownOpen = true;
                    Dispatcher.UIThread.RunJobs();
                    var item = picker.ContainerFromIndex(0)!;
                    Assert.That(item.ActualThemeVariant, Is.EqualTo(variant));
                    if (theme.Id == ColourScheme.Catppuccin)
                    {
                        var presenter = item.GetVisualDescendants()
                            .OfType<ContentPresenter>()
                            .Single(control => control.Name == "PART_ContentPresenter");
                        Assert.That(
                            ((ISolidColorBrush)presenter.Foreground!).Color,
                            Is.EqualTo(((ISolidColorBrush)app.FindResource(variant, "PageBrush")!).Color)
                        );
                        using var popupFrame = TopLevel.GetTopLevel(item)!.CaptureRenderedFrame();
                        var popupScreenshot = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"Catppuccin-{palette.Id}-dropdown.png");
                        popupFrame!.Save(popupScreenshot, PngBitmapEncoderOptions.Default);
                        TestContext.AddTestAttachment(popupScreenshot);
                    }
                    picker.IsDropDownOpen = false;
                    calendar.IsDropDownOpen = true;
                    Dispatcher.UIThread.RunJobs();
                    calendar.IsDropDownOpen = false;
                    Dispatcher.UIThread.RunJobs();
                    using var frame = window.CaptureRenderedFrame();
                    Assert.That(frame, Is.Not.Null);
                    var screenshot = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{theme.Id}-{palette.Id}.png");
                    frame!.Save(screenshot, PngBitmapEncoderOptions.Default);
                    TestContext.AddTestAttachment(screenshot);
                    if (theme.Id == ColourScheme.Catppuccin)
                    {
                        var point = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
                        window.MouseMove(point);
                        foreach (var isPressed in new[] { false, true })
                        {
                            if (isPressed)
                                window.MouseDown(point, MouseButton.Left);
                            Dispatcher.UIThread.RunJobs();
                            var presenter = button
                                .GetVisualDescendants()
                                .OfType<ContentPresenter>()
                                .Single(control => control.Name == "PART_ContentPresenter");
                            Assert.Multiple(() =>
                            {
                                Assert.That(button.IsPointerOver, Is.True);
                                Assert.That(button.IsPressed, Is.EqualTo(isPressed));
                                Assert.That(
                                    ((ISolidColorBrush)presenter.Background!).Color,
                                    Is.EqualTo(
                                        ((ISolidColorBrush)app.FindResource(variant, isPressed ? "PrimaryPressedBrush" : "PrimaryHoverBrush")!).Color
                                    )
                                );
                            });
                            using var stateFrame = window.CaptureRenderedFrame();
                            var stateScreenshot = Path.Combine(
                                TestContext.CurrentContext.WorkDirectory,
                                $"Catppuccin-{palette.Id}-{(isPressed ? "pressed" : "hover")}.png"
                            );
                            stateFrame!.Save(stateScreenshot, PngBitmapEncoderOptions.Default);
                            TestContext.AddTestAttachment(stateScreenshot);
                        }
                        window.MouseUp(point, MouseButton.Left);
                        window.MouseMove(new Point(1, 1));
                        var dialog = new AppearanceDialog(new AppearancePreferences(theme.Id, palette.Id), _ => null, null);
                        try
                        {
                            dialog.Show(window);
                            using var dialogFrame = dialog.CaptureRenderedFrame();
                            var dialogScreenshot = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"Catppuccin-{palette.Id}-appearance.png");
                            dialogFrame!.Save(dialogScreenshot, PngBitmapEncoderOptions.Default);
                            TestContext.AddTestAttachment(dialogScreenshot);
                        }
                        finally
                        {
                            dialog.Close();
                        }
                    }
                }
            }
            Assert.That(app.Styles.OfType<FluentTheme>().Count(), Is.EqualTo(1));
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public void SeveralNamedPalettesWithTheSameFallbackHaveIndependentFluentBrushes()
    {
        var app = (App)Application.Current!;
        var variants = new List<ThemeVariant>();
        try
        {
            foreach (var name in new[] { "Dawn", "Mist", "Dusk" })
            {
                var variant = new ThemeVariant($"Fixture/{name}", ThemeVariant.Light);
                variants.Add(variant);
                var colour = name == "Mist" ? Colors.Green : Colors.Blue;
                var resources = new PaletteResources
                {
                    FluentColours = new ColorPaletteResources { BaseHigh = colour, Accent = colour },
                };
                resources.Initialise(ThemeVariant.Light);
                app.Resources.ThemeDictionaries.Add(variant, resources);
            }
            foreach (var index in new[] { 0, 1, 2, 1, 0 })
            {
                app.RequestedThemeVariant = variants[index];
                var brush = (ISolidColorBrush)app.FindResource(variants[index], "TextControlForeground")!;
                Assert.That(brush.Color, Is.EqualTo(index == 1 ? Colors.Green : Colors.Blue));
                Assert.That(((ISolidColorBrush)app.FindResource(variants[index], "AccentButtonBackground")!).Color, Is.EqualTo(brush.Color));
            }
        }
        finally
        {
            app.ApplyAppearance(new AppearancePreferences());
            foreach (var variant in variants)
                app.Resources.ThemeDictionaries.Remove(variant);
        }
    }

    [Test]
    public void SystemChangesOnlyAffectMatchSystemSelections()
    {
        var app = (App)Application.Current!;
        var theme = ColourThemes.Get(ColourScheme.Hearth);
        app.ApplyAppearance(new AppearancePreferences(ColourScheme.Hearth));
        app.UpdateSystemAppearance(PlatformThemeVariant.Dark);
        Assert.That(app.RequestedThemeVariant, Is.EqualTo(theme.GetVariant("dark")));
        app.UpdateSystemAppearance(PlatformThemeVariant.Light);
        Assert.That(app.RequestedThemeVariant, Is.EqualTo(theme.GetVariant("light")));
        app.ApplyAppearance(new AppearancePreferences(ColourScheme.Hearth, "dark"));
        app.UpdateSystemAppearance(PlatformThemeVariant.Light);
        Assert.That(app.RequestedThemeVariant, Is.EqualTo(theme.GetVariant("dark")));
    }

    [TestCase(null)]
    [TestCase("frappe")]
    [TestCase("macchiato")]
    public void CatppuccinMatchesSystemOrKeepsAnExplicitPalette(string? paletteId)
    {
        var app = (App)Application.Current!;
        var theme = ColourThemes.Get(ColourScheme.Catppuccin);
        app.ApplyAppearance(new AppearancePreferences(ColourScheme.Catppuccin, paletteId));
        foreach (var system in new[] { PlatformThemeVariant.Light, PlatformThemeVariant.Dark, PlatformThemeVariant.Light })
        {
            app.UpdateSystemAppearance(system);
            Assert.That(
                app.RequestedThemeVariant,
                Is.EqualTo(theme.GetVariant(paletteId ?? (system == PlatformThemeVariant.Light ? "latte" : "mocha")))
            );
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DialogKeepsEditsLocalUntilSaveSucceeds(bool failSave)
    {
        var app = (App)Application.Current!;
        var before = app.RequestedThemeVariant;
        AppearancePreferences? saved = null;
        var dialog = new AppearanceDialog(
            new AppearancePreferences(),
            preferences =>
            {
                if (failSave)
                    return "Couldn't save settings. Try again.";
                saved = preferences;
                app.ApplyAppearance(preferences);
                return null;
            },
            null
        );
        try
        {
            dialog.Show();
            var scheme = dialog.FindControl<ComboBox>("SchemePicker")!;
            var palette = dialog.FindControl<ComboBox>("ModePicker")!;
            scheme.SelectedItem = ColourThemes.Get(ColourScheme.Hearth);
            palette.SelectedIndex = 2;
            Assert.That(app.RequestedThemeVariant, Is.EqualTo(before));
            if (!failSave)
            {
                using var frame = dialog.CaptureRenderedFrame();
                var screenshot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Appearance-dialog.png");
                frame!.Save(screenshot, PngBitmapEncoderOptions.Default);
                TestContext.AddTestAttachment(screenshot);
            }
            var save = dialog.GetVisualDescendants().OfType<Button>().Single(button => Equals(button.Content, "Save"));
            save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Multiple(() =>
            {
                Assert.That(saved, Is.EqualTo(failSave ? null : new AppearancePreferences(ColourScheme.Hearth, "dark")));
                Assert.That(dialog.IsVisible, Is.EqualTo(failSave));
                Assert.That(app.RequestedThemeVariant, Is.EqualTo(failSave ? before : ColourThemes.Get(ColourScheme.Hearth).GetVariant("dark")));
            });
        }
        finally
        {
            dialog.Close();
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CancellingOrClosingDiscardsAppearanceEdits(bool closeWindow)
    {
        var app = (App)Application.Current!;
        var before = app.RequestedThemeVariant;
        var saves = 0;
        var dialog = new AppearanceDialog(
            new AppearancePreferences(),
            _ =>
            {
                saves++;
                return null;
            },
            null
        );
        try
        {
            dialog.Show();
            dialog.FindControl<ComboBox>("SchemePicker")!.SelectedItem = ColourThemes.Get(ColourScheme.Hearth);
            dialog.FindControl<ComboBox>("ModePicker")!.SelectedIndex = 2;
            if (closeWindow)
                dialog.Close();
            else
                dialog
                    .GetVisualDescendants()
                    .OfType<Button>()
                    .Single(button => Equals(button.Content, "Cancel"))
                    .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Multiple(() =>
            {
                Assert.That(saves, Is.Zero);
                Assert.That(app.RequestedThemeVariant, Is.EqualTo(before));
                Assert.That(dialog.IsVisible, Is.False);
            });
        }
        finally
        {
            dialog.Close();
        }
    }

    [Test]
    public void DialogListsEveryPaletteAndResetsToMatchSystemWhenThemeChanges()
    {
        var existing = ColourThemes.Get(ColourScheme.Hearth);
        var theme = new ColourTheme(
            ColourScheme.Forest,
            "Fixture",
            [
                new ColourPalette("dawn", "Dawn", ThemeVariant.Light, existing.Palettes[0].Source),
                new ColourPalette("mist", "Mist", ThemeVariant.Light, existing.Palettes[0].Source),
                new ColourPalette("dusk", "Dusk", ThemeVariant.Dark, existing.Palettes[1].Source),
            ],
            "dawn",
            "dusk"
        );
        var dialog = new AppearanceDialog(new AppearancePreferences(ColourScheme.Hearth, "dark"), _ => null, null);
        try
        {
            var scheme = dialog.FindControl<ComboBox>("SchemePicker")!;
            var palette = dialog.FindControl<ComboBox>("ModePicker")!;
            scheme.ItemsSource = new[] { theme, existing };
            scheme.SelectedItem = theme;
            Assert.That(palette.Items.Select(item => item!.ToString()), Is.EqualTo(new[] { "Match System", "Dawn", "Mist", "Dusk" }));
            Assert.That(palette.SelectedIndex, Is.Zero);
            palette.SelectedIndex = 2;
            scheme.SelectedItem = existing;
            Assert.That(palette.SelectedIndex, Is.Zero);
        }
        finally
        {
            dialog.Close();
        }
    }

    [Test]
    public void DesignerDefaultsToForestAndListsTheFourCatppuccinPalettes()
    {
        var dialog = new AppearanceDialog();
        try
        {
            var scheme = dialog.FindControl<ComboBox>("SchemePicker")!;
            var palette = dialog.FindControl<ComboBox>("ModePicker")!;
            Assert.That(((ColourTheme)scheme.SelectedItem!).Id, Is.EqualTo(ColourScheme.Forest));
            Assert.That(palette.SelectedItem!.ToString(), Is.EqualTo("Match System"));
            scheme.SelectedItem = ColourThemes.Get(ColourScheme.Catppuccin);
            Assert.That(
                palette.Items.Select(item => item!.ToString()),
                Is.EqualTo(new[] { "Match System", "Latte", "Frappé", "Macchiato", "Mocha" })
            );
        }
        finally
        {
            dialog.Close();
        }
    }

    private static double Contrast(Color first, Color second)
    {
        static double Linear(byte channel)
        {
            var value = channel / 255.0;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }
        static double Luminance(Color colour) => 0.2126 * Linear(colour.R) + 0.7152 * Linear(colour.G) + 0.0722 * Linear(colour.B);
        var a = Luminance(first);
        var b = Luminance(second);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }
}
