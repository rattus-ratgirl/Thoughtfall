namespace Thoughtfall.Desktop;

using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using Thoughtfall.Application;

/// <summary>Connects the diary and instance owner to Avalonia's desktop lifetime.</summary>
internal partial class App : Avalonia.Application
{
    private readonly DiaryStore _store;
    private readonly DesktopInstance _instance;
    private readonly DesktopSettings _settings;
    private readonly AppearancePreferences _appearance;
    private readonly string? _appearanceError;

    /// <summary>Initializes a new instance of the <see cref="App"/> class.</summary>
    /// <param name="store">The shared diary store.</param>
    /// <param name="instance">The desktop instance owner.</param>
    internal App(DiaryStore store, DesktopInstance instance)
    {
        _store = store;
        _instance = instance;
        _settings = new DesktopSettings(store.Folder);
        _appearance = _settings.LoadAppearance(out _appearanceError);
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        var palette = _appearance.ColourScheme == ColourScheme.Hearth ? "Hearth" : "Thoughtfall";
        var baseUri = new Uri("avares://Thoughtfall.Desktop/");
        // Fluent's non-accent palette properties are read at startup. Never replace this at runtime.
        Styles.Insert(0, new StyleInclude(baseUri) { Source = new Uri(baseUri, $"Themes/Palettes/{palette}.axaml") });
        RequestedThemeVariant = _appearance.ThemeVariant;
    }

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var controller = new ApplicationController(this, _store, desktop, _instance, _settings, _appearance, _appearanceError);
            desktop.MainWindow = controller.Capture;
            _instance.Listen(
                () => Dispatcher.UIThread.Post(controller.ActivateCapture),
                () => Dispatcher.UIThread.Post(controller.ShowActivationError)
            );
            desktop.Exit += (_, _) => controller.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
