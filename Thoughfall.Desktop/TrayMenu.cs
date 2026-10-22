namespace Thoughfall.Desktop;

using Avalonia.Controls;
using Avalonia.Platform;

internal sealed class TrayMenu : IDisposable
{
    private readonly TrayIcon _icon;

    internal TrayMenu(Avalonia.Application app)
    {
        var menu = new NativeMenu();
        AddItem(menu, "Capture a thought", (_, _) => CaptureRequested?.Invoke(this, EventArgs.Empty));
        AddItem(menu, "Export entries…", (_, _) => ExportRequested?.Invoke(this, EventArgs.Empty));
        AddItem(menu, "Open data folder", (_, _) => OpenFolderRequested?.Invoke(this, EventArgs.Empty));
        menu.Add(new NativeMenuItemSeparator());
        AddItem(menu, "Quit", (_, _) => QuitRequested?.Invoke(this, EventArgs.Empty));

        using var icon = AssetLoader.Open(new Uri("avares://Thoughfall.Desktop/Assets/Thoughfall.ico"));
        _icon = new TrayIcon
        {
            Icon = new WindowIcon(icon),
            ToolTipText = "Thoughfall",
            Menu = menu,
            IsVisible = true,
        };
        _icon.Clicked += (_, _) => CaptureRequested?.Invoke(this, EventArgs.Empty);
        TrayIcon.SetIcons(app, [_icon]);
    }

    internal event EventHandler? CaptureRequested;

    internal event EventHandler? ExportRequested;

    internal event EventHandler? OpenFolderRequested;

    internal event EventHandler? QuitRequested;

    internal bool CanHideCapture => _icon.NativeMenuExporter is not null && !OperatingSystem.IsLinux();

    public void Dispose() => _icon.Dispose();

    private static void AddItem(NativeMenu menu, string header, EventHandler handler)
    {
        var item = new NativeMenuItem(header);
        item.Click += handler;
        menu.Add(item);
    }
}
