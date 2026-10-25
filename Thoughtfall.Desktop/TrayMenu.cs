namespace Thoughtfall.Desktop;

using Avalonia.Controls;
using Avalonia.Platform;

/// <summary>Owns the tray icon and exposes its menu actions.</summary>
internal sealed class TrayMenu : IDisposable
{
    private readonly TrayIcon _icon;

    /// <summary>Initializes a new instance of the <see cref="TrayMenu"/> class.</summary>
    /// <param name="app">The application hosting the tray icon.</param>
    internal TrayMenu(Avalonia.Application app)
    {
        var menu = new NativeMenu();
        AddItem(menu, "Capture a thought", (_, _) => CaptureRequested?.Invoke(this, EventArgs.Empty));
        if (OperatingSystem.IsWindows())
        {
            AddItem(menu, "Capture shortcut…", (_, _) => CaptureShortcutRequested?.Invoke(this, EventArgs.Empty));
        }

        AddItem(menu, "Export entries…", (_, _) => ExportRequested?.Invoke(this, EventArgs.Empty));
        AddItem(menu, "Appearance…", (_, _) => AppearanceRequested?.Invoke(this, EventArgs.Empty));
        AddItem(menu, "Open data folder", (_, _) => OpenFolderRequested?.Invoke(this, EventArgs.Empty));
        menu.Add(new NativeMenuItemSeparator());
        AddItem(menu, "Quit", (_, _) => QuitRequested?.Invoke(this, EventArgs.Empty));

        using var icon = AssetLoader.Open(new Uri("avares://Thoughtfall.Desktop/Assets/Thoughtfall.ico"));
        _icon = new TrayIcon
        {
            Icon = new WindowIcon(icon),
            ToolTipText = "Thoughtfall",
            Menu = menu,
            IsVisible = true,
        };
        _icon.Clicked += (_, _) => CaptureRequested?.Invoke(this, EventArgs.Empty);
        TrayIcon.SetIcons(app, [_icon]);
    }

    /// <summary>Occurs when capture is requested from the tray.</summary>
    internal event EventHandler? CaptureRequested;

    /// <summary>Occurs when the Windows capture shortcut dialog is requested.</summary>
    internal event EventHandler? CaptureShortcutRequested;

    /// <summary>Occurs when appearance settings are requested.</summary>
    internal event EventHandler? AppearanceRequested;

    /// <summary>Occurs when export is requested from the tray.</summary>
    internal event EventHandler? ExportRequested;

    /// <summary>Occurs when the data folder is requested from the tray.</summary>
    internal event EventHandler? OpenFolderRequested;

    /// <summary>Occurs when shutdown is requested from the tray.</summary>
    internal event EventHandler? QuitRequested;

    /// <summary>Gets a value indicating whether the tray provides a supported route back to hidden capture.</summary>
    internal bool CanHideCapture => _icon.NativeMenuExporter is not null && !OperatingSystem.IsLinux();

    /// <inheritdoc />
    public void Dispose() => _icon.Dispose();

    private static void AddItem(NativeMenu menu, string header, EventHandler handler)
    {
        var item = new NativeMenuItem(header);
        item.Click += handler;
        menu.Add(item);
    }
}
