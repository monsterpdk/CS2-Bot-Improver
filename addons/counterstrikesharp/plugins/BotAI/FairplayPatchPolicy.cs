namespace BotAI;

internal static class FairplayPatchPolicy
{
    private static readonly HashSet<string> Disabled=new(StringComparer.Ordinal)
    {
        "DefuseBomb_SkipIsVisibleCheck", "BombBeep_CT_GlobalHearRange",
        "OnBombPlanted_AllBotsLearnSite", "OnAudibleEvent_GlobalHearRange",
        "BombPickup_CT_GlobalHearRange",
        // Respect native shooting delay and target-notice decisions.
        "AttackState_SkipFireRateCheck", "IsNoticable_AlwaysTrue"
    };
    internal static bool IsEnabled(string name)=>!Disabled.Contains(name);
}
