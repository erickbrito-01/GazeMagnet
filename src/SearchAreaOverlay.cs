namespace GazeMagnet;

internal sealed class SearchAreaOverlay : Form
{
    private const int PaddingPixels = 4;
    private static readonly Color Cutout = Color.Fuchsia;
    private Rectangle? displayedBounds;

    public SearchAreaOverlay()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        MinimumSize = Size.Empty;
        BackColor = Cutout;
        TransparencyKey = Cutout;
        DoubleBuffered = true;
        ResizeRedraw = true;
        AccessibleName = "Raio de busca e mira do GazeMagnet";
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var settings = base.CreateParams;
            settings.ExStyle |= Native.NoActivate | Native.Transparent | Native.ToolWindow;
            return settings;
        }
    }

    public void Display(Point center, int radius)
    {
        int extent = radius + PaddingPixels;
        int size = extent * 2 + 1;
        var bounds = new Rectangle(center.X - extent, center.Y - extent, size, size);
        if (displayedBounds == bounds)
            return;

        Native.ShowOutline(Handle, bounds);
        displayedBounds = bounds;
    }

    public void Clear()
    {
        if (displayedBounds == null)
            return;

        Native.HideOutline(Handle);
        displayedBounds = null;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var ring = new Rectangle(PaddingPixels, PaddingPixels,
            ClientSize.Width - PaddingPixels * 2 - 1, ClientSize.Height - PaddingPixels * 2 - 1);
        if (ring.Width <= 0 || ring.Height <= 0)
            return;

        Point center = new(ring.Left + ring.Width / 2, ring.Top + ring.Height / 2);
        int arm = Math.Min(8, ring.Width / 2 - 3);
        using var shadow = new Pen(Color.FromArgb(15, 23, 42), 4);
        using var line = new Pen(Color.DeepSkyBlue, 2);
        DrawGuide(shadow);
        DrawGuide(line);

        void DrawGuide(Pen pen)
        {
            e.Graphics.DrawEllipse(pen, ring);
            e.Graphics.DrawLine(pen, center.X - arm, center.Y, center.X + arm, center.Y);
            e.Graphics.DrawLine(pen, center.X, center.Y - arm, center.X, center.Y + arm);
        }
    }
}
