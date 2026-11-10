using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace GazeMagnet;

internal sealed record TargetSizeLimits(int Width, int Height)
{
    public static TargetSizeLimits Default { get; } = new(1600, 160);

    public bool Includes(Rectangle bounds) => bounds.Width <= Width && bounds.Height <= Height;
}

internal sealed record TargetSnapshot(
    nint Window, long CapturedAt, TargetSizeLimits Limits, IReadOnlyList<Target> Targets);

internal sealed record ClickProbe(nint Window, Target Target, long StartedAt);
internal sealed record ClickProbeResult(ClickProbe Probe, bool IsMatch);

internal sealed class TargetScanner : IDisposable
{
    private readonly CancellationTokenSource stop = new();
    private readonly Thread worker;
    private TargetSnapshot? latest;
    private nint requestedWindow;
    private TargetSizeLimits sizeLimits = TargetSizeLimits.Default;
    private ClickProbe? requestedProbe;
    private ClickProbeResult? probeResult;

    public TargetScanner()
    {
        worker = new Thread(ScanLoop) { IsBackground = true, Name = "Leitura UI Automation" };
        worker.SetApartmentState(ApartmentState.MTA);
        worker.Start();
    }

    public TargetSnapshot? Latest => Volatile.Read(ref latest);

    public ClickProbeResult? ProbeResult => Volatile.Read(ref probeResult);

    public void VerifyClick(ClickProbe? probe) => Volatile.Write(ref requestedProbe, probe);

    public void SetWindow(nint window) => Volatile.Write(ref requestedWindow, window);

    public void SetSizeLimits(TargetSizeLimits limits) => Volatile.Write(ref sizeLimits, limits);

    private static readonly Condition Containers = new OrCondition(
        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document),
        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window),
        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Pane),
        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Group),
        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ToolBar),
        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuBar),
        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.StatusBar),
        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.List),
        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Tree),
        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.DataGrid),
        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Table),
        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Tab),
        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Menu));

    private static readonly Condition Clickable = new AndCondition(
        new PropertyCondition(AutomationElement.IsEnabledProperty, true),
        new PropertyCondition(AutomationElement.IsOffscreenProperty, false),
        new NotCondition(Containers),
        new OrCondition(
            new PropertyCondition(AutomationElement.IsInvokePatternAvailableProperty, true),
            new PropertyCondition(AutomationElement.IsTogglePatternAvailableProperty, true),
            new PropertyCondition(AutomationElement.IsSelectionItemPatternAvailableProperty, true),
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Hyperlink),
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuItem),
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ComboBox),
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit)));

    private void ScanLoop()
    {
        CancellationToken token = stop.Token;
        while (!token.IsCancellationRequested)
        {
            ClickProbe? probe = Interlocked.Exchange(ref requestedProbe, null);
            if (probe != null)
                Volatile.Write(ref probeResult, new ClickProbeResult(probe, TargetIsUnderPoint(probe)));

            nint window = Volatile.Read(ref requestedWindow);
            if (window == 0)
            {
                Volatile.Write(ref latest, null);
            }
            else
            {
                long startedAt = Environment.TickCount64;
                TargetSizeLimits limits = Volatile.Read(ref sizeLimits);
                var targets = ReadTargets(window, limits);
                if (!token.IsCancellationRequested && window == Volatile.Read(ref requestedWindow))
                    Volatile.Write(ref latest, new TargetSnapshot(window, startedAt, limits, targets));
            }

            token.WaitHandle.WaitOne(180);
        }
    }

    private static bool TargetIsUnderPoint(ClickProbe probe)
    {
        Point point = probe.Target.Center;
        if (Native.RootAt(point) != probe.Window)
            return false;

        try
        {
            var element = AutomationElement.FromPoint(new System.Windows.Point(point.X, point.Y));
            for (int level = 0; element != null && level < 12; level++)
            {
                if (string.Join(".", element.GetRuntimeId()) == probe.Target.Id)
                {
                    var current = element.Current;
                    return current.IsEnabled && !current.IsOffscreen &&
                        current.BoundingRectangle.Contains(point.X, point.Y) &&
                        Native.RootAt(point) == probe.Window;
                }
                if (element.FindFirst(TreeScope.Element, Clickable) != null)
                    return false;
                element = TreeWalker.RawViewWalker.GetParent(element);
            }
        }
        catch (Exception error) when (error is COMException or ElementNotAvailableException or
                                      InvalidOperationException or UnauthorizedAccessException or ArgumentException)
        {
        }
        return false;
    }

    private static List<Target> ReadTargets(nint window, TargetSizeLimits limits)
    {
        var result = new List<Target>();

        try
        {
            var root = AutomationElement.FromHandle(window);
            var cache = new CacheRequest { AutomationElementMode = AutomationElementMode.None };
            cache.Add(AutomationElement.BoundingRectangleProperty);
            cache.Add(AutomationElement.RuntimeIdProperty);
            using (cache.Activate())
            {
                foreach (AutomationElement element in root.FindAll(TreeScope.Descendants, Clickable))
                {
                    if (element.GetCachedPropertyValue(AutomationElement.BoundingRectangleProperty, true)
                        is not System.Windows.Rect bounds || bounds.IsEmpty ||
                        bounds.Width < 4 || bounds.Height < 4 ||
                        bounds.Width > 32000 || bounds.Height > 32000 ||
                        Math.Abs(bounds.X) > 100000 || Math.Abs(bounds.Y) > 100000 ||
                        element.GetCachedPropertyValue(AutomationElement.RuntimeIdProperty, true)
                        is not int[] id)
                        continue;

                    var rectangle = Rectangle.FromLTRB((int)Math.Ceiling(bounds.Left), (int)Math.Ceiling(bounds.Top),
                        (int)Math.Floor(bounds.Right), (int)Math.Floor(bounds.Bottom));
                    if (!limits.Includes(rectangle))
                        continue;

                    var target = new Target(string.Join(".", id), rectangle);
                    if (Native.RootAt(target.Center) == window)
                        result.Add(target);
                    if (result.Count >= 500)
                        break;
                }
            }
        }
        catch (Exception error) when (error is COMException or ElementNotAvailableException or
                                      InvalidOperationException or UnauthorizedAccessException or ArgumentException)
        {
            result.Clear();
        }

        return result;
    }

    public void Dispose()
    {
        stop.Cancel();
        if (worker.Join(80))
            stop.Dispose();
    }
}
