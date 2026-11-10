using System.Net.Sockets;

namespace GazeMagnet;

internal sealed partial class MainForm
{
    private const int ReconnectLimit = 3;
    private const int StableConnectionMs = 5000;

    private sealed record ConnectionEnd(string Message, bool Retryable);

    private async void ToggleConnection()
    {
        if (connection != null)
        {
            if (armed || resumeAfterReconnect)
                Pause("Desconectando. Cursor manual.");
            connection.Cancel();
            return;
        }

        bool automatic = autoConnection.Checked;
        string appKey = automatic ? GazePointerPort.DemoKey : key.Text.Trim();
        if (appKey.Length == 0)
        {
            connectionStatus.Text = "Informe uma chave. Para teste, use AppKeyDemo.";
            return;
        }

        using var session = new CancellationTokenSource();
        CancellationToken stop = session.Token;
        connection = session;
        connect.Text = "Desconectar";
        RefreshConnectionFields();
        connectionStatus.Text = "Conectando… verifique se apareceu um pedido de acesso no GazePointer.";

        bool previouslyAuthorized = false;
        int retries = 0;
        try
        {
            while (true)
            {
                bool attemptEnded = false;
                bool accepted = false;
                long? firstValidAt = null;
                long lastValidAt = 0;
                ConnectionEnd end = await ConnectOnceAsync(automatic, appKey,
                    () =>
                    {
                        accepted = true;
                        if (stop.IsCancellationRequested)
                            return;
                        BeginInvoke(() =>
                        {
                            if (attemptEnded || stop.IsCancellationRequested)
                                return;
                            authorized = true;
                            reconnecting = false;
                            connect.Text = "Desconectar";
                            connectionStatus.Text = "API autorizada. Aguardando coordenadas do olhar…";
                            if (resumeAfterReconnect)
                                gazeRecovery.Update(Environment.TickCount64, null);
                        });
                    },
                    sample =>
                    {
                        if (sample.Position != null)
                        {
                            firstValidAt ??= sample.ReceivedAt;
                            lastValidAt = sample.ReceivedAt;
                        }
                        Volatile.Write(ref latest, sample);
                    }, stop);

                attemptEnded = true;
                authorized = false;
                Volatile.Write(ref latest, null);
                stop.ThrowIfCancellationRequested();
                previouslyAuthorized |= accepted;
                if (!previouslyAuthorized || !end.Retryable)
                {
                    connectionStatus.Text = end.Message;
                    break;
                }

                if (firstValidAt is long first && lastValidAt - first >= StableConnectionMs)
                    retries = 0;
                if (retries == ReconnectLimit)
                {
                    connectionStatus.Text = $"Reconexão encerrada após {ReconnectLimit} tentativas. {end.Message}";
                    break;
                }

                PrepareReconnect();
                connectionStatus.Text = $"{end.Message} Reconectando ({retries + 1}/{ReconnectLimit})…";
                await Task.Delay(1000 << retries, stop);
                retries++;
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
            if (!closing)
                connectionStatus.Text = "Desconectado.";
        }
        catch (InvalidOperationException) when (closing)
        {
        }
        finally
        {
            connection = null;
            authorized = false;
            reconnecting = false;
            Volatile.Write(ref latest, null);
            if (!closing)
            {
                if (armed || resumeAfterReconnect)
                    Pause("Conexão encerrada. Cursor manual.");
                connect.Text = "Conectar";
                RefreshConnectionFields();
            }
        }
    }

    private async Task<ConnectionEnd> ConnectOnceAsync(bool automatic, string appKey, Action accepted,
        Action<GazeSample> sample, CancellationToken stop)
    {
        bool findingPort = automatic;
        try
        {
            int selectedPort = (int)port.Value;
            if (automatic)
            {
                if (!reconnecting)
                    connectionStatus.Text = "Procurando a porta do GazePointer…";
                selectedPort = await Task.Run(GazePointerPort.Find, stop);
                stop.ThrowIfCancellationRequested();
                port.Value = selectedPort;
                findingPort = false;
            }
            if (reconnecting)
                connectionStatus.Text = "Reconectando… confirme o acesso no GazePointer se ele pedir.";
            else if (automatic)
                connectionStatus.Text = $"Porta {selectedPort} encontrada. Confira se o GazePointer pediu autorização.";

            await Task.Run(() => GazeFlowClient.ReceiveAsync(selectedPort, appKey, accepted, sample, stop), stop);
            stop.ThrowIfCancellationRequested();
            return new("A conexão com o GazePointer foi encerrada.", true);
        }
        catch (GazeSessionExpiredException error)
        {
            return new(error.Message, true);
        }
        catch (TimeoutException error)
        {
            return new(error.Message, false);
        }
        catch (OperationCanceledException) when (!stop.IsCancellationRequested)
        {
            return new("Tempo de espera esgotado. Confira o GazePointer.", true);
        }
        catch (InvalidDataException error)
        {
            return new(error.Message, findingPort);
        }
        catch (SocketException)
        {
            return new("API indisponível. Abra o GazePointer e confira a porta local.", true);
        }
        catch (IOException)
        {
            return new("A conexão com o GazePointer foi encerrada.", true);
        }
    }

    private void PrepareReconnect()
    {
        reconnecting = true;
        connect.Text = "Cancelar reconexão";
        if (!armed && !resumeAfterReconnect)
            return;

        Pause("Cursor manual enquanto a API reconecta. O olhar volta com sinal estável.");
        resumeAfterReconnect = true;
        arm.Text = "Cancelar retomada";
    }

    private void TryResumeAfterReconnect(GazeSample? gaze, long now)
    {
        ObserveManualActivity(Cursor.Position, Native.MouseButtonDown(), now);
        if (!authorized)
            return;

        GazeAvailability availability = gazeRecovery.Update(now, gaze?.ReceivedAt);
        if (availability == GazeAvailability.TimedOut || !confirmed.Checked || !emergencyRegistered || demo.Checked)
            Pause("API reconectada. Confirme a calibração e use Assumir cursor quando o sinal voltar.");
        else if (availability == GazeAvailability.Recovered)
            ArmCursor(resuming: true);
    }
}
