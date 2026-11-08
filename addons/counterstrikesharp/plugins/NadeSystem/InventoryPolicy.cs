using System;
using System.Collections.Generic;
namespace NadeSystem;
internal static class InventoryPolicy
{
    internal static string? Kind(string name)=>name switch
    {
        "weapon_smokegrenade"=>"smoke","weapon_hegrenade"=>"he","weapon_flashbang"=>"flash",
        "weapon_molotov" or "weapon_incgrenade"=>"molotov","weapon_decoy"=>"decoy",_=>null
    };
    internal static int Item(string name)=>name switch
    {"weapon_flashbang"=>43,"weapon_hegrenade"=>44,"weapon_smokegrenade"=>45,"weapon_molotov"=>46,"weapon_decoy"=>47,"weapon_incgrenade"=>48,_=>0};
    internal static int Limit(string kind,int flashLimit)=>kind=="flash"?Math.Clamp(flashLimit,0,2):1;
    internal static bool CanBuy(string kind,IReadOnlyDictionary<string,int> stock,int total,int totalLimit,int flashLimit,int cost,int money,int budget)
        =>(kind is "smoke" or "he" or "flash" or "molotov" or "decoy") && cost>0 && cost<=money && cost<=budget && total<Math.Clamp(totalLimit,0,4) && stock.GetValueOrDefault(kind)<Limit(kind,flashLimit);
    internal static bool CanSpend(int owned,int pending)=>owned>pending && pending>=0;
}
