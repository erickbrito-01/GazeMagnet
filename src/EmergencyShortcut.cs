namespace GazeMagnet;

internal readonly record struct EmergencyShortcut(Keys Key, bool Ctrl, bool Alt, bool Shift)
{
    public static EmergencyShortcut Default => new(Keys.F10, true, true, false);

    public bool IsAllowed() =>
        (Key is >= Keys.F1 and <= Keys.F11 && (Ctrl || Alt) &&
            !(Key == Keys.F11 && Ctrl && Alt && !Shift)) ||
        ((Key is >= Keys.A and <= Keys.Z or >= Keys.D0 and <= Keys.D9) && Shift && Ctrl != Alt);

    public uint WindowsModifiers() => (Alt ? 1u : 0u) | (Ctrl ? 2u : 0u) | (Shift ? 4u : 0u);

    public override string ToString() =>
        (Ctrl ? "Ctrl + " : "") + (Alt ? "Alt + " : "") + (Shift ? "Shift + " : "") + KeyLabel(Key);

    public static string KeyLabel(Keys key) => key is >= Keys.D0 and <= Keys.D9
        ? ((int)(key - Keys.D0)).ToString()
        : key.ToString();
}
