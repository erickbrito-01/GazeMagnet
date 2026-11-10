using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace GazeMagnet;

internal static class DashboardStyle
{
    public static readonly Color Surface = Color.FromArgb(17, 25, 30);
    public static readonly Color Card = Color.FromArgb(30, 40, 47);
    public static readonly Color Border = Color.FromArgb(61, 76, 85);
    public static readonly Color Input = Color.FromArgb(38, 49, 56);
    public static readonly Color Text = Color.FromArgb(243, 248, 251);
    public static readonly Color Muted = Color.FromArgb(188, 204, 215);
    public static readonly Color Accent = Color.FromArgb(53, 229, 212);
    public static readonly Color Green = Color.FromArgb(106, 237, 173);
    public static readonly Color Yellow = Color.FromArgb(249, 208, 91);
    public static readonly Color Red = Color.FromArgb(255, 115, 121);

    public static readonly Font Body = new("Segoe UI Variable Text", 11.25f);
    public static readonly Font Small = new("Segoe UI Variable Text", 10.25f);
    public static readonly Font Action = new("Segoe UI Variable Text", 11.5f, FontStyle.Bold);
    public static readonly Font Title = new("Segoe UI Variable Display", 16f, FontStyle.Bold);
    public static readonly Font Brand = new("Segoe UI Variable Display", 32f, FontStyle.Bold);

    public static void DrawRounded(Graphics g, RectangleF bounds, float radius, Brush fill, Pen? border = null)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        float diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        using var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        g.FillPath(fill, path);
        if (border != null)
            g.DrawPath(border, path);
    }

    public static void SetWindowColors(nint window)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000) || SystemInformation.HighContrast)
            return;

        int dark = 1;
        int caption = ColorTranslator.ToWin32(Card);
        int text = ColorTranslator.ToWin32(Text);
        DwmSetWindowAttribute(window, 20, ref dark, sizeof(int));
        DwmSetWindowAttribute(window, 35, ref caption, sizeof(int));
        DwmSetWindowAttribute(window, 36, ref text, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);
}

internal enum DashboardGlyph { Link, Magnet, Mouse, Cursor, Alert, Info, Pause, Check, Dot, Approach, Fixed }

internal sealed class DashboardIcon : Control
{
    private DashboardGlyph glyph;
    private Color tint;

    public DashboardIcon(DashboardGlyph glyph, Color tint)
    {
        this.glyph = glyph;
        this.tint = tint;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        TabStop = false;
    }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal Color Tint
    {
        get => tint;
        set
        {
            if (tint == value)
                return;
            tint = value;
            Invalidate();
        }
    }

    internal void SetGlyph(DashboardGlyph value)
    {
        if (glyph == value)
            return;
        glyph = value;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e) =>
        Draw(e.Graphics, glyph, ClientRectangle, tint);

