namespace Thoughfall.Desktop;

using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Thoughfall.Application;

/// <summary>Connects the diary and instance owner to Avalonia's desktop lifetime.</summary>
internal partial class App : Avalonia.Application
{
    private readonly DiaryStore _store;
    private readonly DesktopInstance _instance;

    /// <summary>Initializes a new instance of the <see cref="App"/> class.</summary>
    /// <param name="store">The shared diary store.</param>
    /// <param name="instance">The desktop instance owner.</param>
    internal App(DiaryStore store, DesktopInstance instance)
    {
        _store = store;
        _instance = instance;
    }

    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var controller = new ApplicationController(this, _store, desktop, _instance);
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
