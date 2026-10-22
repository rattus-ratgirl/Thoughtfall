namespace Thoughfall.Desktop;

using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Thoughfall.Application;

internal partial class App : Avalonia.Application
{
    private readonly DiaryStore _store;
    private readonly DesktopInstance _instance;

    internal App(DiaryStore store, DesktopInstance instance)
    {
        _store = store;
        _instance = instance;
    }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

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
