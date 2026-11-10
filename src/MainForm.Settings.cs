namespace GazeMagnet;

internal sealed partial class MainForm
{
    private void RestoreSettings()
    {
        AppSettings settings = AppSettings.Load(settingsPath);
        manualPort = Math.Clamp(settings.ManualPort ?? manualPort, (int)port.Minimum, (int)port.Maximum);
        manualKey = settings.ReadManualKey();
        autoConnection.Checked = settings.AutoConnection ?? autoConnection.Checked;

        RestoreNumber(radius, settings.Radius);
        RestoreNumber(dwell, settings.FixationMs);
        RestoreNumber(hysteresis, settings.Hysteresis);
        RestoreNumber(maxTargetWidth, settings.MaxTargetWidth);
        RestoreNumber(maxTargetHeight, settings.MaxTargetHeight);
        RestoreNumber(clickTime, settings.ClickTime);
        if (settings.GazeStability is int stability && stability >= 0 && stability < gazeStability.Items.Count)
            gazeStability.SelectedIndex = stability;
        if (settings.LastClickMode is (int)GazeClickMode.Practice or (int)GazeClickMode.LeftClick)
            lastClickMode = (GazeClickMode)settings.LastClickMode.Value;

        magnetic.Checked = settings.Magnetic ?? magnetic.Checked;
        targetsOnly.Checked = settings.TargetsOnly ?? targetsOnly.Checked;
        showSearchArea.Checked = settings.ShowSearchArea ?? showSearchArea.Checked;
        demo.Checked = settings.Demo ?? demo.Checked;
        enterClicksTarget.Checked = settings.EnterClicksTarget ?? enterClicksTarget.Checked;
    }

    private static void RestoreNumber(NumericUpDown field, decimal? value)
    {
        if (value is decimal saved)
            field.Value = Math.Clamp(saved, field.Minimum, field.Maximum);
    }

    private void SaveSettings()
    {
        bool automatic = autoConnection.Checked;
        new AppSettings
        {
            AutoConnection = automatic,
            ManualPort = automatic ? manualPort : (int)port.Value,
            ProtectedKey = AppSettings.ProtectManualKey(automatic ? manualKey : key.Text),
            Radius = (int)radius.Value,
            FixationMs = (int)dwell.Value,
            Hysteresis = (int)hysteresis.Value,
            MaxTargetWidth = (int)maxTargetWidth.Value,
            MaxTargetHeight = (int)maxTargetHeight.Value,
            GazeStability = gazeStability.SelectedIndex,
            Magnetic = magnetic.Checked,
            TargetsOnly = targetsOnly.Checked,
            Demo = demo.Checked,
            ShowSearchArea = showSearchArea.Checked,
            EnterClicksTarget = enterClicksTarget.Checked,
            ClickTime = clickTime.Value,
            LastClickMode = (int)lastClickMode
        }.Save(settingsPath);
    }
}
