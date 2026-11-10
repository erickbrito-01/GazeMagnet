namespace GazeMagnet;

internal enum GazeAvailability { Tracking, Waiting, Recovered, TimedOut }

internal sealed class GazeRecovery
{
    private const int WaitLimitMs = 5000;
    private const int StablePeriodMs = 150;
    private const int MaxSampleGapMs = 450;

    private long? lostAt;
    private long? validSince;
    private long lastValidSample;

    public GazeAvailability Update(long now, long? sampleAt)
    {
        if (lostAt == null)
        {
            if (sampleAt != null)
                return GazeAvailability.Tracking;
            lostAt = now;
        }

        if (now - lostAt.Value >= WaitLimitMs)
            return GazeAvailability.TimedOut;

        if (sampleAt is not long receivedAt)
        {
            validSince = null;
            return GazeAvailability.Waiting;
        }

        if (validSince == null || receivedAt - lastValidSample > MaxSampleGapMs)
            validSince = receivedAt;
        lastValidSample = receivedAt;

        if (receivedAt - validSince.Value < StablePeriodMs)
            return GazeAvailability.Waiting;

        Reset();
        return GazeAvailability.Recovered;
    }

    public void Reset()
    {
        lostAt = null;
        validSince = null;
    }
}
