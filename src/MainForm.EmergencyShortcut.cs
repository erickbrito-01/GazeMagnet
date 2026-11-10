namespace GazeMagnet;

internal sealed partial class MainForm
{
    private const int AlternateEmergencyId = 3;
    private EmergencyShortcut preferredEmergencyShortcut = EmergencyShortcut.Default;
    private EmergencyShortcut activeEmergencyShortcut = EmergencyShortcut.Default;
    private int emergencyShortcutId = Native.EmergencyId;
    private bool CanEditEmergencyShortcut => !armed && !resumeAfterReconnect;
    private string EmergencyKeyLabel => emergencyRegistered ? activeEmergencyShortcut.ToString() : "Atalho indisponível";
    private string ArmedMessage => $"Cursor pelo olhar. {EmergencyKeyLabel} devolve o controle manual.";
    private const string EmergencyUnavailable = "Atalho de emergência indisponível. Use Alterar atalho.";

    private bool RegisterEmergencyShortcut(EmergencyShortcut shortcut, int id) =>
        Native.RegisterShortcut(Handle, id, shortcut.Key, shortcut.WindowsModifiers());

    private void RestoreEmergencyRegistration()
    {
        emergencyShortcutId = Native.EmergencyId;
        activeEmergencyShortcut = preferredEmergencyShortcut;
        emergencyRegistered = RegisterEmergencyShortcut(activeEmergencyShortcut, emergencyShortcutId);
        if (!emergencyRegistered && preferredEmergencyShortcut != EmergencyShortcut.Default)
        {
            activeEmergencyShortcut = EmergencyShortcut.Default;
            emergencyRegistered = RegisterEmergencyShortcut(activeEmergencyShortcut, emergencyShortcutId);
        }
        RefreshEmergencyShortcutDisplay();
    }

    private string? ApplyEmergencyShortcut(EmergencyShortcut shortcut)
    {
        if (!CanEditEmergencyShortcut)
            return "Pause o controle antes de alterar o atalho.";
        if (!shortcut.IsAllowed())
            return "Combinação indisponível. Confira as regras abaixo.";

        if (!emergencyRegistered || shortcut != activeEmergencyShortcut)
        {
            int nextId = emergencyShortcutId == Native.EmergencyId ? AlternateEmergencyId : Native.EmergencyId;
            if (!RegisterEmergencyShortcut(shortcut, nextId))
                return "O Windows não aceitou essa combinação. Escolha outra.";
            if (emergencyRegistered && !Native.UnregisterShortcut(Handle, emergencyShortcutId))
            {
                Native.UnregisterShortcut(Handle, nextId);
                return "Não foi possível concluir a troca. Tente novamente.";
            }

            emergencyShortcutId = nextId;
            activeEmergencyShortcut = shortcut;
            emergencyRegistered = true;
        }

        preferredEmergencyShortcut = shortcut;
        RefreshEmergencyShortcutDisplay();
        SaveSettings();
        return null;
    }

    private void RefreshEmergencyShortcutDisplay()
    {
        emergency.SetHint(EmergencyKeyLabel);
        emergency.AccessibleDescription = $"Interrompe o controle do GazeMagnet. {EmergencyKeyLabel}.";
        buttonHelp.SetToolTip(emergency, emergency.AccessibleDescription);
        emergencyShortcutStatus.Text = !emergencyRegistered ? EmergencyUnavailable :
            activeEmergencyShortcut != preferredEmergencyShortcut
                ? $"Atalho salvo indisponível. Em uso temporário: {EmergencyKeyLabel}."
                : $"Atalho de emergência: {EmergencyKeyLabel}.";
    }

    private void EditEmergencyShortcut()
    {
        if (!CanEditEmergencyShortcut)
            return;

        using var dialog = new Form
        {
            Text = "Alterar atalho de emergência", StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false,
            ShowInTaskbar = false, ClientSize = new Size(414, 245), AutoScaleMode = AutoScaleMode.Dpi,
            Font = DashboardStyle.Body
        };
        var ctrl = new CheckBox { Text = "Ctrl", AutoSize = true, Checked = preferredEmergencyShortcut.Ctrl };
        var alt = new CheckBox { Text = "Alt", AutoSize = true, Checked = preferredEmergencyShortcut.Alt };
        var shift = new CheckBox { Text = "Shift", AutoSize = true, Checked = preferredEmergencyShortcut.Shift };
        var keys = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100, AccessibleName = "Tecla final" };
        Keys[] choices = [.. Enumerable.Range((int)Keys.F1, 11).Select(value => (Keys)value),
            .. Enumerable.Range((int)Keys.A, 26).Select(value => (Keys)value),
            .. Enumerable.Range((int)Keys.D0, 10).Select(value => (Keys)value)];
        keys.Items.AddRange(choices.Select(EmergencyShortcut.KeyLabel).ToArray());
        keys.SelectedIndex = Array.IndexOf(choices, preferredEmergencyShortcut.Key);
        var inputs = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        inputs.Controls.AddRange([ctrl, alt, shift, keys]);
        var error = new Label { Dock = DockStyle.Fill, ForeColor = Color.Firebrick, AccessibleName = "Resultado da alteração" };
        var apply = new Button { Text = "Aplicar", AutoSize = true };
        var cancel = new Button { Text = "Cancelar", AutoSize = true, DialogResult = DialogResult.Cancel };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        buttons.Controls.AddRange([cancel, apply]);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(14) };
        foreach (int height in new[] { 27, 38, 64, 42, 42 })
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        layout.Controls.Add(new Label { Text = "Escolha os modificadores e a tecla final.", Dock = DockStyle.Fill }, 0, 0);
        layout.Controls.Add(inputs, 0, 1);
        layout.Controls.Add(new Label
        {
            Text = "F1 a F11 precisam de Ctrl ou Alt. Letras e números usam Ctrl + Shift ou Alt + Shift. Ctrl + Alt + F11 é reservado para o clique pelo olhar.",
            Dock = DockStyle.Fill
        }, 0, 2);
        layout.Controls.Add(error, 0, 3);
        layout.Controls.Add(buttons, 0, 4);
        dialog.Controls.Add(layout);
        dialog.AcceptButton = apply;
        dialog.CancelButton = cancel;
        apply.Click += (_, _) =>
        {
            string? message = ApplyEmergencyShortcut(new(choices[keys.SelectedIndex], ctrl.Checked, alt.Checked, shift.Checked));
            if (message == null)
                dialog.DialogResult = DialogResult.OK;
            else
                error.Text = message;
        };
        dialog.ShowDialog(this);
    }
}
