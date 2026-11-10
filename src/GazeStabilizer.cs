namespace GazeMagnet;

internal enum GazeStability { Off, Light, Medium, Strong }

internal sealed class GazeStabilizer
{
    private const int MaxSampleGapMs = 450;
    private long? lastSampleAt;
    private Point previousRaw;
    private double x, y, velocityX, velocityY;

    private Point FilteredPoint => new((int)Math.Round(x), (int)Math.Round(y));

    public Point Update(Point raw, long sampleAt, GazeStability level)
    {
        if (level == GazeStability.Off)
            return raw;

        if (lastSampleAt is not long last || sampleAt - last > MaxSampleGapMs)
        {
            lastSampleAt = sampleAt;
            previousRaw = raw;
            x = raw.X;
            y = raw.Y;
            velocityX = velocityY = 0;
            return raw;
        }

        if (sampleAt <= last)
            return FilteredPoint;

        double seconds = (sampleAt - last) / 1000d;
        // One Euro: movimentos rápidos elevam a frequência de corte e reduzem o atraso.
        double velocityWeight = Weight(1, seconds);
        velocityX += velocityWeight * ((raw.X - previousRaw.X) / seconds - velocityX);
        velocityY += velocityWeight * ((raw.Y - previousRaw.Y) / seconds - velocityY);
        double speed = Math.Sqrt(velocityX * velocityX + velocityY * velocityY);
        var (minimumCutoff, speedFactor) = level switch
        {
            GazeStability.Light => (1.8, 0.019),
            GazeStability.Strong => (0.4, 0.011),
            _ => (0.8, 0.014)
        };
        double weight = Weight(minimumCutoff + speedFactor * speed, seconds);
        x += weight * (raw.X - x);
        y += weight * (raw.Y - y);
        previousRaw = raw;
        lastSampleAt = sampleAt;
        return FilteredPoint;
    }

    public void Reset() => lastSampleAt = null;

    private static double Weight(double cutoff, double seconds)
    {
        double rate = 2 * Math.PI * cutoff * seconds;
        return rate / (1 + rate);
    }
}