    public static void Draw(Graphics g, DashboardGlyph glyph, RectangleF bounds, Color tint)
    {
        float size = Math.Min(bounds.Width, bounds.Height);
        if (size <= 0)
            return;

        var state = g.Save();
        g.TranslateTransform(bounds.X + (bounds.Width - size) / 2, bounds.Y + (bounds.Height - size) / 2);
        g.ScaleTransform(size / 24, size / 24);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(tint, 1.85f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        using var brush = new SolidBrush(tint);
        switch (glyph)
        {
            case DashboardGlyph.Link:
                g.TranslateTransform(12, 12);
                g.RotateTransform(-45);
                g.TranslateTransform(-12, -12);
                g.DrawArc(pen, 2, 8, 8, 8, 270, -180);
                g.DrawArc(pen, 14, 8, 8, 8, 270, 180);
                g.DrawLine(pen, 6, 8, 10, 8);
                g.DrawLine(pen, 6, 16, 10, 16);
                g.DrawLine(pen, 14, 8, 18, 8);
                g.DrawLine(pen, 14, 16, 18, 16);
                g.DrawLine(pen, 8, 12, 16, 12);
                break;
            case DashboardGlyph.Magnet:
                g.DrawArc(pen, 4, 4, 16, 16, 0, 180);
                g.DrawArc(pen, 9, 9, 6, 6, 0, 180);
                g.DrawLines(pen, [new PointF(4, 12), new(4, 4), new(9, 4), new(9, 12)]);
                g.DrawLines(pen, [new PointF(15, 12), new(15, 4), new(20, 4), new(20, 12)]);
                g.DrawLine(pen, 4, 8, 9, 8);
                g.DrawLine(pen, 15, 8, 20, 8);
                break;
            case DashboardGlyph.Mouse:
                DashboardStyle.DrawRounded(g, new RectangleF(5, 2, 14, 20), 7, Brushes.Transparent, pen);
                g.DrawLine(pen, 12, 6, 12, 10);
                break;
            case DashboardGlyph.Cursor:
                g.DrawPolygon(pen, [new PointF(5, 2), new(20, 14), new(13, 14), new(16, 21),
                    new(12, 22), new(9, 15), new(5, 19)]);
                break;
            case DashboardGlyph.Alert:
                g.DrawPolygon(pen, [new PointF(12, 2), new(22, 20), new(2, 20)]);
                g.DrawLine(pen, 12, 8, 12, 13);
                g.FillEllipse(brush, 11, 15, 2, 2);
                break;
            case DashboardGlyph.Info:
            case DashboardGlyph.Pause:
            case DashboardGlyph.Check:
                g.DrawEllipse(pen, 2, 2, 20, 20);
                if (glyph == DashboardGlyph.Check)
                    g.DrawLines(pen, [new PointF(7, 12), new(10.5f, 15.5f), new(17, 8.5f)]);
                else if (glyph == DashboardGlyph.Pause)
                {
                    g.DrawLine(pen, 9, 8, 9, 16);
                    g.DrawLine(pen, 15, 8, 15, 16);
                }
                else
                {
                    g.FillEllipse(brush, 11, 6, 2, 2);
                    g.DrawLine(pen, 12, 11, 12, 17);
                }
                break;
            case DashboardGlyph.Dot:
                using (var halo = new SolidBrush(Color.FromArgb(38, tint)))
                    g.FillEllipse(halo, 1, 1, 22, 22);
                g.FillEllipse(brush, 5, 5, 14, 14);
                break;
            case DashboardGlyph.Approach:
            case DashboardGlyph.Fixed:
                if (glyph == DashboardGlyph.Approach)
                    pen.DashStyle = DashStyle.Dash;
                DashboardStyle.DrawRounded(g, new RectangleF(2, 5, 20, 14), 3, Brushes.Transparent, pen);
                break;
        }
        g.Restore(state);
    }
}

internal sealed class DashboardButton(DashboardGlyph glyph, bool emergency = false, string? hint = null) : Button
{
    private bool hovered;
    private bool pressed;

    protected override void OnCreateControl()
    {
        base.OnCreateControl();
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hovered = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hovered = false; pressed = false; Invalidate(); }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); pressed = e.Button == MouseButtons.Left; Invalidate(); }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); pressed = false; Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? DashboardStyle.Card);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float scale = DeviceDpi / 96f;
        var bounds = new Rectangle(1, 1, Width - 3, Height - 3);
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        Color fill = emergency ? Color.FromArgb(42, 41, 48) : Color.FromArgb(10, 140, 125);
        Color top = emergency ? Color.FromArgb(48, 42, 49) : Color.FromArgb(12, 166, 151);
        if (pressed)
            top = fill;
        else if (hovered)
            top = ControlPaint.Light(top, 0.10f);

        using var background = new LinearGradientBrush(bounds, top, fill, 90);
        using var border = new Pen(Focused ? DashboardStyle.Text : emergency ? DashboardStyle.Red : DashboardStyle.Accent,
            (Focused || emergency ? 2 : 1) * scale);
        DashboardStyle.DrawRounded(g, bounds, 8 * scale, background, border);

        int icon = (int)(20 * scale);
        int gap = (int)(9 * scale);
        var textSize = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPadding);
        int left = Math.Max((int)(9 * scale), (Width - icon - gap - textSize.Width) / 2);
        DashboardIcon.Draw(g, glyph, new Rectangle(left, (Height - icon) / 2, icon, icon),
            emergency ? DashboardStyle.Red : DashboardStyle.Text);
        var area = new Rectangle(left + icon + gap, 0, Math.Max(0, Width - left - icon - gap - 5), Height);
        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;
        if (hint == null)
        {
            TextRenderer.DrawText(g, Text, Font, area, DashboardStyle.Text, flags);
            return;
        }
        area.Height = Height / 2 + (int)(5 * scale);
        TextRenderer.DrawText(g, Text, Font, area, DashboardStyle.Text, flags);
        area.Y = Height / 2 - (int)(2 * scale);
        area.Height = Height - area.Y;
        TextRenderer.DrawText(g, hint, DashboardStyle.Small, area, DashboardStyle.Accent, flags);
    }
}

