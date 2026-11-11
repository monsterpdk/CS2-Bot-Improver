namespace BotState;

internal static class LadderMovementPolicy
{
    internal static bool ShouldDefer(bool climbing,float now,float? lastClimb)
        =>climbing||(float.IsFinite(now)&&lastClimb is {} last&&float.IsFinite(last)&&now>=last&&now-last<5);
}
