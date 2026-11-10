namespace GazeMagnet;

internal sealed record Target(string Id, Rectangle Bounds)
{
    public Point Center => new(Bounds.Left + Bounds.Width / 2, Bounds.Top + Bounds.Height / 2);

    public long Area => (long)Bounds.Width * Bounds.Height;

    public bool Encloses(Target other) => Bounds != other.Bounds && Bounds.Contains(other.Bounds);

    public double DistanceTo(Point point)
    {
        int x = Math.Max(0, Math.Max(Bounds.Left - point.X, point.X - Bounds.Right + 1));
        int y = Math.Max(0, Math.Max(Bounds.Top - point.Y, point.Y - Bounds.Bottom + 1));
        return Math.Sqrt((double)x * x + (double)y * y);
    }
}

internal sealed class Magnet
{
    private string? pendingId;
    private long pendingSince;

    public Target? LockedTarget { get; private set; }
    public bool IsSwitchingTarget => LockedTarget != null && pendingId != null;

    public void Reset()
    {
        LockedTarget = null;
        pendingId = null;
    }

    public (Target? Target, bool Locked) Update(
        IReadOnlyList<Target> targets, Point gaze, long now, int radius, int dwell, int hysteresis,
        bool targetsOnly = false)
    {
        Target? nearest = null;
        Target? locked = null;
        Target? pending = null;
        double nearestDistance = radius;
        long nearestCenterDistance = long.MaxValue;
        foreach (Target item in targets)
        {
            if (item.Id == LockedTarget?.Id)
                locked = item;
            if (item.Id == pendingId)
                pending = item;

            double distance = item.DistanceTo(gaze);
            long x = item.Center.X - (long)gaze.X;
            long y = item.Center.Y - (long)gaze.Y;
            long centerDistance = x * x + y * y;
            bool smallerUnderGaze = distance == 0 && nearest != null && item.Area < nearest.Area;
            bool compareCenters = distance != 0 || nearest == null || item.Area == nearest.Area;
            if (distance < nearestDistance ||
                (distance == nearestDistance && (smallerUnderGaze ||
                    (compareCenters && centerDistance < nearestCenterDistance))))
            {
                nearest = item;
                nearestDistance = distance;
                nearestCenterDistance = centerDistance;
            }
        }

        double lockedDistance = locked?.DistanceTo(gaze) ?? double.PositiveInfinity;
        if (lockedDistance > radius + hysteresis)
            locked = null;
        LockedTarget = locked;

        // Preserve a contagem dentro do raio. Voltar ao alvo fixado ou mirar
        // diretamente em um controle aninhado ainda permite mudar de escolha.
        if (nearest != null && pending != null && nearest.Id != pending.Id)
        {
            double pendingDistance = pending.DistanceTo(gaze);
            bool aimedAtRelatedTarget = nearestDistance == 0 && (pending.Encloses(nearest) ||
                (pendingDistance > 0 && (nearest.Id == locked?.Id || nearest.Encloses(pending))));
            if (pendingDistance <= radius && !aimedAtRelatedTarget)
            {
                nearest = pending;
                nearestDistance = pendingDistance;
            }
        }

        // Ao olhar para um controle dentro do alvo preso, a fixação já filtra
        // oscilações. Exigir vantagem de distância aqui impediria a troca.
        bool aimedAtNestedControl = nearestDistance == 0 && nearest != null &&
            locked?.Encloses(nearest) == true;
        bool aimedAtNeighbor = targetsOnly && nearestDistance == 0 && lockedDistance > 0;
        bool hasChallenger = nearest != null && nearest.Id != locked?.Id &&
            (locked == null || nearest.Id == pendingId || aimedAtNestedControl || aimedAtNeighbor ||
                nearestDistance + hysteresis < lockedDistance);
        if (!hasChallenger)
        {
            pendingId = null;
            return (locked, locked != null);
        }

        if (pendingId != nearest!.Id)
        {
            pendingId = nearest.Id;
            pendingSince = now;
        }
        if (now - pendingSince >= dwell)
        {
            LockedTarget = nearest;
            pendingId = null;
            return (nearest, true);
        }

        return locked == null || targetsOnly ? (nearest, false) : (locked, true);
    }
}
