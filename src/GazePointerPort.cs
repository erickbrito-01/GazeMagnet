using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GazeMagnet;

internal static class GazePointerPort
{
    public const int DefaultPort = 43333;
    public const string DemoKey = "AppKeyDemo";

    private const uint InsufficientBuffer = 122;
    private const int IPv4 = 2;
    private const int ListenersWithPid = 3;
    private const int RowSize = 24;

    public static int Find()
    {
        var owners = new HashSet<int>();
        foreach (Process process in Process.GetProcessesByName("GazePointer"))
        {
            using (process)
                owners.Add(process.Id);
        }

        if (owners.Count == 0)
            throw new InvalidDataException("Abra o GazePointer e clique em Conectar novamente.");

        byte[] table = ReadListeners();
        int count = BinaryPrimitives.ReadInt32LittleEndian(table);
        var ports = new HashSet<int>();
        for (int i = 0; i < count; i++)
        {
            var row = table.AsSpan(4 + i * RowSize, RowSize);
            if (!owners.Contains(BinaryPrimitives.ReadInt32LittleEndian(row[20..])))
                continue;

            uint address = BinaryPrimitives.ReadUInt32BigEndian(row[4..]);
            if (address is not (0 or 0x7f000001))
                continue;

            int port = BinaryPrimitives.ReadUInt16BigEndian(row[8..]);
            if (port != 0)
                ports.Add(port);
        }

        if (ports.Contains(DefaultPort))
            return DefaultPort;
        if (ports.Count == 1)
            return ports.First();

        throw new InvalidDataException(ports.Count == 0
            ? "O GazePointer ainda não abriu a porta local. Aguarde e tente novamente."
            : "O GazePointer está usando mais de uma porta. Desmarque a configuração automática e informe a porta.");
    }

    private static byte[] ReadListeners()
    {
        int size = 0;
        GetExtendedTcpTable(null, ref size, false, IPv4, ListenersWithPid, 0);
        for (int attempt = 0; attempt < 3 && size >= 4; attempt++)
        {
            var table = new byte[size];
            uint result = GetExtendedTcpTable(table, ref size, false, IPv4, ListenersWithPid, 0);
            if (result == InsufficientBuffer)
                continue;
            if (result != 0)
                break;

            int count = BinaryPrimitives.ReadInt32LittleEndian(table);
            if (count >= 0 && count <= (size - 4) / RowSize)
                return table;
            break;
        }

        throw new InvalidDataException("Não foi possível identificar a porta. Desmarque a configuração automática para informá-la.");
    }

    [DllImport("iphlpapi.dll")]
    private static extern uint GetExtendedTcpTable([Out] byte[]? table, ref int size,
        [MarshalAs(UnmanagedType.Bool)] bool sort, int addressFamily, int tableClass, uint reserved);
}
