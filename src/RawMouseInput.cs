using System.Runtime.InteropServices;

namespace GazeMagnet;

internal static class RawMouseInput
{
    public const int Message = 0x00FF;
    private const uint Input = 0x10000003;
    private const uint InputSink = 0x00000100;
    private const uint Remove = 0x00000001;
    private static readonly uint HeaderSize = (uint)Marshal.SizeOf<Header>();

    public static bool Register(nint window) => SetRegistration(InputSink, window);

    public static void Unregister() => SetRegistration(Remove, 0);

    public static bool IsMovement(nint handle)
    {
        uint size = (uint)Marshal.SizeOf<Packet>();
        return GetRawInputData(handle, Input, out Packet packet, ref size, HeaderSize) == size &&
            size == (uint)Marshal.SizeOf<Packet>() && packet.Header.Type == 0 &&
            (packet.Mouse.X != 0 || packet.Mouse.Y != 0);
    }

    private static bool SetRegistration(uint flags, nint window) =>
        RegisterRawInputDevices([new Device { UsagePage = 1, Usage = 2, Flags = flags, Window = window }],
            1, (uint)Marshal.SizeOf<Device>());

    [StructLayout(LayoutKind.Sequential)]
    private struct Device
    {
        public ushort UsagePage;
        public ushort Usage;
        public uint Flags;
        public nint Window;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Header
    {
        public uint Type;
        public uint Size;
        public nint Device;
        public nint Parameter;
    }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    private struct Mouse
    {
        [FieldOffset(12)] public int X;
        [FieldOffset(16)] public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Packet
    {
        public Header Header;
        public Mouse Mouse;
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterRawInputDevices(Device[] devices, uint count, uint size);

    [DllImport("user32.dll")]
    private static extern uint GetRawInputData(nint handle, uint command, out Packet data, ref uint size, uint headerSize);
}
