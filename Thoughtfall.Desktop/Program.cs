namespace Thoughtfall.Desktop;

using Avalonia;
using Thoughtfall.Application;

/// <summary>Starts the desktop application or activates its running instance.</summary>
internal static class Program
{
    /// <summary>Creates the application builder used by Avalonia's designer, without desktop services.</summary>
    internal static AppBuilder BuildAvaloniaApp() => BuildAvaloniaApp(() => new App());

    private static AppBuilder BuildAvaloniaApp(Func<App> createApp) =>
        AppBuilder.Configure(createApp).UsePlatformDetect().WithInterFont().LogToTrace();

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            // Inno Setup checks this marker before installing or uninstalling.
            // It never owns the mutex or closes the app, so drafts can be saved normally.
            using var installerMarker = OperatingSystem.IsWindows() ? new Mutex(false, @"Local\Thoughtfall.Running") : null;
#if DEBUG
            var dataFolder = AppContext.BaseDirectory;
#else
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(home))
            {
                throw new IOException("The user's home directory is unavailable.");
            }

            var dataFolder = Path.Combine(home, ".thoughtfall");
#endif
            var store = new DiaryStore(dataFolder);
            using var instance = DesktopInstance.Open(store.Folder);
            if (!instance.IsOwner)
            {
                instance.RequestCaptureAsync().GetAwaiter().GetResult();
                return 0;
            }

            return BuildAvaloniaApp(() => new App(store, instance)).StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Thoughtfall couldn't start: {ex.Message}");
            ErrorReporting.Unexpected("starting Thoughtfall", ex);
            return 1;
        }
    }
}
