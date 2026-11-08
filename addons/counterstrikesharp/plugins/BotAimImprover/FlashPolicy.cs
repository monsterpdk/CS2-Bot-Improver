namespace BotAimImprover;

internal static class FlashPolicy
{
    internal static float NativeDeadline(float now,float hold,float alpha,float nativeUntil)
    {
        if(!float.IsFinite(now))return 0;
        var fromHold=float.IsFinite(alpha) && alpha>0 ? Deadline(now,hold) : 0;
        var fromNative=float.IsFinite(nativeUntil) && nativeUntil>now && nativeUntil-now<=10 ? nativeUntil : 0;
        return MathF.Max(fromHold,fromNative);
    }
    internal static float Deadline(float now,float duration)
        => float.IsFinite(now) && float.IsFinite(duration) && duration>0 ? now+MathF.Min(duration,10f) : 0;
    internal static bool Active(float now,float nativeUntil,float eventUntil)
        => float.IsFinite(now) && ((float.IsFinite(nativeUntil) && nativeUntil>now) ||
            (float.IsFinite(eventUntil) && eventUntil>now));
}
