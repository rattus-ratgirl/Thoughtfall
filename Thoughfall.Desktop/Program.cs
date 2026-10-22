namespace Thoughfall.Desktop;

using Avalonia;
using Thoughfall.Application;

/// <summary>Starts the desktop application or activates its running instance.</summary>
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(home))
            {
                throw new IOException("The user's home directory is unavailable.");
            }

            var store = new DiaryStore(Path.Combine(home, ".thoughtfall"));
            using var instance = DesktopInstance.Open(store.Folder);
            if (!instance.IsOwner)
            {
                instance.RequestCaptureAsync().GetAwaiter().GetResult();
                return 0;
            }

            return AppBuilder
                .Configure(() => new App(store, instance))
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Thoughfall couldn't start: {ex.Message}");
            ErrorReporting.Unexpected("starting Thoughfall", ex);
            return 1;
        }
    }
}
