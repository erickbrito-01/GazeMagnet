using System.Drawing.Drawing2D;

namespace GazeMagnet;

internal sealed partial class MainForm
{
    private readonly Panel viewport = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly Panel dashboardPage = new() { Margin = Padding.Empty };
    private readonly DashboardButton emergency = new(DashboardGlyph.Alert, true, "Ctrl + Alt + F10")
    {
        Text = "Parada de emergência",
        Font = DashboardStyle.Action,
        Cursor = Cursors.Hand
    };

    private readonly Label apiHeadline = DashboardText("", DashboardStyle.Action);
    private readonly Label apiDetail = DashboardText("", DashboardStyle.Small, DashboardStyle.Muted);
    private readonly DashboardIcon apiDot = new(DashboardGlyph.Dot, DashboardStyle.Muted);
    private readonly DashboardIcon connectionIcon = new(DashboardGlyph.Check, DashboardStyle.Muted);
    private readonly DashboardIcon cursorIcon = new(DashboardGlyph.Pause, DashboardStyle.Muted);
    private readonly Label cursorState = DashboardText("", DashboardStyle.Action);
    private readonly Label cursorSource = DashboardText("", DashboardStyle.Small);
    private readonly Label footerApi = DashboardText("", DashboardStyle.Small);
    private readonly Label footerApproach = DashboardText("Amarelo tracejado: aproximando", DashboardStyle.Small);
    private readonly Label footerLocked = DashboardText("Verde contínuo: fixado", DashboardStyle.Small);
    private readonly DashboardIcon footerApiIcon = new(DashboardGlyph.Dot, DashboardStyle.Muted);
    private readonly DashboardIcon footerCursorIcon = new(DashboardGlyph.Cursor, DashboardStyle.Muted);
    private readonly DashboardIcon footerStateIcon = new(DashboardGlyph.Pause, DashboardStyle.Muted);
    private readonly DashboardIcon footerApproachIcon = new(DashboardGlyph.Approach, DashboardStyle.Yellow);
    private readonly DashboardIcon footerLockedIcon = new(DashboardGlyph.Fixed, DashboardStyle.Green);
    private readonly Panel footer = new() { BackColor = DashboardStyle.Card };
    private readonly Panel[] footerDividers = [Divider(), Divider(), Divider(), Divider()];
    private readonly ToolTip fieldHelp = new();
    private readonly ToolTip buttonHelp = new() { InitialDelay = 500, ReshowDelay = 150, AutoPopDelay = 9000 };
    private bool arrangingDashboard;

