using Fairplay.Diagnostics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;
namespace BotStuckDiagnostics;

public sealed class Plugin:BasePlugin
{
    public override string ModuleName=>"Bot Stuck Diagnostics";
    public override string ModuleVersion=>"0.1.0-fairplay.6";
    public override string ModuleAuthor=>"Fairplay maintenance";
    public override string ModuleDescription=>"Read-only, bounded cross-map stationary bot observation";
    private sealed class Entry
    {
        public required nint Pawn;
        public required IdleTracker Tracker;
        public string Name="";
        public float NextSurvivorSample;
    }
    private readonly Dictionary<int,Entry> _entries=new();
    private readonly Dictionary<int,float> _fired=new();
    private readonly string _session=Guid.NewGuid().ToString("N");
    private string _map="not-loaded";
    private int _mapInstance,_round;
    private bool _live,_active,_failed;
    private long _samples,_candidates,_rows,_snapshotErrors;
    private float _nextHeartbeat;
    private DiagnosticLog? _log;
    private object? _controller;
    private MethodInfo? _isLocked,_isReplaying,_weaponLock;

    public override void Load(bool hotReload)
    {
        AddCommand("bot_stuck_status","Show the persistent read-only stuck observer status",(_,info)=>
            info.ReplyToCommand($"[StuckObserver] version={ModuleVersion} active={_active} live={_live} failed={_failed} maps={_mapInstance} samples={_samples} candidates={_candidates} rows={_rows} snapshotErrors={_snapshotErrors} log={_log?.Path}"));
        var steam=System.IO.Path.GetFullPath(System.IO.Path.Combine(ModuleDirectory,"..","..","..","..","steam.inf"));
        if(!OperatingSystem.IsWindows() || !File.Exists(steam) || !File.ReadLines(steam).Any(s=>s.Trim()=="ClientVersion=2000943"))
        {Logger.LogError("[StuckObserver] Inactive on unaudited build/platform");return;}
        _log=new(ModuleDirectory,"stuck");_active=_log.Enabled;
        AddCommand("bot_stuck_diagnostics","Detailed observation: on/off (default off)",(_,info)=>
        {
            var value=info.ArgCount>1?info.GetArg(1):"";
            if(value is not ("on" or "off")){info.ReplyToCommand("Usage: bot_stuck_diagnostics on|off");return;}
            _entries.Clear();_log.SetEnabled(value=="on");_active=_log.Enabled;
            info.ReplyToCommand($"[StuckObserver] active={_active} maxBytes={2*DiagnosticLog.FileLimit}");
        });
        Write("session_start",null,new {version=ModuleVersion,readOnly=true,thresholdSeconds=IdleTracker.Threshold,radius=IdleTracker.Radius,verticalRadius=IdleTracker.VerticalRadius,repeatSeconds=IdleTracker.RepeatInterval,maxLogBytes=2*DiagnosticLog.FileLimit});
        RegisterListener<Listeners.OnMapStart>(map=>
        {
            EndEntries("map_change",null);_live=false;_map=map;_mapInstance++;_round=0;_fired.Clear();
            Write("map_start",null,new {map});
        });
        RegisterListener<Listeners.OnMapEnd>(()=>{_live=false;EndEntries("map_end",null);Write("map_end",null,null);});
        RegisterEventHandler<EventRoundStart>((_,_)=>
        {
            _live=false;EndEntries("round_start",Server.CurrentTime);
            if(_mapInstance==0){_map=Server.MapName;_mapInstance=1;Write("map_inferred_after_reload",Server.CurrentTime,null);}
            _round++;_fired.Clear();Write("round_start",Server.CurrentTime,null);return HookResult.Continue;
        });
        RegisterEventHandler<EventRoundFreezeEnd>((_,_)=>
        {_live=true;_nextHeartbeat=Server.CurrentTime;Write("round_live",Server.CurrentTime,null);return HookResult.Continue;});
        RegisterEventHandler<EventRoundEnd>((_,_)=>
        {_live=false;EndEntries("round_end",Server.CurrentTime);Write("round_end",Server.CurrentTime,null);return HookResult.Continue;});
        RegisterEventHandler<EventPlayerDeath>((ev,_)=>{if(ev.Userid is {} p)EndSlot(p.Slot,"death",Server.CurrentTime);return HookResult.Continue;});
        RegisterEventHandler<EventPlayerDisconnect>((ev,_)=>{if(ev.Userid is {} p)EndSlot(p.Slot,"disconnect",Server.CurrentTime);return HookResult.Continue;});
        RegisterEventHandler<EventWeaponFire>((ev,_)=>{if(_live && ev.Userid is {IsBot:true} p)_fired[p.Slot]=Server.CurrentTime;return HookResult.Continue;});
        // No STOP_ON_MAPCHANGE: observation continues through every map transition.
        AddTimer(1,Sample,CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);
        AddCommand("bot_stuck_mark","Record current bot snapshots; optional bot name or slot",(_,info)=>
        {
            if(!_live || !_active || _failed){info.ReplyToCommand("[StuckObserver] Waiting for a live round");return;}
            try
            {
                var players=Fairplay.Runtime.LivePlayers.Get();var filter=info.ArgCount>1?info.GetArg(1):"";int count=0;
                var rules=Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault()?.GameRules;
                foreach(var p in players.Where(Eligible).Where(p=>filter=="" || p.Slot.ToString()==filter || p.PlayerName.Contains(filter,StringComparison.OrdinalIgnoreCase)))
                {Snapshot("manual_mark",p,players,rules,Server.CurrentTime,0);count++;}
                info.ReplyToCommand($"[StuckObserver] Saved {count} bot snapshots");
            }catch(Exception ex){Fail(ex);}
        });
        Logger.LogInformation("[StuckObserver] Observation only; continuous cross-map log: {Path}",_log.Path);
        // Deliberately no entity lookup or live-round activation at load/hot reload.
    }
    public override void Unload(bool hotReload){_live=false;EndEntries("unload",null);Write("session_end",null,new {samples=_samples,candidates=_candidates});_active=false;}
    private static bool Eligible(CCSPlayerController p)=>p.IsValid && p.IsBot && p.PawnIsAlive && !p.HasBeenControlledByPlayerThisRound && p.TeamNum is 2 or 3 && p.PlayerPawn.Value is {IsValid:true,AbsOrigin:not null};
    private static Point Pos(Vector v)=>new(v.X,v.Y,v.Z);
    private void Sample()
    {
        if(!_active || _failed || !_live)return; // Before any game/native access.
        try
        {
            var now=Server.CurrentTime;
            var rules=Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault()?.GameRules;
            if(rules is null || rules.FreezePeriod){EndEntries("freeze_or_missing_rules",now);return;}
            var players=Fairplay.Runtime.LivePlayers.Get();var bots=players.Where(Eligible).ToList();var slots=bots.Select(p=>p.Slot).ToHashSet();
            int aliveT=players.Count(p=>p.IsValid&&p.PawnIsAlive&&p.TeamNum==2),aliveCT=players.Count(p=>p.IsValid&&p.PawnIsAlive&&p.TeamNum==3);
            foreach(var slot in _entries.Keys.Where(s=>!slots.Contains(s)).ToArray())EndSlot(slot,"not_eligible",now);
            _samples++;
            foreach(var p in bots)
            {
                var pawn=p.PlayerPawn.Value!;var point=Pos(pawn.AbsOrigin!);if(!point.Finite)continue;
                if(!_entries.TryGetValue(p.Slot,out var e) || e.Pawn!=pawn.Handle)
                {
                    EndSlot(p.Slot,"pawn_changed",now);_fired.Remove(p.Slot);
                    _entries[p.Slot]=new(){Pawn=pawn.Handle,Name=p.PlayerName,Tracker=new(now,point)};continue;
                }
                var observation=e.Tracker.Observe(now,point);
                // This is context observation, not a claim that the survivor is stuck.
                // Jumping/backtracking can prevent stationary detection entirely.
                if((p.TeamNum==2?aliveT:aliveCT)==1&&now>=e.NextSurvivorSample)
                {e.NextSurvivorSample=now+5;Snapshot("last_survivor_sample",p,players,rules,now,0);}
                if(observation.Notice is Notice.First or Notice.Repeat)
                {if(observation.Notice==Notice.First)_candidates++;Snapshot(observation.Notice==Notice.First?"stationary_candidate":"stationary_repeat",p,players,rules,now,observation.Seconds);}
                else if(observation.Notice is Notice.Resumed or Notice.Gap)
                    Write("stationary_end",now,new {slot=p.Slot,name=e.Name,reason=observation.Notice.ToString(),seconds=observation.Seconds});
            }
            if(now>=_nextHeartbeat){Write("heartbeat",now,new {aliveBots=bots.Count,samples=_samples,candidates=_candidates,snapshotErrors=_snapshotErrors});_nextHeartbeat=now+60;}
        }catch(Exception ex){Fail(ex);}
    }
    private void EndSlot(int slot,string reason,float? now)
    {
        if(_entries.Remove(slot,out var e) && e.Tracker.Reported)
            Write("stationary_end",now,new {slot,name=e.Name,reason,lastSample=e.Tracker.LastSample,seconds=MathF.Max(0,e.Tracker.LastSample-e.Tracker.Started)});
        _fired.Remove(slot);
    }
    private void EndEntries(string reason,float? now){foreach(var slot in _entries.Keys.ToArray())EndSlot(slot,reason,now);}
    private object? Read(Func<object?> get,string field,List<string> errors)
    {
        try
        {
            var value=get();
            if(value is float f && !float.IsFinite(f))return null;
            if(value is double d && !double.IsFinite(d))return null;
            if(value is Point point && !point.Finite)return null;
            return value;
        }catch(Exception ex){errors.Add(field+":"+ex.GetType().Name);return null;}
    }
    private void Snapshot(string kind,CCSPlayerController p,List<CCSPlayerController> players,CCSGameRules? rules,float now,float seconds)
    {
        var errors=new List<string>();var pawn=p.PlayerPawn.Value!;var bot=pawn.Bot;
        object? R(string field,Func<object?> read)=>Read(read,field,errors);
        var liveT=players.Count(x=>x.IsValid && x.PawnIsAlive && x.TeamNum==2);
        var liveCT=players.Count(x=>x.IsValid && x.PawnIsAlive && x.TeamNum==3);
        var native=new Dictionary<string,object?>();
        if(bot is not null)
        {
            native["goal"]=R("goal",()=>Pos(bot.GoalPosition));
            native["goalEntity"]=R("goalEntity",()=>bot.GoalEntity.Raw);
            native["taskEntity"]=R("taskEntity",()=>bot.TaskEntity.Raw);
            native["pathIndex"]=R("pathIndex",()=>bot.PathIndex);
            native["pathLadderEnd"]=R("pathLadderEnd",()=>bot.PathLadderEnd);
            native["stateAge"]=R("stateAge",()=>now-bot.StateTimestamp);
            native["isStopping"]=R("isStopping",()=>bot.IsStopping);
            native["enemyVisible"]=R("enemyVisible",()=>bot.IsEnemyVisible);
            native["enemy"]=R("enemy",()=>bot.Enemy.Raw);
            native["lastSawEnemyAge"]=R("lastSawEnemyAge",()=>now-bot.LastSawEnemyTimestamp);
            native["attacking"]=R("attacking",()=>bot.IsAttacking);
            native["friendInWay"]=R("friendInWay",()=>bot.IsFriendInTheWay);
            native["waitingBehindFriend"]=R("waitingBehindFriend",()=>bot.IsWaitingBehindFriend);
            native["waitDeadline"]=R("waitDeadline",()=>bot.WaitTimer.Timestamp);
            native["repathDeadline"]=R("repathDeadline",()=>bot.RepathTimer.Timestamp);
            native["stuckTimestamp"]=R("stuckTimestamp",()=>bot.StuckTimestamp);
            native["isStuck"]=R("isStuck",()=>bot.IsStuck);
            native["stuckSpot"]=R("stuckSpot",()=>Pos(bot.StuckSpot));
            native["pathfinderControlsEyes"]=R("pathfinderControlsEyes",()=>bot.EyeAnglesUnderPathFinderControl);
            native["lookAt"]=R("lookAt",()=>Pos(bot.LookAtSpot));
            native["targetSpot"]=R("targetSpot",()=>Pos(bot.TargetSpot));
            native["lookDeadline"]=R("lookDeadline",()=>bot.LookAtSpotTimestamp+bot.LookAtSpotDuration);
        }
        var data=new Dictionary<string,object?>
        {
            ["slot"]=p.Slot,["name"]=p.PlayerName,["team"]=p.TeamNum,["stillSeconds"]=seconds,
            ["verticalSpan"]=_entries.TryGetValue(p.Slot,out var tracked)?tracked.Tracker.VerticalSpan:(float?)null,
            ["position"]=R("position",()=>Pos(pawn.AbsOrigin!)),["velocity"]=R("velocity",()=>Pos(pawn.AbsVelocity)),
            ["eyeAngles"]=R("eyeAngles",()=>new {pitch=pawn.EyeAngles.X,yaw=pawn.EyeAngles.Y}),
            ["grounded"]=R("grounded",()=>(pawn.Flags&1)!=0),["moveType"]=R("moveType",()=>pawn.MoveType.ToString()),
            ["actualMoveType"]=R("actualMoveType",()=>pawn.ActualMoveType.ToString()),
            ["ladderNormal"]=R("ladderNormal",()=>pawn.MovementServices is {} movement&&movement.Handle!=0?Pos(new CCSPlayer_MovementServices(movement.Handle).LadderNormal):null),
            ["weapon"]=R("weapon",()=>pawn.WeaponServices?.ActiveWeapon.Value?.DesignerName),
            ["hasBomb"]=R("hasBomb",()=>pawn.WeaponServices?.MyWeapons.Any(w=>w.Value?.DesignerName=="weapon_c4")),
            ["defusing"]=R("defusing",()=>pawn.IsDefusing),["flashRemaining"]=R("flashRemaining",()=>MathF.Max(0,pawn.BlindUntilTime-now)),
            ["planting"]=R("planting",()=>pawn.WeaponServices?.ActiveWeapon.Value is {} weapon && weapon.DesignerName=="weapon_c4" ? weapon.As<CC4>().StartedArming : false),
            ["lastShotAge"]=_fired.TryGetValue(p.Slot,out var fired)?now-fired:null,
            ["aliveT"]=liveT,["aliveCT"]=liveCT,["lastAliveOnTeam"]=p.TeamNum==2?liveT==1:liveCT==1,
            ["bombPlanted"]=R("bombPlanted",()=>rules?.BombPlanted),
            ["roundRemaining"]=R("roundRemaining",()=>rules is null?null:rules.RoundTime-(now-rules.RoundStartTime)),
            ["native"]=native,["controller"]=ControlState(p.Slot),["errors"]=errors
        };
        if(errors.Count>0)_snapshotErrors++;
        Write(kind,now,data);
    }
    private object ControlState(int slot)
    {
        try
        {
            if(_controller is null)
            {
                var contract=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("BotControllerApi.IBotControllerApi")).FirstOrDefault(t=>t is not null);
                if(contract is null)return new {ready=false};
                var capType=typeof(PluginCapability<>).MakeGenericType(contract);var cap=Activator.CreateInstance(capType,"botcontroller:api");
                _controller=capType.GetMethod("Get")!.Invoke(cap,null);if(_controller is null)return new {ready=false};
                var type=_controller.GetType();_isLocked=type.GetMethods().Single(m=>m.Name=="IsLocked" && m.GetParameters().Length==2 && m.GetParameters()[1].ParameterType.IsEnum);
                _isReplaying=type.GetMethod("IsReplaying");_weaponLock=type.GetMethod("GetWeaponLock");
            }
            var kind=_isLocked!.GetParameters()[1].ParameterType;
            return new {ready=true,all=_isLocked.Invoke(_controller,new[]{(object)slot,Enum.Parse(kind,"All")}),aim=_isLocked.Invoke(_controller,new[]{(object)slot,Enum.Parse(kind,"Aim")}),weapon=_weaponLock?.Invoke(_controller,new object[]{slot})?.ToString(),replay=_isReplaying?.Invoke(_controller,new object[]{slot})};
        }catch(Exception ex){_controller=null;return new {ready=false,error=ex.GetType().Name};}
    }
    private void Write(string kind,float? time,object? data)
    {
        if(_log is null || !_log.Enabled || _failed)return;
        try{_log.Append(JsonSerializer.Serialize(new {utc=DateTime.UtcNow,session=_session,map=_map,mapInstance=_mapInstance,round=_round,time,kind,data}));_rows++;}
        catch(Exception ex){Fail(ex);}
    }
    private void Fail(Exception ex)
    {
        if(_failed)return;_log?.Error(ex);_failed=true;_live=false;
        Logger.LogError(ex,"[StuckObserver] Observation stopped after an error; bot behavior unchanged");
    }
}
