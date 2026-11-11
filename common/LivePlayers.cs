using System;using System.Collections.Generic;using CounterStrikeSharp.API;using CounterStrikeSharp.API.Core;
namespace Fairplay.Runtime;
internal static class LivePlayers
{
    // Avoid Server.MaxPlayers caching the menu's one-player limit.
    internal static List<CCSPlayerController> Get()
    {
        var result=new List<CCSPlayerController>();int max=Math.Clamp(NativeAPI.GetMaxClients(),0,128);
        for(int slot=0;slot<max;slot++){var p=Utilities.GetPlayerFromSlot(slot);if(p is not null&&p.IsValid&&p.Connected==PlayerConnectedState.Connected)result.Add(p);}
        return result;
    }
}