    private void BuildDashboard()
    {
        Text = "GazeMagnet";
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = DashboardStyle.Body;
        ForeColor = DashboardStyle.Text;
        BackColor = DashboardStyle.Surface;
        ClientSize = new Size(1208, 690);
        MinimumSize = new Size(420, 300);
        DoubleBuffered = true;

        viewport.BackColor = dashboardPage.BackColor = DashboardStyle.Surface;
        Controls.Add(viewport);
        viewport.Controls.Add(dashboardPage);
        dashboardPage.Controls.Add(footer);

        clickMode.Items.AddRange(["Desligado", "Treino (sem clique)", "Clique esquerdo"]);
        clickMode.SelectedIndex = (int)GazeClickMode.Off;
        StyleChoiceField(clickMode);
        gazeStability.Items.AddRange(["Desligado", "Leve", "Médio", "Forte"]);
        gazeStability.SelectedIndex = (int)GazeStability.Medium;
        StyleChoiceField(gazeStability);

        SetStatusLabel(connectionStatus, "Abra o GazePointer e clique em Conectar.");
        SetStatusLabel(clickStatus, "Desligado. Ctrl + Alt + F11 pausa ou retoma o clique pelo olhar.");
        SetStatusLabel(controlStatus, "Cursor manual.");

        var header = new DashboardHeader();
        var brand = DashboardText("GazeMagnet", DashboardStyle.Brand);
        var platform = DashboardText("GAZEPOINTER / WINDOWS", DashboardStyle.Action, DashboardStyle.Muted);
        header.Controls.AddRange([brand, platform, apiDot, apiHeadline, apiDetail]);

        var connectionCard = new DashboardCard("1. Conexão", "Conecte aqui o GazeMagnet ao GazePointer.", DashboardGlyph.Link);
        var magnetCard = new DashboardCard("2. Magnetismo", "Ajuste a fixação e os limites de tamanho dos alvos.", DashboardGlyph.Magnet);
        var clickCard = new DashboardCard("3. Clique pelo olhar", "Defina o modo e o tempo de permanência no alvo.", DashboardGlyph.Mouse);
        var cursorCard = new DashboardCard("4. Controle do cursor", "Assuma o controle do cursor com o olhar.", DashboardGlyph.Cursor);
        dashboardPage.Controls.AddRange([header, connectionCard, magnetCard, clickCard, cursorCard]);

        var portField = new DashboardField("Porta local", port);
        var keyField = new DashboardField("Chave da API", key);
        var connectionLine = Divider();
        connectionCard.Controls.AddRange([autoConnection, portField, keyField, connect, connectionLine, connectionIcon, connectionStatus]);

        var radiusField = new DashboardField("Raio (px)", radius, Help("Raio (px)", "Até que distância da borda de um botão ou link o olhar\nconsegue atrair o cursor. Um raio maior alcança alvos\nmais distantes."));
        var dwellField = new DashboardField("Fixação (ms)", dwell, Help("Fixação (ms)", "Quanto tempo o olhar precisa ficar no mesmo alvo para\no cursor grudar. Por exemplo, 320 ms é cerca de um\nterço de segundo."));
        var hysteresisField = new DashboardField("Histerese (px)", hysteresis, Help("Histerese (px)", "Margem extra para o cursor continuar preso quando o\nolhar oscila. Uma margem maior dificulta a troca entre\nalvos próximos."));
        var widthField = new DashboardField("Largura máx. (px)", maxTargetWidth, Help("Largura máx. (px)", "Limite de largura de um alvo. Controles mais largos\nficam fora da seleção. Reduza se faixas muito largas\natraírem o cursor."));
        var heightField = new DashboardField("Altura máx. (px)", maxTargetHeight, Help("Altura máx. (px)", "Limite de altura de um alvo. Controles mais altos\nficam fora da seleção. Reduza se painéis grandes\natraírem o cursor."));
        var stabilityField = new DashboardField("Estabilizar olhar", gazeStability, Help("Estabilizar olhar", "Reduz a tremedeira da mira e do cursor quando você\nolha para o mesmo lugar. Médio equilibra estabilidade\ne resposta. Use Leve se a mira demorar a acompanhar\ne Forte se ela ainda tremer."));
        var magnetHelp = Help("Usar magnetismo", "Atrai o cursor para botões, links e outros controles\npróximos do seu olhar.");
        var targetsHelp = Help("Mover só entre alvos", "Com magnetismo ligado, o cursor espera um novo alvo\nantes de se mover. Ao olhar para um espaço vazio, ele\nfica parado.");
        var demoHelp = Help("Demonstração com mouse", "Use o mouse para ver quais controles seriam atraídos.\nO app só mostra o destaque.");
        var searchAreaHelp = Help("Área de busca e mira", "O círculo azul mostra o raio para encontrar novos alvos.\nA mira indica onde o app está buscando: no olhar ou\nno ponteiro, durante a demonstração com mouse.");
        var enterHelp = Help("Clicar no alvo verde com Enter", "Com o cursor sobre o alvo verde, Enter dá um clique\nesquerdo. Fora de um alvo fixado, Enter funciona como\nde costume. Funciona sem ativar o clique pelo olhar.");
        var magnetLine = Divider();
        magnetCard.Controls.AddRange([radiusField, dwellField, hysteresisField, widthField, heightField, stabilityField,
            magnetLine, magnetic, magnetHelp, targetsOnly, targetsHelp, demo, demoHelp, showSearchArea, searchAreaHelp,
            enterClicksTarget, enterHelp]);

        var modeField = new DashboardField("Modo", clickMode, Help("Modo", "Desligado mantém o clique manual. Treino mostra a\ncontagem sem clicar. Clique esquerdo clica ao manter o\nolhar no alvo."));
        var timeField = new DashboardField("Tempo para clicar (s)", clickTime, Help("Tempo para clicar (s)", "Depois que o alvo fica verde, mantenha-o dentro do\ncírculo pelo tempo escolhido. Se ele sair do círculo\nou você começar a trocar de alvo, a contagem zera."));
        var clickLine = Divider();
        var clickIcon = new DashboardIcon(DashboardGlyph.Info, DashboardStyle.Muted);
        clickCard.Controls.AddRange([modeField, timeField, clickLine, clickIcon, clickStatus]);

        var cursorLine = Divider();
        var calibrationHelp = Help("Calibração", "Marque depois de calibrar o olhar no GazePointer e\ndesligar o botão Control Mouse Cursor nele. Assim, só\no GazeMagnet controla o cursor pelo olhar.");
        cursorCard.Controls.AddRange([confirmed, calibrationHelp, arm, emergency, cursorLine, cursorIcon, controlStatus]);

        void UpdateButtonHelp()
        {
            buttonHelp.SetToolTip(connect, connect.Text switch
            {
                "Conectar" => "Conecta ao GazePointer para receber as coordenadas do olhar.",
                "Cancelar reconexão" => "Interrompe as tentativas e mantém o cursor manual.",
                _ => "Encerra a conexão com o GazePointer e devolve o cursor ao mouse."
            });
            buttonHelp.SetToolTip(arm, arm.Text switch
            {
                "Assumir cursor" => "Depois de confirmar a calibração, permite mover o cursor com o olhar.",
                "Cancelar retomada" => "Mantém o cursor manual quando a API voltar.",
                _ => "Pausa o controle pelo olhar e devolve o cursor ao mouse."
            });
        }

        connect.TextChanged += (_, _) => UpdateButtonHelp();
        arm.TextChanged += (_, _) => UpdateButtonHelp();
        UpdateButtonHelp();
        buttonHelp.SetToolTip(emergency, "Interrompe o controle e o clique pelo olhar. Atalho Ctrl + Alt + F10.");
        Deactivate += (_, _) => fieldHelp.Hide(this);
        Disposed += (_, _) =>
        {
            fieldHelp.Dispose();
            buttonHelp.Dispose();
        };
        footer.Controls.AddRange(footerDividers);
        footer.Controls.AddRange([footerStateIcon, cursorState, footerCursorIcon, cursorSource, footerApiIcon,
            footerApi, footerApproachIcon, footerApproach, footerLockedIcon, footerLocked]);

        viewport.Resize += (_, _) => Arrange();
        DpiChanged += (_, _) => BeginInvoke(Arrange);
        Load += (_, _) =>
        {
            Size area = Screen.FromControl(this).WorkingArea.Size;
            if (Width > area.Width || Height > area.Height)
                Size = new Size(Math.Min(Width, area.Width), Math.Min(Height, area.Height));
            Arrange();
        };
        Shown += (_, _) => Arrange();

        RefreshDashboard();
        Arrange();

        void Arrange()
        {
            if (arrangingDashboard || viewport.ClientSize.Width <= 0)
                return;
            arrangingDashboard = true;
            viewport.SuspendLayout();
            try
            {
                int width = (int)Math.Floor(viewport.ClientSize.Width * 96d / DeviceDpi);
                bool wide = width >= 970;
                int height = LayoutHeight(wide, width);
                if (Px(height) > viewport.ClientSize.Height)
                {
                    int scrollbar = Math.Max(16, SystemInformation.VerticalScrollBarWidth * 96 / DeviceDpi);
                    width = Math.Min(width, (int)Math.Floor(viewport.Width * 96d / DeviceDpi) - scrollbar);
                    wide = width >= 970;
                    height = LayoutHeight(wide, width);
                }

                int edge = wide ? 20 : 14;
                const int gap = 14;
                int contentWidth = width - edge * 2;
                int leftWidth = wide ? (contentWidth - gap) * 47 / 100 : contentWidth;
                int rightWidth = wide ? contentWidth - gap - leftWidth : contentWidth;
                int rightX = wide ? edge + leftWidth + gap : edge;
                int top = wide ? 109 : 145;
                int topHeight = MagnetHeight(rightWidth);
                const int bottomHeight = 210;
                int connectionHeight = wide ? topHeight : 264;
                int magnetY = wide ? top : top + connectionHeight + gap;
                int clickY = magnetY + topHeight + gap;
                int cursorY = wide ? clickY : clickY + bottomHeight + gap;
                bool stackedActions = rightWidth < 465;
                int cursorHeight = bottomHeight + (stackedActions ? 56 : 0);
                int footerY = cursorY + cursorHeight + 17;

                int footerHeight = width >= 1080 ? 54 : 98;
                height = Math.Max(height, (int)Math.Floor(viewport.ClientSize.Height * 96d / DeviceDpi));
                footerY = Math.Max(footerY, height - footerHeight);
                dashboardPage.Size = new Size(Px(width), Px(height));
                viewport.AutoScrollMinSize = dashboardPage.Size;
                Place(header, 0, 0, width, top);
                Place(brand, 101, 17, Math.Min(338, width - 110), 53);
                Place(platform, 103, 67, Math.Min(310, width - 110), 23);
                int apiX = wide ? 464 : 28;
                int apiY = wide ? 29 : 103;
                Place(apiDot, apiX, apiY + 4, 20, 20);
                Place(apiHeadline, apiX + 29, apiY, Math.Min(440, width - apiX - 39), 23);
                Place(apiDetail, apiX + 29, apiY + 23, Math.Min(460, width - apiX - 39), 22);

                Place(connectionCard, edge, top, leftWidth, connectionHeight);
                Place(magnetCard, rightX, magnetY, rightWidth, topHeight);
                Place(clickCard, edge, clickY, leftWidth, bottomHeight);
                Place(cursorCard, rightX, cursorY, rightWidth, cursorHeight);
                Place(footer, 0, footerY, width, height - footerY);

                const int inset = 22;
                int left = leftWidth - inset * 2;
                int right = rightWidth - inset * 2;
                int portWidth = (left - gap) * 38 / 100;
                Place(autoConnection, inset, 72, left, 25);
                Place(portField, inset, 103, portWidth, 54);
                Place(keyField, inset + portWidth + gap, 103, left - portWidth - gap, 54);
                Place(connect, inset, 164, left, 43);
                Place(connectionLine, inset, 218, left, 1);
                StatusRow(connectionIcon, connectionStatus, inset, 222, left, 40);

                DashboardField[] magnetFields = [radiusField, dwellField, hysteresisField, widthField, heightField, stabilityField];
                int columns = MagnetColumns(rightWidth);
                int column = (right - (columns - 1) * gap) / columns;
                for (int i = 0; i < magnetFields.Length; i++)
                {
                    int x = i % columns * (column + gap);
                    Place(magnetFields[i], inset + x, 73 + i / columns * 57,
                        i % columns == columns - 1 ? right - x : column, 52);
                }

                int switchY = columns == 2 ? 257 : 200;
                Place(magnetLine, inset, switchY - 6, right, 1);
                PlaceSwitch(magnetic, magnetHelp, inset, switchY, right, 24);
                PlaceSwitch(targetsOnly, targetsHelp, inset, switchY + 24, right, 24);
                int demoHeight = rightWidth < 450 ? 38 : 24;
                PlaceSwitch(demo, demoHelp, inset, switchY + 48, right, demoHeight);
                PlaceSwitch(showSearchArea, searchAreaHelp, inset, switchY + 48 + demoHeight, right, 24);
                PlaceSwitch(enterClicksTarget, enterHelp, inset, switchY + 72 + demoHeight, right, 24);
                int half = (left - gap) * (left < 365 ? 43 : 50) / 100;
                Place(modeField, inset, 76, half, 53);
                Place(timeField, inset + half + gap, 76, left - half - gap, 53);
                Place(clickLine, inset, 145, left, 1);
                StatusRow(clickIcon, clickStatus, inset, 151, left, 49);

                PlaceSwitch(confirmed, calibrationHelp, inset, 69, right, 38);
                int armWidth = stackedActions ? right : (right - gap) * 46 / 100;
                Place(arm, inset, 112, armWidth, 49);
                Place(emergency, stackedActions ? inset : inset + armWidth + gap, stackedActions ? 168 : 112,
                    stackedActions ? right : right - armWidth - gap, 49);
                int lineY = stackedActions ? 228 : 172;
                Place(cursorLine, inset, lineY, right, 1);
                StatusRow(cursorIcon, controlStatus, inset, lineY + 4, right, 34);
                ArrangeFooter(width);
            }
            finally
            {
                viewport.ResumeLayout();
                viewport.PerformLayout();
                arrangingDashboard = false;
            }
        }
    }

