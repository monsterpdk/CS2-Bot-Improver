using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace BotAimImprover;

public partial class BotAimImprover
{
    private MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, float>? _smokeDensity;
    private readonly HashSet<int> _activeSmoke = new();
    private readonly Dictionary<IntPtr, (int Enemy, float Time, bool Blocked)> _smokeSamples = new();
    private readonly Dictionary<IntPtr, (int Enemy, AimLocation Location)> _clearAim = new();
    private readonly Dictionary<IntPtr, AimLocation> _blindDirections = new();
    private readonly Dictionary<IntPtr, AimLocation> _clearDirections = new();
    private bool _reportedSmokeError;

    private void LoadSmokeFairAim()
    {
        if (!OperatingSystem.IsWindows()) return; // Windows offsets verified only.
        _smokeDensity = new("40 55 53 56 41 55 41 57 48 8D 6C 24");
        RegisterEventHandler<EventSmokegrenadeDetonate>((ev, _) =>
        {
            _activeSmoke.Add(ev.Entityid);
            _smokeSamples.Clear();
            return HookResult.Continue;
        });
        RegisterEventHandler<EventSmokegrenadeExpired>((ev, _) =>
        {
            _activeSmoke.Remove(ev.Entityid);
            _smokeSamples.Clear();
            return HookResult.Continue;
        });
        RegisterEventHandler<EventRoundStart>((_, _) => { ResetSmokeAim(); return HookResult.Continue; });
        RegisterListener<Listeners.OnMapStart>(_ => ResetSmokeAim());
    }

    private void ResetSmokeAim()
    {
        _activeSmoke.Clear();
        _smokeSamples.Clear();
        _clearAim.Clear();
        _blindDirections.Clear();
        _clearDirections.Clear();
        _reportedSmokeError = false;
    }

    private unsafe bool ConcealedAim(IntPtr bot, int enemy, CCSPlayerPawn target, Vector eye, CCSPlayerController controller)
    {
        if (_smokeDensity is null || target.AbsOrigin is null) return false;
        if (_activeSmoke.Count == 0) { _blindDirections.Remove(bot); return false; }
        var now = Server.CurrentTime;
        bool blocked;
        try
        {
            if (_smokeSamples.TryGetValue(bot, out var sample) && sample.Enemy == enemy && now >= sample.Time && now - sample.Time < 0.1f)
                blocked = sample.Blocked;
            else
            {
                var origin = target.AbsOrigin;
                var head = new AimLocation(origin.X, origin.Y, origin.Z + target.ViewOffset.Z);
                var chest = new AimLocation(origin.X, origin.Y, origin.Z + target.ViewOffset.Z * 0.67f);
                blocked = SmokeAimPolicy.Concealed(SmokeDensity(eye, head), SmokeDensity(eye, chest));
                _smokeSamples[bot] = (enemy, now, blocked);
            }
            if (!blocked) { _blindDirections.Remove(bot); return false; }
            var hasMemory = _clearAim.TryGetValue(bot, out var last);
            if (!_blindDirections.TryGetValue(bot, out var direction))
            {
                var pawn = controller.PlayerPawn.Value;
                var velocity = pawn?.AbsVelocity;
                direction = velocity is not null && velocity.X * velocity.X + velocity.Y * velocity.Y >= 32f * 32f
                    ? SmokeAimPolicy.Direction(velocity.X, velocity.Y, 0)
                    : _clearDirections.TryGetValue(bot, out var trusted) ? trusted
                    : SmokeAimPolicy.Direction(0, 0, pawn?.EyeAngles.Y ?? 0);
                direction = OpenBlindDirection(eye, direction);
                _blindDirections[bot] = direction;
            }
            var held = SmokeAimPolicy.Hold(enemy, hasMemory ? last.Enemy : -1, hasMemory ? last.Location : null, new(eye.X, eye.Y, eye.Z), direction);
            // Instruction-validated Windows CS2 ClientVersion 2000924 fields. Clear native
            // visibility and undo the just-computed live enemy aim point. Native
            // blind fire toward the last clearly observed point can still occur.
            Marshal.WriteByte(bot + _off.IsVisible, 0);
            float* spot = (float*)(bot + _off.TargetSpot).ToPointer();
            spot[0] = held.X; spot[1] = held.Y; spot[2] = held.Z;
            return true;
        }
        catch (Exception ex)
        {
            if (!_reportedSmokeError)
            {
                _reportedSmokeError = true;
                Logger.LogError(ex, "[SmokeAim] Smoke aim check failed");
            }
            return false;
        }
    }

    private unsafe float SmokeDensity(Vector eye, AimLocation target)
    {
        float* from = stackalloc float[3] { eye.X, eye.Y, eye.Z };
        float* to = stackalloc float[3] { target.X, target.Y, target.Z };
        return _smokeDensity!.Invoke((IntPtr)from, (IntPtr)to, IntPtr.Zero);
    }

    private unsafe void RememberClearAim(IntPtr bot, int enemy, CCSPlayerController? controller = null)
    {
        if (_smokeDensity is null) return;
        float* spot = (float*)(bot + _off.TargetSpot).ToPointer();
        var location = new AimLocation(spot[0], spot[1], spot[2]);
        if (location.Finite)
        {
            _clearAim[bot] = (enemy, location);
            var yaw = controller?.PlayerPawn.Value?.EyeAngles.Y;
            if (yaw.HasValue) _clearDirections[bot] = SmokeAimPolicy.Direction(0, 0, yaw.Value);
        }
    }

    private AimLocation OpenBlindDirection(Vector eye, AimLocation preferred)
    {
        // World-only corridor probes, once per concealed period. No enemy
        // positions or sounds enter this choice; facing is not refreshed while
        // concealed, so the native AI's changing hidden aim cannot steer it.
        try
        {
            var best = preferred;
            var bestScore = float.NegativeInfinity;
            foreach (var angle in new float[] { 0, -45, 45, -90, 90, -135, 135, 180 })
            {
                var radians = angle * MathF.PI / 180f;
                var direction = new AimLocation(preferred.X * MathF.Cos(radians) - preferred.Y * MathF.Sin(radians), preferred.X * MathF.Sin(radians) + preferred.Y * MathF.Cos(radians), 0);
                var end = new Vector(eye.X + direction.X * 512f, eye.Y + direction.Y * 512f, eye.Z);
                var result = Trace.TraceEndShape(eye, end, options: new TraceOptions { InteractsWith = Masks.SolidBrushOnly });
                if (!float.IsFinite(result.Fraction)) continue;
                var score = SmokeAimPolicy.CorridorScore(result.Fraction, angle);
                if (score > bestScore) { bestScore = score; best = direction; }
            }
            return best;
        }
        catch { return preferred; }
    }
}
