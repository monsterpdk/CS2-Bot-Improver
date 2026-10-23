namespace NadeSystem;

internal static class NativeGrenadePolicy
{
    public static bool Available(string type,bool smoke,bool he,bool molotov) => type switch
    {
        "smoke"=>smoke,
        "he"=>he,
        "molotov" or "incgrenade"=>molotov,
        "flash" or "decoy"=>true,
        _=>false
    };
}