    private static int LayoutHeight(bool wide, int width)
    {
        int contentWidth = width - (wide ? 40 : 28);
        int right = wide ? contentWidth - 14 - (contentWidth - 14) * 47 / 100 : contentWidth;
        int cursorHeight = right < 465 ? 266 : 210;
        int magnetHeight = MagnetHeight(right);
        int footerHeight = width >= 1080 ? 54 : 98;
        return wide ? 109 + magnetHeight + 14 + cursorHeight + 17 + footerHeight
            : 145 + 264 + 14 + magnetHeight + 14 + 210 + 14 + cursorHeight + 17 + footerHeight;
    }

    private static int MagnetColumns(int cardWidth) => cardWidth < 452 ? 2 : 3;

    private static int MagnetHeight(int cardWidth) =>
        324 + (MagnetColumns(cardWidth) == 2 ? 57 : 0) + (cardWidth < 450 ? 14 : 0);

    private void PlaceSwitch(CheckBox toggle, DashboardHelpIcon help, int x, int y, int width, int height)
    {
        int textWidth = TextRenderer.MeasureText(toggle.Text, toggle.Font, Size.Empty, TextFormatFlags.NoPadding).Width;
        int helpX = Math.Min(width - 20, 54 + (int)Math.Ceiling(textWidth * 96d / DeviceDpi));
        Place(toggle, x, y, helpX, height);
        Place(help, x + helpX, y + (height - 18) / 2, 18, 18);
    }

