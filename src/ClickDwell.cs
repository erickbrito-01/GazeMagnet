namespace GazeMagnet;

internal sealed class ClickDwell
{
    private const int RearmTimeMs = 150;
    private const int MaxSampleGapMs = 450;

    private Target? countingTarget;
    private long startedAt;
    private long lastSample;
    private Target? firedTarget;
    private long? awaySince;
    private long lastAwaySample;

    public double Update(Target? target, Point gaze, long sampleAt, int delayMs, int radius)
    {
        if (firedTarget != null)
        {
            if (firedTarget.DistanceTo(gaze) <= radius)
            {
                awaySince = null;
            }
            else
            {
                if (awaySince == null || sampleAt - lastAwaySample > MaxSampleGapMs)
                    awaySince = sampleAt;
                lastAwaySample = sampleAt;
                if (sampleAt - awaySince.Value >= RearmTimeMs)
                    firedTarget = null;
            }
            if (firedTarget != null)
                return 0;
        }

        if (target == null || target.DistanceTo(gaze) > radius)
        {
            Cancel();
            return 0;
        }
        if (target != countingTarget || sampleAt - lastSample > MaxSampleGapMs)
        {
            countingTarget = target;
            startedAt = sampleAt;
        }

        lastSample = sampleAt;
        return Math.Clamp((sampleAt - startedAt) / (double)delayMs, 0, 1);
    }

    public void Cancel()
    {
        countingTarget = null;
        awaySince = null;
    }

    public void Complete(Target target)
    {
        firedTarget = target;
        Cancel();
    }
}
