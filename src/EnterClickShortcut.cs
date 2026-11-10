using System.Runtime.InteropServices;

namespace GazeMagnet;

internal sealed class EnterClickShortcut : IDisposable
{
    private const int LowLevelKeyboard = 13;
    private const int KeyDown = 0x0100;
    private const int KeyUp = 0x0101;
    private const int SystemKeyDown = 0x0104;
    private const int SystemKeyUp = 0x0105;
    private const int ExtendedKey = 1;
    private const int FlagsOffset = 8;

    private readonly Func<bool> tryClick;
    private readonly HookProc callback;
    private readonly KeyState[] keys = new KeyState[2];
    private nint hook;

    private enum KeyState { Up, PassedThrough, Consumed }

    public EnterClickShortcut(Func<bool> tryClick)
    {
        this.tryClick = tryClick;
        callback = OnInput;
    }

    public bool Start()
    {
        if (hook == 0)
            hook = SetWindowsHookEx(LowLevelKeyboard, callback, GetModuleHandle(null), 0);
        return hook != 0;
    }

    private nint OnInput(int code, nint message, nint data)
    {
        if (code == 0 && Marshal.ReadInt32(data) == (int)Keys.Enter)
        {
            // O Enter numérico é uma tecla estendida e tem seu próprio estado.
            ref KeyState state = ref keys[Marshal.ReadInt32(data, FlagsOffset) & ExtendedKey];
            if ((int)message is KeyUp or SystemKeyUp)
            {
                bool consumed = state == KeyState.Consumed;
                state = KeyState.Up;
                if (consumed)
                    return 1;
            }
            else if ((int)message is KeyDown or SystemKeyDown)
            {
                if (state == KeyState.Up)
                    state = tryClick() ? KeyState.Consumed : KeyState.PassedThrough;
                if (state == KeyState.Consumed)
                    return 1;
            }
        }
        return CallNextHookEx(0, code, message, data);
    }

    public void Dispose()
    {
        if (hook == 0)
            return;
        UnhookWindowsHookEx(hook);
        hook = 0;
        Array.Clear(keys);
    }

    private delegate nint HookProc(int code, nint message, nint data);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string? name);

    [DllImport("user32.dll", EntryPoint = "SetWindowsHookExW")]
    private static extern nint SetWindowsHookEx(int id, HookProc procedure, nint module, uint threadId);

    [DllImport("user32.dll")]
    private static extern int UnhookWindowsHookEx(nint hook);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hook, int code, nint message, nint data);
}
