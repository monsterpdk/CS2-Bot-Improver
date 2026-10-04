using System;

namespace NadeSystem;

internal static class ReplaySafety
{
    // Gameplay limits for recorded lineups, not a simulation of Valve's throw strength.
    internal const float MaxReleaseOffset = 64f;
    internal const float MaxSpeed = 1000f;
    internal const float MaxLandingDistance = 1800f;

    internal static string? Reject(Vec3 eye, float yaw, float pitch, GrenadeData grenade)
    {
        var origin = grenade.ProjectilePosition;
        var velocity = grenade.ProjectileVelocity;
        var landing = grenade.LandingPosition;
        if (!Finite(eye) || !Finite(origin) || !Finite(velocity) || !Finite(landing)
            || !float.IsFinite(yaw) || !float.IsFinite(pitch)) return "invalidGeometry";
        if (DistanceSquared(eye, origin) > MaxReleaseOffset * MaxReleaseOffset) return "remoteRelease";
        float speedSquared = velocity.X * velocity.X + velocity.Y * velocity.Y + velocity.Z * velocity.Z;
        if (speedSquared <= 0f || speedSquared > MaxSpeed * MaxSpeed) return "excessSpeed";
        float dx = landing.X - origin.X, dy = landing.Y - origin.Y;
        if (dx * dx + dy * dy > MaxLandingDistance * MaxLandingDistance) return "excessRange";
        float horizontal = MathF.Sqrt(velocity.X * velocity.X + velocity.Y * velocity.Y);
        if (horizontal > 1f)
        {
            float radians = yaw * MathF.PI / 180f;
            float dot = (MathF.Cos(radians) * velocity.X + MathF.Sin(radians) * velocity.Y) / horizontal;
            if (dot < 0.5f) return "wrongHeading"; // more than 60 degrees sideways/backwards
        }
        float throwPitch = -MathF.Atan2(velocity.Z, horizontal) * 180f / MathF.PI;
        if (MathF.Abs(throwPitch - pitch) > 75f) return "wrongPitch";
        return null;
    }

    internal static float DistanceSquared(Vec3 a, Vec3 b)
        => (a.X-b.X)*(a.X-b.X) + (a.Y-b.Y)*(a.Y-b.Y) + (a.Z-b.Z)*(a.Z-b.Z);
    internal static bool Finite(Vec3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
}
