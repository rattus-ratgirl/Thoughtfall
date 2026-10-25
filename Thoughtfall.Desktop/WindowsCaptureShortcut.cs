namespace Thoughtfall.Desktop;

using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Threading;

/// <summary>Owns one Windows hotkey on the capture window's UI thread.</summary>
internal sealed class WindowsCaptureShortcut : IDisposable
{
    private readonly Window _window;
    private readonly Action _activate;
    private readonly nint _handle;
    private int _nextId;
    private int? _registeredId;

    internal WindowsCaptureShortcut(Window window, Action activate)
    {
        Dispatcher.UIThread.VerifyAccess();

        _window = window;
        _activate = activate;
        _handle = window.TryGetPlatformHandle()?.Handle ?? 0;

        Win32Properties.AddWndProcHookCallback(window, OnWindowMessage);
    }

    internal string? Register(CaptureShortcut? shortcut)
    {
        Dispatcher.UIThread.VerifyAccess();
        var error = Suspend();

        if (error is not null || shortcut is null)
            return error;

        if (_handle == 0 || !shortcut.IsValid)
            return "The capture shortcut could not be registered on this window.";

        uint modifiers = 0x4000; // MOD_NOREPEAT suppresses held-key notifications.

        if (shortcut.Control)
            modifiers |= 0x0002;

        if (shortcut.Alt)
            modifiers |= 0x0001;

        if (shortcut.Shift)
            modifiers |= 0x0004;

        // Distinct IDs prevent a queued WM_HOTKEY from an old registration activating a new one.
        _nextId = _nextId == 0xBFFF ? 1 : _nextId + 1;

        if (!RegisterHotKey(_handle, _nextId, modifiers, shortcut.VirtualKey))
        {
            var code = Marshal.GetLastWin32Error();
            return $"Couldn't register {shortcut.DisplayText}. It may already be in use. Choose another shortcut. (Windows error {code})";
        }

        _registeredId = _nextId;
        return null;
    }

    internal string? Suspend()
    {
        Dispatcher.UIThread.VerifyAccess();

        if (_registeredId is not { } id)
            return null;

        if (!UnregisterHotKey(_handle, id))
            return $"Couldn't release the capture shortcut. (Windows error {Marshal.GetLastWin32Error()})";

        _registeredId = null;
        return null;
    }

    private nint OnWindowMessage(nint handle, uint message, nint wParam, nint lParam, ref bool handled)
    {
        const uint wmHotKey = 0x0312;

        if (message != wmHotKey || _registeredId is not { } id || wParam != id)
            return 0;

        handled = true;
        _activate();
        return 0;
    }

    public void Dispose()
    {
        Dispatcher.UIThread.VerifyAccess();
        var error = Suspend();

        if (error is not null)
            ErrorReporting.Unexpected("releasing the capture shortcut", new IOException(error));

        _registeredId = null;
        Win32Properties.RemoveWndProcHookCallback(_window, OnWindowMessage);
        // Closing the owning window on final shutdown also releases any remaining HWND registration.
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(nint window, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(nint window, int id);
}
