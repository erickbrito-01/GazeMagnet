namespace GazeMagnet;

internal sealed class TargetOverlay : Form
{
    private static readonly Color Cutout = Color.Fuchsia;
    private bool locked;
    private bool pulse;
    private Rectangle? displayedBounds;
    private ClickFillOverlay? clickFill;

    public TargetOverlay()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Cutout;
        TransparencyKey = Cutout;
        DoubleBuffered = true;
        AccessibleName = "Destaque do alvo magnético";
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

    public void Display(Rectangle target, bool fixedOnTarget, double clickProgress = 0, bool clicked = false)
    {
        Rectangle outline = Rectangle.Inflate(target, 4, 4);
        if (displayedBounds != outline)
        {
            Native.ShowOutline(Handle, outline);
            displayedBounds = outline;
        }
        if (locked != fixedOnTarget || pulse != clicked)
        {
            locked = fixedOnTarget;
            pulse = clicked;
            Invalidate();
        }

        int fillWidth = fixedOnTarget
            ? (int)Math.Round(target.Width * (clicked ? 1 : Math.Clamp(clickProgress, 0, 1)))
            : 0;
        if (fillWidth > 0)
        {
            clickFill ??= new ClickFillOverlay();
            clickFill.Display(new Rectangle(target.X, target.Y, fillWidth, target.Height));
        }
        else
        {
            clickFill?.Clear();
        }
    }

    public void Clear()
    {
        clickFill?.Clear();
        if (displayedBounds == null)
            return;

        Native.HideOutline(Handle);
        displayedBounds = null;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var shadow = new Pen(Color.FromArgb(15, 23, 42), 5);
        using var line = new Pen(locked ? Color.MediumSpringGreen : Color.Gold, 3);
        if (!locked)
            line.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
        var border = new Rectangle(3, 3, Math.Max(1, Width - 7), Math.Max(1, Height - 7));
        e.Graphics.DrawRectangle(shadow, border);
        e.Graphics.DrawRectangle(line, border);
        if (locked && pulse)
        {
            using var flash = new Pen(Color.White, 3);
            e.Graphics.DrawRectangle(flash, border);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            clickFill?.Dispose();
        base.Dispose(disposing);
    }

    private sealed class ClickFillOverlay : Form
    {
        private Rectangle? displayedBounds;

        public ClickFillOverlay()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.MediumSpringGreen;
            Opacity = 0.22;
            ResizeRedraw = true;
            AccessibleName = "Preenchimento do tempo para clicar";
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

        public void Display(Rectangle bounds)
        {
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
    }
}
