namespace GazeMagnet;

internal sealed partial class MainForm : Form
{
    private const int ManualMovementDelayMs = 2000;
    private const int MouseClickDelayMs = 230;
    private const string ArmedMessage = "Cursor pelo olhar. Ctrl + Alt + F12 devolve o controle manual.";
    private const string ManualMovementMessage = "Mouse em uso. O olhar retoma após 2 segundos sem movimento.";
    private const string WaitingForGazeMessage = "Sinal do olhar instável. Aguardando retorno automático…";

    private const int ClickProbeTimeoutMs = 500;
    private const int ClickPulseMs = 300;

    private enum GazeClickMode { Off, Practice, LeftClick }

    private readonly Label clickStatus = new();
    private readonly ComboBox clickMode = new DashboardComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170 };
    private readonly NumericUpDown clickTime = new()
    {
        Minimum = 0.6m, Maximum = 4.0m, Value = 1.0m,
        DecimalPlaces = 1, Increment = 0.1m, Width = 100, Enabled = false
    };
    private readonly ClickDwell clickDwell = new();
    private readonly MouseActivity mouseActivity = new();

    private readonly Label connectionStatus = new();
    private readonly Label controlStatus = new();
    private readonly TextBox key = new() { Text = GazePointerPort.DemoKey, Width = 170 };
    private readonly NumericUpDown port = Number(GazePointerPort.DefaultPort, 1, 65535);
    private readonly CheckBox autoConnection = Check("Configurar conexão automaticamente", true);
    private int manualPort = GazePointerPort.DefaultPort;
    private string manualKey = GazePointerPort.DemoKey;

    private readonly NumericUpDown radius = Number(56, 10, 250);
    private readonly NumericUpDown dwell = Number(320, 80, 1600);
    private readonly NumericUpDown hysteresis = Number(24, 4, 100);
    private readonly NumericUpDown maxTargetWidth = Number(TargetSizeLimits.Default.Width, 24, 8000);
    private readonly NumericUpDown maxTargetHeight = Number(TargetSizeLimits.Default.Height, 16, 1500);
    private readonly ComboBox gazeStability = new DashboardComboBox() { DropDownStyle = ComboBoxStyle.DropDownList };
    private TargetSizeLimits sizeLimits = TargetSizeLimits.Default;
    private readonly Button connect = Button("Conectar", DashboardGlyph.Link);
    private readonly Button arm = Button("Assumir cursor", DashboardGlyph.Cursor);
    private readonly CheckBox confirmed = Check("Calibrei o GazePointer e desliguei o botão Control Mouse Cursor.");
    private readonly CheckBox magnetic = Check("Usar magnetismo", true);
    private readonly CheckBox targetsOnly = Check("Mover só entre alvos");
    private readonly CheckBox demo = Check("Demonstração com mouse (apenas destaque)");
    private readonly CheckBox showSearchArea = Check("Mostrar área de busca e mira");
    private readonly CheckBox enterClicksTarget = Check("Clicar no alvo verde com Enter");
    private readonly EnterClickShortcut enterClickShortcut;
    private readonly string settingsPath;
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 30 };
    private readonly TargetOverlay overlay = new();
    private readonly TargetOverlay candidateOverlay = new();
    private readonly SearchAreaOverlay searchAreaOverlay = new();
    private readonly TargetScanner scanner = new();
    private readonly Magnet magnet = new();
    private readonly GazeRecovery gazeRecovery = new();
    private readonly GazeStabilizer gazeStabilizer = new();

    private CancellationTokenSource? connection;
    private GazeSample? latest;
    private bool authorized;
    private bool reconnecting;
    private bool resumeAfterReconnect;
    private bool armed;
    private bool emergencyRegistered;
    private bool clickShortcutRegistered;
    private GazeClickMode lastClickMode = GazeClickMode.Practice;
    private ClickProbe? pendingClick;
    private Target? pulseTarget;
    private nint pulseWindow;
    private long pulseUntil;

    private GazeClickMode CurrentClickMode => (GazeClickMode)clickMode.SelectedIndex;
    private bool closing;
    private Point lastCursor;
    private Point? previousGaze;
    private long holdUntil;
    private bool manualMovement;
    private nint lastWindow;
    private (Target Target, nint Window)? enterClickTarget;
    private bool enterClickPending;

    public MainForm() : this(AppSettings.DefaultPath)
    {
    }

    internal MainForm(string settingsPath)
    {
        this.settingsPath = settingsPath;
        enterClickShortcut = new EnterClickShortcut(TryQueueEnterClick);
        BuildDashboard();

        clickMode.SelectedIndexChanged += (_, _) => ChangeClickMode();
        clickTime.ValueChanged += (_, _) => CancelGazeClick();

        autoConnection.CheckedChanged += (_, _) => ChangeConnectionMode();
        RefreshConnectionFields();
        connect.Click += (_, _) => ToggleConnection();
        arm.Click += (_, _) => ToggleControl();
        emergency.Click += (_, _) => EmergencyStop();
        confirmed.CheckedChanged += (_, _) =>
        {
            if (!confirmed.Checked && (armed || resumeAfterReconnect))
                Pause("Cursor manual. Confirme o controle antes de rearmar.");
        };
        demo.CheckedChanged += (_, _) =>
        {
            Pause(demo.Checked ? "Demonstração ativa. Mova o mouse perto de um botão." : "Cursor manual.");
            targetsOnly.Enabled = magnetic.Checked && !demo.Checked;
            if (demo.Checked)
                confirmed.Checked = false;
        };
        magnetic.CheckedChanged += (_, _) =>
        {
            targetsOnly.Enabled = magnetic.Checked && !demo.Checked;
            if (!magnetic.Checked)
                searchAreaOverlay.Clear();
            ResetTarget();
        };
        showSearchArea.CheckedChanged += (_, _) =>
        {
            if (!showSearchArea.Checked)
                searchAreaOverlay.Clear();
        };
        enterClicksTarget.CheckedChanged += (_, _) =>
        {
            if (!enterClicksTarget.Checked)
                enterClickShortcut.Dispose();
            else if (!enterClickShortcut.Start())
            {
                enterClicksTarget.Checked = false;
                controlStatus.Text = "Não foi possível ativar o clique com Enter.";
            }
        };
        targetsOnly.CheckedChanged += (_, _) => ResetTarget();
        radius.ValueChanged += (_, _) => ResetTarget();
        dwell.ValueChanged += (_, _) => ResetTarget();
        hysteresis.ValueChanged += (_, _) => ResetTarget();
        gazeStability.SelectedIndexChanged += (_, _) =>
        {
            gazeStabilizer.Reset();
            ResetTarget();
        };
        maxTargetWidth.ValueChanged += (_, _) => ChangeSizeLimits();
        maxTargetHeight.ValueChanged += (_, _) => ChangeSizeLimits();
        RestoreSettings();
        timer.Tick += (_, _) =>
        {
            Tick();
            RefreshDashboard();
        };
        timer.Start();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        DashboardStyle.SetWindowColors(Handle);
        emergencyRegistered = Native.RegisterShortcut(Handle, Native.EmergencyId, Keys.F12);
        clickShortcutRegistered = Native.RegisterShortcut(Handle, Native.ToggleGazeClickId, Keys.F11);
        if (!clickShortcutRegistered)
            clickStatus.Text = "Ctrl + Alt + F11 indisponível. Use o seletor Modo.";
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (emergencyRegistered)
            Native.UnregisterShortcut(Handle, Native.EmergencyId);
        if (clickShortcutRegistered)
            Native.UnregisterShortcut(Handle, Native.ToggleGazeClickId);
        clickShortcutRegistered = false;
        emergencyRegistered = false;
        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == Native.HotKeyMessage && message.WParam == Native.EmergencyId)
        {
            EmergencyStop();
            return;
        }
        if (message.Msg == Native.HotKeyMessage && message.WParam == Native.ToggleGazeClickId)
        {
            clickMode.SelectedIndex = (int)(CurrentClickMode == GazeClickMode.Off
                ? lastClickMode : GazeClickMode.Off);
            return;
        }
        base.WndProc(ref message);
    }

    private void EmergencyStop()
    {
        clickMode.SelectedIndex = (int)GazeClickMode.Off;
        demo.Checked = false;
        confirmed.Checked = false;
        Pause("Emergência acionada. O cursor voltou ao controle manual.");
        RefreshDashboard();
    }

    private void ChangeConnectionMode()
    {
        if (autoConnection.Checked)
        {
            manualPort = (int)port.Value;
            manualKey = key.Text;
            port.Value = GazePointerPort.DefaultPort;
            key.Text = GazePointerPort.DemoKey;
        }
        else
        {
            port.Value = manualPort;
            key.Text = manualKey;
        }

        RefreshConnectionFields();
        connectionStatus.Text = autoConnection.Checked
            ? "Abra o GazePointer e clique em Conectar."
            : "Informe a porta e a chave e clique em Conectar.";
    }

    private void RefreshConnectionFields()
    {
        autoConnection.Enabled = connection == null;
        port.Enabled = key.Enabled = connection == null && !autoConnection.Checked;
        key.UseSystemPasswordChar = !autoConnection.Checked;
    }

    private void ToggleControl()
    {
        if (armed || resumeAfterReconnect)
        {
            Pause("Pausado. Cursor manual.");
            return;
        }
        if (demo.Checked || !emergencyRegistered || !confirmed.Checked || !authorized ||
            ValidGaze(Environment.TickCount64) == null)
        {
            controlStatus.Text = demo.Checked ? "Desative a demonstração para assumir o cursor." :
                !emergencyRegistered ? "Atalho ocupado. Libere Ctrl + Alt + F12 e reabra o GazeMagnet." :
                !confirmed.Checked ? "Confirme a calibração e desligue o controle nativo do mouse." :
                "Aguarde coordenadas válidas da API antes de assumir o cursor.";
            return;
        }

        ArmCursor(resuming: false);
    }

    private void ArmCursor(bool resuming)
    {
        ResetTarget();
        lastCursor = Cursor.Position;
        long startAt = Environment.TickCount64 + 400;
        holdUntil = resuming ? Math.Max(holdUntil, startAt) : startAt;
        manualMovement = resuming && manualMovement;
        resumeAfterReconnect = false;
        armed = true;
        arm.Text = "Pausar cursor";
        controlStatus.Text = manualMovement ? ManualMovementMessage : ArmedMessage;
    }

    private GazeSample? ValidGaze(long now)
    {
        GazeSample? sample = Volatile.Read(ref latest);
        if (sample?.Position is not Point point || now - sample.ReceivedAt > 650 ||
            Screen.PrimaryScreen?.Bounds.Contains(point) != true)
            return null;
        return sample;
    }

    private void Tick()
    {
        long now = Environment.TickCount64;
        GazeSample? gaze = ValidGaze(now);
        if (authorized)
            connectionStatus.Text = gaze != null ? "API conectada · recebendo coordenadas do olhar." :
                "API conectada · sem coordenadas válidas. Confira o rosto e a calibração.";

        if (resumeAfterReconnect)
            TryResumeAfterReconnect(gaze, now);

        bool showTargets = (demo.Checked || armed) && magnetic.Checked;
        if (!demo.Checked && !armed)
            return;

        Point cursor = Cursor.Position;
        if (armed && !CanUseGaze(gaze, cursor, now))
            return;

        var stability = (GazeStability)gazeStability.SelectedIndex;
        Point point = demo.Checked ? cursor : gazeStabilizer.Update(gaze!.Position!.Value, gaze.ReceivedAt, stability);
        int searchRadius = (int)radius.Value;
        nint window = showTargets ? Native.RootAt(point) : 0;
        if (showTargets && showSearchArea.Checked)
            searchAreaOverlay.Display(point, searchRadius);
        scanner.SetWindow(window);
        if (lastWindow != window)
        {
            ResetTarget();
            lastWindow = window;
        }

        bool keepOnTargets = armed && magnetic.Checked && targetsOnly.Checked;
        Point destination = point;
        Target? lockedTarget = null;
        Target? candidate = null;
        TargetSnapshot? snapshot = scanner.Latest;
        if (showTargets && snapshot != null && snapshot.Window == window &&
            snapshot.Limits == sizeLimits && now - snapshot.CapturedAt <= 900)
        {
            var (target, fixedOnTarget) = magnet.Update(snapshot.Targets, point, demo.Checked ? now : gaze!.ReceivedAt,
                searchRadius, (int)dwell.Value, (int)hysteresis.Value, keepOnTargets);
            lockedTarget = VisibleTarget(magnet.LockedTarget, window);
            if (!fixedOnTarget)
                candidate = VisibleTarget(target, window);
            if (lockedTarget != null)
                destination = lockedTarget.Center;
        }
        else
        {
            magnet.Reset();
            previousGaze = null;
        }

        if (demo.Checked)
        {
            DisplayTargets(lockedTarget, candidate, 0, false);
            return;
        }

        // O sinal recém-lido pode ter sido invalidado durante a seleção do alvo.
        long beforeMove = Environment.TickCount64;
        GazeSample? recent = ValidGaze(beforeMove);
        if (recent?.Position is not Point currentGaze)
        {
            CanUseGaze(null, Cursor.Position, beforeMove);
            return;
        }
        if (ManualCursorHasPriority(Cursor.Position, Native.MouseButtonDown(), beforeMove, recent.ReceivedAt))
            return;

        bool canMove = !keepOnTargets ||
            (lockedTarget != null && recent == gaze && !magnet.IsSwitchingTarget);
        if (canMove && !MoveCursor(cursor, point, destination, lockedTarget != null))
            return;

        // Uma nova amostra só entra na contagem depois de passar pela seleção do alvo.
        if (recent != gaze)
        {
            Point recentPoint = gazeStabilizer.Update(currentGaze, recent.ReceivedAt, stability);
            if (lockedTarget == null || magnet.IsSwitchingTarget || lockedTarget.DistanceTo(recentPoint) > searchRadius ||
                Native.RootAt(currentGaze) != window)
            {
                CancelGazeClick();
                enterClickTarget = null;
            }
            return;
        }

        Target? clickTarget = magnet.IsSwitchingTarget ? null : lockedTarget;
        enterClickTarget = clickTarget != null && window != Handle ? (clickTarget, window) : null;
        var (progress, pulse) = UpdateGazeClick(enterClickPending ? null : clickTarget,
            window, point, recent, beforeMove, searchRadius);
        DisplayTargets(lockedTarget, candidate, progress, pulse);
    }

    private bool MoveCursor(Point cursor, Point point, Point destination, bool locked)
    {
        // Evita microdeslocamentos entre alvos, mas vai ao centro exato ao prender.
        if (destination == cursor || (!locked && (previousGaze == point ||
            (Math.Abs(destination.X - cursor.X) <= 2 && Math.Abs(destination.Y - cursor.Y) <= 2))))
            return true;

        if (Native.SetCursorPos(destination.X, destination.Y) == 0)
        {
            Pause("O Windows bloqueou o movimento. Cursor manual.");
            return false;
        }
        previousGaze = locked ? null : point;
        lastCursor = destination;
        return true;
    }

    private void ChangeClickMode()
    {
        CancelGazeClick();
        var mode = CurrentClickMode;
        clickTime.Enabled = mode != GazeClickMode.Off;
        if (mode == GazeClickMode.Off)
        {
            mouseActivity.Dispose();
            clickStatus.Text = clickShortcutRegistered
                ? "Desligado. Ctrl + Alt + F11 pausa ou retoma o clique pelo olhar."
                : "Desligado. Use o seletor Modo.";
            return;
        }

        if (!mouseActivity.Start())
        {
            clickMode.SelectedIndex = (int)GazeClickMode.Off;
            clickStatus.Text = "Clique pelo olhar indisponível. Não foi possível acompanhar os cliques manuais.";
            return;
        }
        lastClickMode = mode;
        clickStatus.Text = mode == GazeClickMode.Practice
            ? "Treino com o olhar real. O preenchimento simula o clique."
            : "Clique esquerdo ativo fora deste painel quando o cursor estiver pelo olhar.";
    }

    private (double Progress, bool Pulse) UpdateGazeClick(
        Target? target, nint window, Point point, GazeSample sample, long now, int radius)
    {
        GazeClickMode mode = CurrentClickMode;
        if (mode == GazeClickMode.Off)
            return default;

        if (Native.ModifierDown())
        {
            CancelGazeClick();
            return default;
        }

        // A posição original do olhar também precisa pertencer à janela do alvo suavizado.
        if (target != null && (lastCursor != target.Center || Native.RootAt(target.Center) != window ||
            (sample.Position is not Point raw || Native.RootAt(raw) != window) ||
            (mode == GazeClickMode.LeftClick && window == Handle)))
            target = null;

        double progress = clickDwell.Update(target, point, sample.ReceivedAt, (int)(clickTime.Value * 1000), radius);
        if (target == null || progress < 1)
        {
            CancelProbe();
            return (progress, target != null && target == pulseTarget && window == pulseWindow && now < pulseUntil);
        }

        if (pendingClick == null)
        {
            pendingClick = new ClickProbe(window, target, now);
            scanner.VerifyClick(pendingClick);
            return (progress, false);
        }

        ClickProbe probe = pendingClick;
        if (probe.Target != target || probe.Window != window || now - probe.StartedAt > ClickProbeTimeoutMs)
        {
            CancelGazeClick();
            return default;
        }

        ClickProbeResult? result = scanner.ProbeResult;
        if (result?.Probe != probe)
            return (progress, false);

        if (now - sample.ReceivedAt > 300)
            return (progress, false);

        CancelProbe();
        if (!result.IsMatch || Native.RootAt(target.Center) != window || Cursor.Position != target.Center ||
            Native.MouseButtonDown() || Native.ModifierDown() ||
            ValidGaze(Environment.TickCount64) != sample)
        {
            CancelGazeClick();
            return default;
        }

        clickDwell.Complete(target);
        if (mode == GazeClickMode.LeftClick && !Native.SendLeftClick())
        {
            clickStatus.Text = "O Windows não confirmou o envio do clique.";
            return default;
        }

        pulseTarget = target;
        pulseWindow = window;
        pulseUntil = now + ClickPulseMs;
        clickStatus.Text = mode == GazeClickMode.Practice
            ? "Treino: clique simulado. Tire o alvo do círculo para repetir."
            : "Clique enviado. Tire o alvo do círculo para repetir.";
        return (1, true);
    }

    private Target? VisibleTarget(Target? target, nint window) =>
        target != null && Native.RootAt(target.Center) == window &&
        (demo.Checked || Screen.PrimaryScreen?.Bounds.Contains(target.Center) == true) ? target : null;

    private bool TryQueueEnterClick()
    {
        if (enterClickPending || enterClickTarget is not { } request || !CanClickWithEnter(request))
            return false;

        enterClickPending = true;
        CancelGazeClick();
        BeginInvoke(() =>
        {
            enterClickPending = false;
            if (enterClickTarget != request || !CanClickWithEnter(request))
                return;
            if (Native.SendLeftClick())
                clickDwell.Complete(request.Target);
            else
                controlStatus.Text = "O Windows não confirmou o clique com Enter.";
        });
        return true;
    }

    private bool CanClickWithEnter((Target Target, nint Window) request) =>
        !closing && IsHandleCreated && enterClicksTarget.Checked && armed && !manualMovement &&
        ValidGaze(Environment.TickCount64) is { } gaze && gaze.ReceivedAt >= holdUntil &&
        !Native.ModifierDown() && !Native.MouseButtonDown() &&
        Cursor.Position == request.Target.Center && Native.RootAt(request.Target.Center) == request.Window;

    private void DisplayTargets(Target? locked, Target? candidate, double progress, bool pulse)
    {
        ShowTargetStatus(candidate != null, locked != null);
        if (candidate == null)
            candidateOverlay.Clear();
        else
            candidateOverlay.Display(candidate.Bounds, false);

        if (locked == null)
            overlay.Clear();
        else
            overlay.Display(locked.Bounds, true, progress, pulse);
    }

    private void CancelProbe()
    {
        if (pendingClick == null)
            return;
        pendingClick = null;
        scanner.VerifyClick(null);
    }

    private void CancelGazeClick()
    {
        clickDwell.Cancel();
        CancelProbe();
        pulseTarget = null;
    }

    private bool CanUseGaze(GazeSample? gaze, Point cursor, long now)
    {
        GazeAvailability availability = gazeRecovery.Update(now, gaze?.ReceivedAt);
        if (availability == GazeAvailability.TimedOut)
        {
            Pause("Sinal indisponível por 5 segundos. Clique em Assumir cursor após recuperar o sinal.");
            return false;
        }

        bool mouseDown = Native.MouseButtonDown();
        if (availability == GazeAvailability.Waiting)
        {
            ObserveManualActivity(cursor, mouseDown, now);
            StopTargetSearch();
            controlStatus.Text = WaitingForGazeMessage;
            return false;
        }

        if (availability == GazeAvailability.Recovered)
            controlStatus.Text = manualMovement ? ManualMovementMessage : ArmedMessage;

        return !ManualCursorHasPriority(cursor, mouseDown, now, gaze!.ReceivedAt);
    }

    private bool ObserveManualActivity(Point cursor, bool mouseDown, long now)
    {
        bool moved = cursor != lastCursor;
        if (moved)
        {
            lastCursor = cursor;
            holdUntil = now + ManualMovementDelayMs;
            manualMovement = true;
        }

        holdUntil = Math.Max(holdUntil, mouseActivity.LastButtonPressAt + MouseClickDelayMs);
        if (mouseDown)
            holdUntil = Math.Max(holdUntil, now + MouseClickDelayMs);
        return moved;
    }

    private bool ManualCursorHasPriority(Point cursor, bool mouseDown, long now, long sampleAt)
    {
        if (ObserveManualActivity(cursor, mouseDown, now))
            controlStatus.Text = ManualMovementMessage;
        if (now < holdUntil || sampleAt < holdUntil)
        {
            StopTargetSearch();
            return true;
        }

        if (manualMovement)
        {
            manualMovement = false;
            controlStatus.Text = ArmedMessage;
        }
        return false;
    }

    private void ChangeSizeLimits()
    {
        sizeLimits = new TargetSizeLimits((int)maxTargetWidth.Value, (int)maxTargetHeight.Value);
        scanner.SetSizeLimits(sizeLimits);
        ResetTarget();
    }

    private void ResetTarget()
    {
        magnet.Reset();
        enterClickTarget = null;
        CancelGazeClick();
        overlay.Clear();
        candidateOverlay.Clear();
        ShowTargetStatus(false, false);
        previousGaze = null;
    }

    private void Pause(string message)
    {
        armed = false;
        resumeAfterReconnect = false;
        gazeRecovery.Reset();
        arm.Text = "Assumir cursor";
        StopTargetSearch();
        controlStatus.Text = message;
    }

    private void StopTargetSearch()
    {
        scanner.SetWindow(0);
        gazeStabilizer.Reset();
        ResetTarget();
        searchAreaOverlay.Clear();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (e.Cancel)
            return;

        SaveSettings();
        closing = true;
        timer.Dispose();
        connection?.Cancel();
        mouseActivity.Dispose();
        enterClickShortcut.Dispose();
        scanner.Dispose();
        overlay.Dispose();
        candidateOverlay.Dispose();
        searchAreaOverlay.Dispose();
    }

    private static CheckBox Check(string text, bool value = false) =>
        new DashboardSwitch { Text = text, Checked = value, Font = DashboardStyle.Body };

    private static Button Button(string text, DashboardGlyph glyph) =>
        new DashboardButton(glyph) { Text = text, Font = DashboardStyle.Action, Cursor = Cursors.Hand };

    private static NumericUpDown Number(int value, int min, int max) => new()
    {
        Minimum = min, Maximum = max, Value = value, Width = 100
    };
}