    private DashboardHelpIcon Help(string subject, string explanation)
    {
        var help = new DashboardHelpIcon(subject, explanation);
        help.MouseHover += (_, _) =>
        {
            var anchor = PointToClient(help.PointToScreen(new Point(help.Width + Px(8), help.Height + Px(8))));
            fieldHelp.Show(explanation, this, anchor);
        };
        help.MouseLeave += (_, _) => fieldHelp.Hide(this);
        return help;
    }

    private void ArrangeFooter(int width)
    {
        bool wide = width >= 1080;
        foreach (var divider in footerDividers)
            divider.Visible = wide;

        if (wide)
        {
            int usable = width - 40;
            int first = usable * 12 / 100;
            int second = usable * 15 / 100;
            int third = usable * 23 / 100;
            int fourth = usable * 26 / 100;
            int x = 20;
            StatusRow(footerStateIcon, cursorState, x, 9, first, 35);
            x += first;
            Place(footerDividers[0], x - 8, 16, 1, 22);
            StatusRow(footerCursorIcon, cursorSource, x, 9, second, 35);
            x += second;
            Place(footerDividers[1], x - 8, 16, 1, 22);
            StatusRow(footerApiIcon, footerApi, x, 9, third, 35);
            x += third;
            Place(footerDividers[2], x - 8, 16, 1, 22);
            StatusRow(footerApproachIcon, footerApproach, x, 9, fourth, 35);
            x += fourth;
            Place(footerDividers[3], x - 8, 16, 1, 22);
            StatusRow(footerLockedIcon, footerLocked, x, 9, width - x - 13, 35);
            return;
        }

        int half = (width - 34) / 2;
        StatusRow(footerStateIcon, cursorState, 14, 6, half, 26);
        StatusRow(footerCursorIcon, cursorSource, 20 + half, 6, half, 26);
        StatusRow(footerApiIcon, footerApi, 14, 34, width - 30, 26);
        StatusRow(footerApproachIcon, footerApproach, 14, 62, half, 31);
        StatusRow(footerLockedIcon, footerLocked, 20 + half, 62, half, 31);
    }

