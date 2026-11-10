using System.Runtime.InteropServices;

namespace GazeMagnet;

internal sealed class MouseActivity : IDisposable
{
    private const int LowLevelMouse = 14;
    private const int LeftButtonDown = 0x0201;
    private const int RightButtonDown = 0x0204;
    private const int MiddleButtonDown = 0x0207;
    private const int ExtraButtonDown = 0x020B;

    private readonly HookProc callback;
    private nint hook;

    public MouseActivity() => callback = OnInput;

    public long LastButtonPressAt { get; private set; } = long.MinValue;

    public bool Start()
    {
        if (hook == 0)
            hook = SetWindowsHookEx(LowLevelMouse, callback, GetModuleHandle(null), 0);
        return hook != 0;
    }

    private nint OnInput(int code, nint message, nint data)
    {
        if (code == 0 && (int)message is LeftButtonDown or RightButtonDown or MiddleButtonDown or ExtraButtonDown)
        {
            var input = Marshal.PtrToStructure<MouseEvent>(data);
            if (input.ExtraInfo != Native.GazeClickMarker)
                LastButtonPressAt = Environment.TickCount64;
        }
        return CallNextHookEx(0, code, message, data);
    }

    public void Dispose()
    {
        if (hook == 0)
            return;
        UnhookWindowsHookEx(hook);
        hook = 0;
    }

    private delegate nint HookProc(int code, nint message, nint data);

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseEvent
    {
        public Point Position;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public nuint ExtraInfo;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string? name);

    [DllImport("user32.dll", EntryPoint = "SetWindowsHookExW")]
    private static extern nint SetWindowsHookEx(int id, HookProc procedure, nint module, uint threadId);

    [DllImport("user32.dll")]
    private static extern int UnhookWindowsHookEx(nint hook);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hook, int code, nint message, nint data);
}
