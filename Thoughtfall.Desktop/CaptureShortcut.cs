namespace Thoughtfall.Desktop;

using Avalonia.Input;

/// <summary>A capture chord with stable key names and explicit modifiers.</summary>
internal sealed record CaptureShortcut(string KeyName, bool Control, bool Alt, bool Shift)
{
    internal static CaptureShortcut Default { get; } = new("J", true, true, false);

    internal string DisplayText => (Control ? "Ctrl + " : "") + (Alt ? "Alt + " : "") + (Shift ? "Shift + " : "") + KeyName;

    internal bool IsValid => (Control || Alt) && VirtualKey != 0;

    // Windows virtual-key codes for letters and top-row digits match ASCII, not Avalonia's Key enum.
    internal uint VirtualKey =>
        KeyName switch
        {
            [>= 'A' and <= 'Z'] or [>= '0' and <= '9'] => KeyName[0],
            "F1" => 0x70,
            "F2" => 0x71,
            "F3" => 0x72,
            "F4" => 0x73,
            "F5" => 0x74,
            "F6" => 0x75,
            "F7" => 0x76,
            "F8" => 0x77,
            "F9" => 0x78,
            "F10" => 0x79,
            "F11" => 0x7A,
            _ => 0,
        };

    internal static CaptureShortcut? FromKey(Key key, KeyModifiers modifiers)
    {
        var name = key switch
        {
            Key.A
            or Key.B
            or Key.C
            or Key.D
            or Key.E
            or Key.F
            or Key.G
            or Key.H
            or Key.I
            or Key.J
            or Key.K
            or Key.L
            or Key.M
            or Key.N
            or Key.O
            or Key.P
            or Key.Q
            or Key.R
            or Key.S
            or Key.T
            or Key.U
            or Key.V
            or Key.W
            or Key.X
            or Key.Y
            or Key.Z
            or Key.F1
            or Key.F2
            or Key.F3
            or Key.F4
            or Key.F5
            or Key.F6
            or Key.F7
            or Key.F8
            or Key.F9
            or Key.F10
            or Key.F11 => key.ToString(),
            Key.D0 => "0",
            Key.D1 => "1",
            Key.D2 => "2",
            Key.D3 => "3",
            Key.D4 => "4",
            Key.D5 => "5",
            Key.D6 => "6",
            Key.D7 => "7",
            Key.D8 => "8",
            Key.D9 => "9",
            _ => null,
        };

        if (name is null || (modifiers & ~(KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Shift)) != 0)
            return null;

        var shortcut = new CaptureShortcut(
            name,
            modifiers.HasFlag(KeyModifiers.Control),
            modifiers.HasFlag(KeyModifiers.Alt),
            modifiers.HasFlag(KeyModifiers.Shift)
        );

        return shortcut.IsValid ? shortcut : null;
    }
}
