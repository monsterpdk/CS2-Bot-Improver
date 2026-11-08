using System;
using System.Collections.Generic;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
namespace NadeSystem;
public partial class NadeSystemPlugin
{
    private void TryReplay(CCSPlayerController bot,GrenadeData g,List<CCSPlayerController> allControllers)
    {
        if(_botNadesMode=="off" || bot.HasBeenControlledByPlayerThisRound)return;
        var kind=g.GrenadeType;NadeStats(kind).Considered++;
        if(!ReadyGrenade(kind)){NadeStats(kind).Unavailable++;return;}
        if(_botNadesMode=="less" && !LessModeAllows(kind,bot.Index))return;
        if(!TrySafeReplay(bot,g,out _)){NadeStats(kind).SafetyRejected++;return;}
        SpawnProjectile(bot,g);
    }
    private void SpawnProjectile(CCSPlayerController bot,GrenadeData g)
    {
        var kind=g.GrenadeType;
        if(!ReadyGrenade(kind))return;
        QueueInventoryThrow(bot,kind,()=>
        {
            if(!TrySafeReplay(bot,g,out var release))return null;
            return (release,new Vector(g.ProjectileVelocity.X,g.ProjectileVelocity.Y,g.ProjectileVelocity.Z));
        },()=>
        {
            RegisterCooldown(g.Id,kind);IncrementCount(kind,bot.TeamNum);
            if(_botNadesMode=="less")IncrementBotCount(kind,bot.Index);
            if((_botNadesMode=="normal" || _botNadesMode=="less") && kind=="smoke" && _freezeEndTime>0 && CounterStrikeSharp.API.Server.CurrentTime-_freezeEndTime<5)
            {_earlySmokeCountByTeam.TryGetValue(bot.TeamNum,out var count);_earlySmokeCountByTeam[bot.TeamNum]=count+1;}
        });
    }
}
