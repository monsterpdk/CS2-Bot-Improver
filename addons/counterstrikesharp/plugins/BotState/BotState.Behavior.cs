using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace BotState;

public partial class BotState
{

    // Resets the Bot's look-around bookkeeping so it can reacquire threats
    private static void ResetLookAroundForBot(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || !player.IsBot) return;
        var pawn = player.PlayerPawn?.Value;
        if (pawn == null || !pawn.IsValid) return;
        var bot = pawn.Bot;
        if (bot == null) return;

        ref float inhibitLookAroundTimestamp = ref bot.InhibitLookAroundTimestamp;
        inhibitLookAroundTimestamp = 0f;

        ref int checkedHidingSpotCount = ref bot.CheckedHidingSpotCount;
        checkedHidingSpotCount = 0;

        ref float lookAroundStateTimestamp = ref bot.LookAroundStateTimestamp;
        lookAroundStateTimestamp = 0f;
    }
    //---------------------------------------------------------------------------------------
    // Removes the native early-round safe period for this Bot.
    private static void ApplyBotState(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return;

        var bot = pawn.Bot;
        if (bot == null) return;

        ref float safeTime = ref bot.SafeTime;
        safeTime = 0f;

        ref bool hasVisitedEnemySpawn = ref bot.HasVisitedEnemySpawn;
        hasVisitedEnemySpawn = true;
    }

    // Keeps bots active without forcing combat readiness or firing decisions.
    private static void UpdateBotBehavior(CCSBot bot, float now)
    {
        ref bool isSleeping = ref bot.IsSleeping;
        isSleeping = false;

        ref bool allowActive = ref bot.AllowActive;
        allowActive = true;

        // Native perception, surprise and fire timers belong to the engine.
        // Do not erase the delays selected by the active bot profile.
        // Teammate Stuck Fix
        ref bool IsWaitingBehindFriend = ref bot.IsWaitingBehindFriend;
        IsWaitingBehindFriend = false;

        CountdownTimer politeTimer = bot.PoliteTimer;

        ref float politeTimerDuration = ref politeTimer.Duration;
        politeTimerDuration = 0.0f;

        ref float politeTimerTimestamp = ref politeTimer.Timestamp;
        politeTimerTimestamp = 0.0f;

        ref float politeTimerTimescale = ref politeTimer.Timescale;
        politeTimerTimescale = 1.0f;

    }
}
