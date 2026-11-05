namespace Thoughtfall.Desktop;

using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Thoughtfall.Application;
using Thoughtfall.Desktop.Appearance;
using Thoughtfall.Desktop.Settings;
using Thoughtfall.Desktop.Shell;

/// <summary>Connects the diary and instance owner to Avalonia's desktop lifetime.</summary>
internal partial class App : Avalonia.Application
{
    private readonly DiaryStore? _store;
    private readonly DesktopInstance? _instance;
    private readonly DesktopSettings? _settings;
    private readonly bool _captureRequested;
    private readonly string? _appearanceError;
    private AppearancePreferences _appearance = new();
    private PlatformThemeVariant _systemAppearance = PlatformThemeVariant.Light;

    /// <summary>Initializes the designer's application with default appearance and no desktop services.</summary>
    public App() { }

    /// <summary>Initializes a new instance of the <see cref="App"/> class.</summary>
    /// <param name="store">The shared diary store.</param>
    /// <param name="instance">The desktop instance owner.</param>
    /// <param name="captureRequested">Whether this launch explicitly requests capture.</param>
    internal App(DiaryStore store, DesktopInstance instance, bool captureRequested)
    {
        _store = store;
        _instance = instance;
        _captureRequested = captureRequested;
        _settings = new DesktopSettings(store.Folder);
        _appearance = _settings.LoadAppearance(out _appearanceError);
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        var paletteStyles = new Styles();
        foreach (var theme in ColourThemes.All)
        {
            foreach (var palette in theme.Palettes)
            {
                var resources = (PaletteResources)AvaloniaXamlLoader.Load(palette.Source);
                resources.Initialise(palette.BaseVariant);
                paletteStyles.Resources.ThemeDictionaries.Add(theme.GetVariant(palette.Id), resources);
            }
        }

        // Shared application styles retain precedence over Fluent's invariant resources.
        Styles.Insert(1, paletteStyles);
        ApplyAppearance(_appearance);
    }

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (PlatformSettings is { } platformSettings)
        {
            UpdateSystemAppearance(platformSettings.GetColorValues().ThemeVariant);
            platformSettings.ColorValuesChanged += OnSystemColoursChanged;
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                lifetime.Exit += (_, _) => platformSettings.ColorValuesChanged -= OnSystemColoursChanged;
        }

        if (
            ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && _store is not null
            && _instance is not null
            && _settings is not null
        )
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var controller = new ApplicationController(this, _store, desktop, _instance, _settings, _appearanceError);
            _instance.Listen(
                () => Dispatcher.UIThread.Post(controller.ActivateCapture),
                () => Dispatcher.UIThread.Post(controller.ShowActivationError)
            );
            desktop.Exit += (_, _) => controller.Dispose();
            controller.Start(_captureRequested);
        }

        base.OnFrameworkInitializationCompleted();
    }

    internal AppearancePreferences CurrentAppearance => _appearance;

    internal void ApplyAppearance(AppearancePreferences preferences)
    {
        _appearance = preferences;
        RequestedThemeVariant = ColourThemes.Get(preferences.ColourScheme).ResolveVariant(preferences.PaletteId, _systemAppearance);
    }

    internal void UpdateSystemAppearance(PlatformThemeVariant appearance)
    {
        _systemAppearance = appearance;
        ApplyAppearance(_appearance);
    }

    private void OnSystemColoursChanged(object? sender, PlatformColorValues values) =>
        Dispatcher.UIThread.Post(() => UpdateSystemAppearance(values.ThemeVariant));
}
