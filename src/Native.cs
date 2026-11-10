using System.Runtime.InteropServices;

namespace GazeMagnet;

internal static class Native
{
    public const int HotKeyMessage = 0x0312;
    public const int EmergencyId = 1;
    public const int ToggleGazeClickId = 2;
    public const nuint GazeClickMarker = 0x474D434B;
    private const uint LeftDown = 0x0002;
    private const uint LeftUp = 0x0004;
    private const uint CtrlAltNoRepeat = 0x4003;
    public const int NoActivate = 0x08000000;
    public const int Transparent = 0x20;
    public const int ToolWindow = 0x80;
    private const uint KeepSize = 0x0001;
    private const uint KeepPosition = 0x0002;
    private const uint KeepZOrder = 0x0004;
    private const uint KeepFocus = 0x0010;
    private const uint ShowWindow = 0x0040;
    private const uint HideWindow = 0x0080;

    [DllImport("user32.dll")]
    private static extern int RegisterHotKey(nint window, int id, uint modifiers, uint key);

    [DllImport("user32.dll")]
    private static extern int UnregisterHotKey(nint window, int id);

    [DllImport("user32.dll")]
    private static extern nint WindowFromPoint(Point point);

    [DllImport("user32.dll")]
    private static extern nint GetAncestor(nint window, uint flags);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int key);

    [DllImport("user32.dll")]
    public static extern int SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern int SetWindowPos(nint window, nint insertAfter, int x, int y,
        int width, int height, uint flags);

    [DllImport("user32.dll")]
    public static extern int IsWindowVisible(nint window);

    public static void ShowOutline(nint window, Rectangle bounds) =>
        SetWindowPos(window, -1, bounds.X, bounds.Y, bounds.Width, bounds.Height, KeepFocus | ShowWindow);

    public static void HideOutline(nint window) =>
        SetWindowPos(window, 0, 0, 0, 0, 0, KeepSize | KeepPosition | KeepZOrder | KeepFocus | HideWindow);

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public nuint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public MouseInput Mouse;
    }

    [DllImport("user32.dll")]
    private static extern uint SendInput(uint count, Input[] inputs, int inputSize);

    public static bool SendLeftClick()
    {
        var press = new Input { Mouse = new MouseInput { Flags = LeftDown, ExtraInfo = GazeClickMarker } };
        var release = new Input { Mouse = new MouseInput { Flags = LeftUp, ExtraInfo = GazeClickMarker } };
        int size = Marshal.SizeOf<Input>();
        uint sent = SendInput(2, [press, release], size);
        if (sent == 1)
            SendInput(1, [release], size);
        return sent == 2;
    }

    public static bool RegisterShortcut(nint window, int id, Keys key) =>
        RegisterHotKey(window, id, CtrlAltNoRepeat, (uint)key) != 0;

    public static void UnregisterShortcut(nint window, int id) => UnregisterHotKey(window, id);

    public static bool ModifierDown() =>
        Down(Keys.ControlKey) || Down(Keys.ShiftKey) || Down(Keys.Menu) ||
        Down(Keys.LWin) || Down(Keys.RWin);

    public static nint RootAt(Point point) => GetAncestor(WindowFromPoint(point), 2);

    public static bool MouseButtonDown() =>
        Down(Keys.LButton) || Down(Keys.RButton) || Down(Keys.MButton) ||
        Down(Keys.XButton1) || Down(Keys.XButton2);

    private static bool Down(Keys key) => GetAsyncKeyState((int)key) < 0;
}
