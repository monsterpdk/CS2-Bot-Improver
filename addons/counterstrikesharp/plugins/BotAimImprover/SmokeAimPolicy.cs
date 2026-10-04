namespace BotAimImprover;

internal readonly record struct AimLocation(float X, float Y, float Z)
{
    public bool Finite => float.IsFinite(X) && float.IsFinite(Y) && float.IsFinite(Z);
}

internal static class SmokeAimPolicy
{
    // Conservative gameplay threshold: stricter than native Vision's 0.23.
    public const float Threshold = 0.10f;
    public static bool Concealed(float head, float chest) =>
        float.IsFinite(head) && float.IsFinite(chest) && Math.Max(head, chest) >= Threshold;
    public static AimLocation Direction(float vx, float vy, float yaw)
    {
        var speedSquared = vx * vx + vy * vy;
        if (float.IsFinite(speedSquared) && speedSquared >= 32f * 32f)
        {
            var length = MathF.Sqrt(speedSquared);
            return new(vx / length, vy / length, 0);
        }
        var radians = (float.IsFinite(yaw) ? yaw : 0) * MathF.PI / 180f;
        return new(MathF.Cos(radians), MathF.Sin(radians), 0);
    }

    public static float CorridorScore(float fraction, float angle) =>
        Math.Clamp(fraction, 0f, 1f) * 512f + MathF.Cos(angle * MathF.PI / 180f) * 64f;

    public static AimLocation Hold(int enemy, int rememberedEnemy, AimLocation? remembered, AimLocation eye, AimLocation direction)
    {
        if (enemy == rememberedEnemy && remembered is { Finite: true } point)
        {
            var dx = point.X - eye.X;
            var dy = point.Y - eye.Y;
            var horizontal = MathF.Sqrt(dx * dx + dy * dy);
            if (horizontal >= 96f)
            {
                // Last-seen points can end up beneath a moving bot. Blind fire
                // never aims steeply down at that old point near its own feet.
                var maxHeight = horizontal * MathF.Tan(12f * MathF.PI / 180f);
                return new(point.X, point.Y, Math.Clamp(point.Z, eye.Z - maxHeight, eye.Z + maxHeight));
            }
        }
        var fallback = Direction(direction.X * 100f, direction.Y * 100f, 0);
        return new(eye.X + fallback.X * 512f, eye.Y + fallback.Y * 512f, eye.Z);
    }
}