    private void StatusRow(DashboardIcon icon, Label text, int x, int y, int width, int height)
    {
        Place(icon, x, y + (height - 18) / 2, 18, 18);
        Place(text, x + 26, y, width - 28, height);
    }

    private int Px(int logical) => (int)Math.Round(logical * DeviceDpi / 96d);

    private void Place(Control control, int x, int y, int width, int height) =>
        control.SetBounds(Px(x), Px(y), Math.Max(0, Px(width)), Math.Max(0, Px(height)));

    private void RefreshDashboard()
    {
        long now = Environment.TickCount64;
        GazeSample? sample = authorized ? ValidGaze(now) : null;
        bool receiving = sample != null;
        Color signal = receiving ? DashboardStyle.Green
            : connection != null ? DashboardStyle.Yellow : DashboardStyle.Muted;
        apiDot.Tint = connectionIcon.Tint = footerApiIcon.Tint = signal;
        connectionIcon.SetGlyph(receiving ? DashboardGlyph.Check : DashboardGlyph.Info);
        apiHeadline.Text = authorized ? "API conectada" : reconnecting ? "Reconectando à API"
            : connection != null ? "Conectando à API" : "API desconectada";
        apiDetail.Text = receiving ? "Recebendo coordenadas do olhar."
            : authorized ? "Aguardando coordenadas válidas."
            : connection != null ? "Aguardando o GazePointer." : "Conecte-se para começar.";
        footerApi.Text = receiving ? "Rastreamento conectado"
            : authorized ? "Aguardando sinal" : reconnecting ? "Reconectando…"
            : connection != null ? "Conectando…" : "API desconectada";
        bool waiting = resumeAfterReconnect || (armed && controlStatus.Text == WaitingForGazeMessage);
        bool cursorActive = armed && !waiting && !manualMovement &&
            now >= holdUntil && sample?.ReceivedAt >= holdUntil;
        cursorState.Text = demo.Checked ? "Demonstração" : waiting ? "Aguardando" : cursorActive ? "Ativo" : "Pausado";
        cursorSource.Text = cursorActive ? "Cursor pelo olhar" : "Cursor manual";
        DashboardGlyph stateGlyph = cursorActive ? DashboardGlyph.Check
            : waiting ? DashboardGlyph.Dot : DashboardGlyph.Pause;
        cursorIcon.SetGlyph(stateGlyph);
        footerStateIcon.SetGlyph(stateGlyph);
        cursorIcon.Tint = footerCursorIcon.Tint = footerStateIcon.Tint =
            waiting ? DashboardStyle.Yellow : cursorActive ? DashboardStyle.Green : DashboardStyle.Muted;
    }

