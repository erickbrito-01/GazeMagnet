using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace GazeMagnet;

internal sealed record GazeSample(Point? Position, long ReceivedAt);
internal sealed class GazeSessionExpiredException() : IOException("O tempo de uso da chave terminou.");

internal static class GazeFlowClient
{
    public static async Task ReceiveAsync(int port, string key, Action authorized, Action<GazeSample> sample,
        CancellationToken stop)
    {
        using var tcp = new TcpClient(AddressFamily.InterNetwork) { NoDelay = true };
        using (var connect = CancellationTokenSource.CreateLinkedTokenSource(stop))
        {
            connect.CancelAfter(TimeSpan.FromSeconds(3));
            await tcp.ConnectAsync(IPAddress.Loopback, port, connect.Token).ConfigureAwait(false);
        }

        using NetworkStream stream = tcp.GetStream();
        using var request = new MemoryStream();
        request.Write("xml"u8);
        using (var writer = new BinaryWriter(request, Encoding.UTF8, leaveOpen: true))
            writer.Write(key);

        await stream.WriteAsync(request.ToArray(), stop).ConfigureAwait(false);
        string response;
        try
        {
            response = await ReadFrameAsync(stream, TimeSpan.FromSeconds(60), stop).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!stop.IsCancellationRequested)
        {
            throw new TimeoutException("Tempo de espera pela autorização esgotado.");
        }
        if (response != "ok" && !response.StartsWith("ok ", StringComparison.Ordinal))
            throw new InvalidDataException("O GazePointer recusou o acesso. Confira a chave e a janela de autorização.");

        authorized();
        string? previousFrame = null;
        long changedAt = Environment.TickCount64;
        while (!stop.IsCancellationRequested)
        {
            // Depois que o fluxo começou, detecte silêncio na conexão antes
            // de o controle perder a intenção de retomar por falta de olhar.
            TimeSpan frameTimeout = TimeSpan.FromSeconds(previousFrame == null ? 10 : 4);
            string frame = await ReadFrameAsync(stream, frameTimeout, stop).ConfigureAwait(false);
            if (frame.StartsWith("time limit", StringComparison.OrdinalIgnoreCase))
                throw new GazeSessionExpiredException();

            long now = Environment.TickCount64;
            if (frame != previousFrame)
                changedAt = now;
            previousFrame = frame;

            // O protocolo legado não envia TrackerState. Quadros idênticos por muito
            // tempo podem indicar perda do rosto mesmo quando o servidor continua enviando.
            Point? position = now - changedAt <= 800 ? ReadPosition(frame) : null;
            sample(new GazeSample(position, now));
        }
    }

    private static Point? ReadPosition(string frame)
    {
        try
        {
            using var input = new StringReader(frame);
            using var reader = XmlReader.Create(input, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                MaxCharactersInDocument = 8192
            });
            XElement data = XElement.Load(reader);
            if (data.Name.LocalName != "CGazeData" ||
                !ReadCoordinate(data, "GazeX", out double x) ||
                !ReadCoordinate(data, "GazeY", out double y) ||
                x < 0 || y < 0 || x > 100000 || y > 100000 ||
                (x == 0 && y == 0))
                return null;

            return new Point((int)Math.Round(x), (int)Math.Round(y));
        }
        catch (XmlException)
        {
            return null;
        }
    }

    private static bool ReadCoordinate(XElement data, string name, out double value) =>
        double.TryParse(data.Element(name)?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
        double.IsFinite(value);

    private static async Task<string> ReadFrameAsync(Stream stream, TimeSpan timeout, CancellationToken stop)
    {
        using var read = CancellationTokenSource.CreateLinkedTokenSource(stop);
        read.CancelAfter(timeout);
        var prefix = new byte[1];
        int length = 0;
        for (int shift = 0; shift <= 7; shift += 7)
        {
            await stream.ReadExactlyAsync(prefix, read.Token).ConfigureAwait(false);
            length |= (prefix[0] & 0x7f) << shift;
            if ((prefix[0] & 0x80) != 0)
                continue;
            if (length == 0 || length > 8192)
                break;

            var frame = new byte[length];
            await stream.ReadExactlyAsync(frame, read.Token).ConfigureAwait(false);
            return Encoding.UTF8.GetString(frame);
        }

        throw new InvalidDataException("O GazePointer enviou uma resposta fora do formato esperado.");
    }
}