internal sealed class DashboardSwitch : CheckBox
{
    public DashboardSwitch()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Cursor = Cursors.Hand;
    }

    protected override void OnCheckedChanged(EventArgs e)
    {
        base.OnCheckedChanged(e);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? DashboardStyle.Card);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float scale = DeviceDpi / 96f;
        float y = (Height - 20 * scale) / 2;
        using var track = new SolidBrush(Checked && Enabled ? Color.FromArgb(13, 153, 137) : Color.FromArgb(85, 100, 109));
        using var thumb = new SolidBrush(Enabled ? DashboardStyle.Text : DashboardStyle.Muted);
        DashboardStyle.DrawRounded(g, new RectangleF(1, y, 40 * scale, 20 * scale), 10 * scale, track);
        g.FillEllipse(thumb, 1 + (Checked ? 22 : 3) * scale, y + 3 * scale, 14 * scale, 14 * scale);
        int start = (int)(50 * scale);
        var text = new Rectangle(start, 0, Math.Max(0, Width - start - 3), Height);
        TextRenderer.DrawText(g, Text, Font, text, Enabled ? DashboardStyle.Text : DashboardStyle.Muted,
            TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
        if (Focused && ShowFocusCues)
        {
            using var focus = new SolidBrush(DashboardStyle.Accent);
            g.FillEllipse(focus, 44 * scale, (Height - 4 * scale) / 2, 4 * scale, 4 * scale);
        }
    }
}

// Keep the native selection, keyboard and accessibility behavior and theme its closed frame.
internal sealed class DashboardComboBox : ComboBox
{
    protected override void WndProc(ref Message message)
    {
        const int paint = 0x000F;
        const int printClient = 0x0318;
        base.WndProc(ref message);
        if (message.Msg != paint && (message.Msg != printClient || message.WParam == 0))
            return;
        if (Width < 4 || Height < 4 || !IsHandleCreated)
            return;

        using var graphics = message.Msg == paint ? Graphics.FromHwnd(Handle) : Graphics.FromHdc(message.WParam);
        float scale = DeviceDpi / 96f;
        int buttonWidth = (int)Math.Round(19 * scale);
        using var input = new SolidBrush(DashboardStyle.Input);
        using var outline = new Pen(DashboardStyle.Input, Math.Max(2, 2 * scale));
        graphics.DrawRectangle(outline, 0, 0, Width - 1, Height - 1);
        graphics.FillRectangle(input, Width - buttonWidth, 0, buttonWidth, Height);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var arrow = new Pen(DashboardStyle.Text, Math.Max(1, 1.4f * scale))
        {
            StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round
        };
        float x = Width - buttonWidth / 2f;
        float y = Height / 2f;
        graphics.DrawLines(arrow, [new PointF(x - 4 * scale, y - 2 * scale), new(x, y + 2 * scale),
            new(x + 4 * scale, y - 2 * scale)]);
    }
}

internal sealed class DashboardHelpIcon : Control
{
    public DashboardHelpIcon(string subject, string explanation)
    {
        Text = "?";
        AccessibleName = $"Ajuda sobre {subject}";
        AccessibleDescription = explanation.Replace('\n', ' ');
        AccessibleRole = AccessibleRole.Graphic;
        TabStop = false;
        Cursor = Cursors.Help;        SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(DashboardStyle.Card);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        float scale = DeviceDpi / 96f;
        float size = 13 * scale;
        var ring = new RectangleF((Width - size) / 2, (Height - size) / 2, size, size);
        Color color = DashboardStyle.Muted;
        using var pen = new Pen(color, scale);
        e.Graphics.DrawEllipse(pen, ring);
        TextRenderer.DrawText(e.Graphics, Text, DashboardStyle.Small, ClientRectangle, color,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}
