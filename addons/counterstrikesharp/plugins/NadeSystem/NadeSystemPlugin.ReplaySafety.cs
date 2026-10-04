using System;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace NadeSystem;

public partial class NadeSystemPlugin
{
    private bool TryReleasePoint(CCSPlayerController bot, string type, out Vector release, out Vec3 eye)
    {
        release = new Vector();
        eye = new Vec3();
        var pawn = bot.PlayerPawn?.Value;
        if (!bot.IsValid || !bot.PawnIsAlive || bot.HasBeenControlledByPlayerThisRound
            || pawn is not { IsValid: true } || pawn.AbsOrigin is null || pawn.EyeAngles is null) return false;
        eye = new Vec3 { X = pawn.AbsOrigin.X, Y = pawn.AbsOrigin.Y, Z = pawn.AbsOrigin.Z + pawn.ViewOffset.Z };
        float yaw = pawn.EyeAngles.Y * MathF.PI / 180f;
        float pitch = pawn.EyeAngles.X * MathF.PI / 180f;
        release.X = eye.X + 16f * MathF.Cos(pitch) * MathF.Cos(yaw);
        release.Y = eye.Y + 16f * MathF.Cos(pitch) * MathF.Sin(yaw);
        release.Z = eye.Z - 16f * MathF.Sin(pitch);
        if (!ReplaySafety.Finite(eye) || !float.IsFinite(release.X) || !float.IsFinite(release.Y) || !float.IsFinite(release.Z)) return false;
        try
        {
            var result = Trace.TraceEndShape(new Vector(eye.X, eye.Y, eye.Z), release, pawn,
                new TraceOptions { InteractsWith = Masks.SolidBrushOnly });
            if (!float.IsFinite(result.Fraction) || result.Fraction < 0.999f)
            {

                return false;
            }
        }
        catch
        {

            return false; // Never bypass collision safety when tracing fails.
        }
        return true;
    }

    private bool TrySafeReplay(CCSPlayerController bot, GrenadeData grenade, out Vector release)
    {
        if (!TryReleasePoint(bot, grenade.GrenadeType, out release, out var eye)) return false;
        var angles = bot.PlayerPawn.Value?.EyeAngles;
        if (angles is null) return false;
        string? reason = ReplaySafety.Reject(eye, angles.Y, angles.X, grenade);
        if (reason is null) return true;

        return false;
    }
}
