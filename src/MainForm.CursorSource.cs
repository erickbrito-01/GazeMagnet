namespace GazeMagnet;

internal sealed partial class MainForm
{
    private const int CursorIdleMs = 2000;
    private const string CursorInstruction = "No GazePointer, calibre o olhar e ligue Control Mouse Cursor. Depois, ative o magnetismo no painel de controle.";
    private readonly ComboBox cursorSourceChoice = new DashboardComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
    private bool rawMouseRegistered;
    private Point observedCursor;
    private Point? searchPoint;
    private long lastExternalMovement;
    private long physicalHoldUntil;
    private Target? lastSnap;
    private Target? visibleSnap;
    private bool UsingNativeCursor => cursorSourceChoice.SelectedIndex == 1;

    private void ChangeCursorSource()
    {
        confirmed.Checked = false;
        Pause(UsingNativeCursor ? "Magnetismo pausado. O GazePointer controla o movimento livre." : "Cursor manual.");
        clickMode.SelectedIndex = (int)GazeClickMode.Off;
        clickMode.Enabled = !UsingNativeCursor;
        targetsOnly.Enabled = !UsingNativeCursor && magnetic.Checked && !demo.Checked;
        confirmed.Text = UsingNativeCursor
            ? "Calibrei o GazePointer e liguei Control Mouse Cursor."
            : "Calibrei o GazePointer e desliguei o botão Control Mouse Cursor.";
        ChangeClickMode();
        RefreshConnectionFields();
        connectionStatus.Text = UsingNativeCursor ? CursorInstruction : autoConnection.Checked
            ? "Abra o GazePointer e clique em Conectar."
            : "Informe a porta e a chave e clique em Conectar.";
        RefreshDashboard();
    }

    private void ArmNativeCursor()
    {
        if (demo.Checked || !emergencyRegistered || !confirmed.Checked || !magnetic.Checked)
        {
            controlStatus.Text = demo.Checked ? "Desative a demonstração para ativar o magnetismo." :
                !emergencyRegistered ? EmergencyUnavailable :
                !confirmed.Checked ? "Confirme a calibração e ligue Control Mouse Cursor no GazePointer." :
                "Marque Usar magnetismo antes de ativar.";
            return;
        }
        if (!RawMouseInput.Register(Handle))
        {
            controlStatus.Text = "Magnetismo pausado. O Windows não permitiu identificar o mouse físico.";
            return;
        }

        rawMouseRegistered = true;
        StopTargetSearch();
        observedCursor = Cursor.Position;
        physicalHoldUntil = Environment.TickCount64 + ManualMovementDelayMs;
        armed = true;
        arm.Text = "Pausar magnetismo";
        controlStatus.Text = "Aguardando movimento do cursor do GazePointer.";
        RefreshConnectionFields();
    }

    private void ReleaseRawMouse()
    {
        if (!rawMouseRegistered)
            return;
        RawMouseInput.Unregister();
        rawMouseRegistered = false;
        searchPoint = null;
        lastSnap = visibleSnap = null;
    }

    private void TickNativeCursor(long now)
    {
        Point cursor = Cursor.Position;
        if (now < physicalHoldUntil)
        {
            observedCursor = cursor;
            searchPoint = null;
            lastSnap = visibleSnap = null;
            StopTargetSearch();
            controlStatus.Text = "Aguardando o mouse ficar parado.";
            return;
        }

        if (cursor != observedCursor)
        {
            observedCursor = cursor;
            lastExternalMovement = now;
            searchPoint = gazeStabilizer.Update(cursor, now, (GazeStability)gazeStability.SelectedIndex);
            visibleSnap = null;
            if (lastSnap?.DistanceTo(cursor) > (int)(radius.Value + hysteresis.Value))
                lastSnap = null;
        }

        if (searchPoint is not Point point || now - lastExternalMovement >= CursorIdleMs)
        {
            searchPoint = null;
            visibleSnap = null;
            StopTargetSearch();
            controlStatus.Text = "Aguardando movimento do cursor do GazePointer.";
            return;
        }

        var (locked, candidate, window) = SelectTargets(point, now, now, true, true);
        if (locked == null || locked != visibleSnap)
            visibleSnap = null;
        if (locked != null && !magnet.IsSwitchingTarget && locked.Id != lastSnap?.Id &&
            locked.DistanceTo(cursor) <= (int)radius.Value && Native.RootAt(cursor) == window &&
            Cursor.Position == cursor && !Native.MouseButtonDown())
        {
            if (cursor != locked.Center && Native.SetCursorPos(locked.Center.X, locked.Center.Y) == 0)
            {
                Pause("O Windows bloqueou o encaixe. Magnetismo pausado.");
                return;
            }
            observedCursor = locked.Center;
            lastSnap = visibleSnap = locked;
        }

        enterClickTarget = visibleSnap != null && window != Handle ? (visibleSnap, window) : null;
        DisplayTargets(visibleSnap, candidate, 0, false);
        controlStatus.Text = $"Magnetismo ativo · cursor do GazePointer. {EmergencyKeyLabel} pausa o encaixe.";
    }
}
