namespace BotAimImprover;

internal readonly record struct HeardCue(AimLocation Position, float ReadyAt, float ExpiresAt)
{
    public bool Ready(float now) => Position.Finite && now >= ReadyAt && now <= ExpiresAt;
}

internal static class AwarenessPolicy
{
    public static float Distance(AimLocation a, AimLocation b) => MathF.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));
    public static bool Audible(AimLocation listener, AimLocation source, float radius, bool obstructed, bool step) =>
        listener.Finite && source.Finite && float.IsFinite(radius) && radius > 0 &&
        MathF.Abs(listener.Z-source.Z) <= (step ? 160 : 320) &&
        Distance(listener,source) <= Math.Min(radius, step ? 850 : 1800)*(obstructed ? 0.4f : 1f);
    public static AimLocation Approximate(AimLocation source, int listener, int sequence)
    {
        // Stable for this heard event, not refreshed from a live hidden enemy.
        var angle=((listener*137+sequence*73)%360)*MathF.PI/180;
        return new(MathF.Round(source.X/48)*48+MathF.Cos(angle)*32, MathF.Round(source.Y/48)*48+MathF.Sin(angle)*32, source.Z);
    }
    public static HeardCue Hear(AimLocation position, float now, bool step, HeardCue? pending) =>
        new(position, pending is {} old && now < old.ReadyAt ? old.ReadyAt : now+0.35f, now+(step ? 2.2f : 3f));
    public static bool Yields(int bot, int ally, bool allyHuman, bool allyBusy) => allyHuman || allyBusy || bot > ally;
    public static AimLocation Bearing(AimLocation eye, AimLocation heard) =>
        SmokeAimPolicy.Hold(-1,-2,null,eye,SmokeAimPolicy.Direction((heard.X-eye.X)*100,(heard.Y-eye.Y)*100,0));
    public static bool CanAssist(bool live, bool busy, bool controlled, bool grounded, bool visibleEnemy) =>
        live && !busy && !controlled && grounded && !visibleEnemy;
}