    private void ShowTargetStatus(bool hasCandidate, bool locked)
    {
        footerApproach.ForeColor = hasCandidate ? DashboardStyle.Yellow : DashboardStyle.Muted;
        footerLocked.ForeColor = locked ? DashboardStyle.Green : DashboardStyle.Muted;
    }

    private static void StyleChoiceField(ComboBox field)
    {
        field.FlatStyle = FlatStyle.Flat;
        field.DrawMode = DrawMode.OwnerDrawFixed;
        field.DrawItem += (_, e) =>
        {
            bool selected = (e.State & DrawItemState.Selected) != 0 && (e.State & DrawItemState.ComboBoxEdit) == 0;
            using var background = new SolidBrush(selected ? Color.FromArgb(18, 90, 87) : DashboardStyle.Input);
            e.Graphics.FillRectangle(background, e.Bounds);
            if (e.Index < 0)
                return;
            var bounds = Rectangle.Inflate(e.Bounds, -4, 0);
            TextRenderer.DrawText(e.Graphics, field.GetItemText(field.Items[e.Index]), field.Font, bounds,
                DashboardStyle.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            if ((e.State & DrawItemState.Focus) != 0 && (e.State & DrawItemState.ComboBoxEdit) == 0)
                e.DrawFocusRectangle();
        };
    }

    private static Panel Divider() => new() { BackColor = DashboardStyle.Border };

    private static Label DashboardText(string text, Font font, Color? color = null) => new()
    {
        Text = text, Font = font, ForeColor = color ?? DashboardStyle.Text, BackColor = Color.Transparent,
        TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
    };

    private static void SetStatusLabel(Label label, string message)
    {
        label.Text = message;
        label.Font = DashboardStyle.Small;
        label.ForeColor = DashboardStyle.Muted;
        label.BackColor = Color.Transparent;
        label.TextAlign = ContentAlignment.MiddleLeft;
    }

    private sealed class DashboardCard : Panel
    {
        private readonly DashboardIcon icon;
        private readonly Label heading;
        private readonly Label description;

        public DashboardCard(string name, string subtitle, DashboardGlyph glyph)
        {
            BackColor = DashboardStyle.Card;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            icon = new DashboardIcon(glyph, DashboardStyle.Accent);
            heading = DashboardText(name, DashboardStyle.Title);
            description = DashboardText(subtitle, DashboardStyle.Small, DashboardStyle.Muted);
            description.AutoEllipsis = false;
            Controls.AddRange([icon, heading, description]);
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            int Scale(int value) => (int)Math.Round(value * DeviceDpi / 96d);
            icon.SetBounds(Scale(22), Scale(19), Scale(30), Scale(30));
            int textWidth = Math.Max(0, Width - Scale(82));
            heading.SetBounds(Scale(65), Scale(12), textWidth, Scale(31));
            description.SetBounds(Scale(65), Scale(42), textWidth, Scale(30));
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(DashboardStyle.Surface);
            if (Width < 4 || Height < 4)
                return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var background = new SolidBrush(DashboardStyle.Card);
            using var border = new Pen(DashboardStyle.Border);
            DashboardStyle.DrawRounded(e.Graphics, new RectangleF(0.5f, 0.5f, Width - 1, Height - 1),
                10 * DeviceDpi / 96f, background, border);
        }
    }

    private sealed class DashboardHeader : Panel
    {
        public DashboardHeader()
        {
            BackColor = DashboardStyle.Surface;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(DashboardStyle.Surface);
            if (Width < 4 || Height < 4)
                return;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (Width * 96d / DeviceDpi >= 970)
            {
                using var ribbon = new LinearGradientBrush(ClientRectangle, DashboardStyle.Surface, Color.FromArgb(12, 79, 77), 0f);
                g.FillPolygon(ribbon, [new PointF(Width * .61f, Height), new(Width * .89f, 0), new(Width, 0), new(Width, Height)]);
                using var stripe = new SolidBrush(Color.FromArgb(42, DashboardStyle.Accent));
                g.FillPolygon(stripe, [new PointF(Width * .77f, Height), new(Width, Height * .19f), new(Width, Height * .47f), new(Width * .85f, Height)]);
            }

            float scale = DeviceDpi / 96f;
            var state = g.Save();
            g.ScaleTransform(scale, scale);
            using var outer = new Pen(DashboardStyle.Accent, 5);
            using var inner = new Pen(Color.FromArgb(15, 153, 144), 4);
            using var core = new SolidBrush(Color.FromArgb(181, 255, 245));
            g.DrawEllipse(outer, 29, 24, 52, 52);
            g.DrawEllipse(inner, 38, 33, 34, 34);
            g.FillEllipse(core, 48, 43, 14, 14);
            using var divider = new Pen(DashboardStyle.Border);
            g.DrawLine(divider, 90, 24, 90, 79);
            if (Width / scale >= 970)
                g.DrawLine(divider, 432, 25, 432, 79);
            g.Restore(state);
        }
    }

    private sealed class DashboardField : Panel
    {
        private readonly Label label;
        private readonly Control input;

        private readonly DashboardHelpIcon? help;

        public DashboardField(string name, Control control, DashboardHelpIcon? help = null)
        {
            input = control;
            this.help = help;
            label = DashboardText(name, DashboardStyle.Small);
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            input.AccessibleName = name;
            input.Font = DashboardStyle.Body;
            input.BackColor = DashboardStyle.Input;
            input.ForeColor = DashboardStyle.Text;
            if (input is TextBox box)
                box.BorderStyle = BorderStyle.None;
            if (input is NumericUpDown number)
            {
                number.BorderStyle = BorderStyle.None;
                var buttons = number.Controls[0];
                buttons.Paint += (_, e) => PaintSpinner(buttons, e.Graphics, number.Enabled);
            }
            input.Enter += (_, _) => Invalidate();
            input.Leave += (_, _) => Invalidate();
            input.EnabledChanged += (_, _) => Invalidate(true);
            Controls.Add(label);
            if (help != null)
                Controls.Add(help);
            Controls.Add(input);
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            int Scale(int value) => (int)Math.Round(value * DeviceDpi / 96d);
            int labelWidth = Width;
            if (help != null)
            {
                int textWidth = TextRenderer.MeasureText(label.Text, label.Font).Width;
                int helpX = Math.Max(0, Math.Min(textWidth + Scale(2), Width - Scale(18)));
                help.SetBounds(helpX, Scale(1), Scale(18), Scale(18));
                labelWidth = helpX;
            }
            label.SetBounds(0, 0, labelWidth, Scale(20));
            int fieldY = Scale(22);
            int inset = Scale(input is ComboBox ? 5 : 8);
            int height = input.GetPreferredSize(Size.Empty).Height;
            input.SetBounds(inset, fieldY + Math.Max(0, (Height - fieldY - height) / 2),
                Math.Max(0, Width - 2 * inset), height);
            if (input is ComboBox combo)
                combo.ItemHeight = Scale(21);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = DeviceDpi / 96f;
            using var fill = new SolidBrush(DashboardStyle.Input);
            using var border = new Pen(input.ContainsFocus ? DashboardStyle.Accent : DashboardStyle.Border);
            DashboardStyle.DrawRounded(e.Graphics, new RectangleF(0.5f, 22 * scale, Width - 1, Height - 22 * scale - 1),
                6 * scale, fill, border);
        }

        private static void PaintSpinner(Control buttons, Graphics g, bool enabled)
        {
            Rectangle rect = buttons.ClientRectangle;
            if (rect.Width <= 0 || rect.Height <= 0)
                return;
            g.Clear(DashboardStyle.Input);
            using var line = new Pen(DashboardStyle.Border);
            g.DrawLine(line, 0, 0, 0, rect.Height);
            using var arrow = new SolidBrush(enabled ? DashboardStyle.Text : DashboardStyle.Muted);
            float x = rect.Width / 2f;
            float y = rect.Height / 4f;
            float w = buttons.DeviceDpi / 96f * 3;
            g.FillPolygon(arrow, [new PointF(x - w, y + w / 2), new(x + w, y + w / 2), new(x, y - w / 2)]);
            y *= 3;
            g.FillPolygon(arrow, [new PointF(x - w, y - w / 2), new(x + w, y - w / 2), new(x, y + w / 2)]);
        }
    }
}
