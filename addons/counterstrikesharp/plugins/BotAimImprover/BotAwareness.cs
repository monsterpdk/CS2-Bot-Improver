using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace BotAimImprover;

public partial class BotAimImprover
{
    private sealed class Awareness
    {
        public required nint Pawn;
        public HeardCue? Heard;
        public float LastReacted = -100;
        public AimLocation Position;
        public float LastProgress;
        public AimLocation? NativeGoal;
        public float GoalUntil;
        public AimLocation? Bomb;
        public bool ExactBomb;
        public float BombUntil;
        public AimLocation? Steer;
        public float SteerUntil;
        public float NextMove;
    }
    private readonly Dictionary<int,Awareness> _awareness = new();
    private readonly Dictionary<int,(AimLocation Center,float Until)> _smokeClouds = new();
    private readonly Dictionary<int,float> _soundDebounce = new();
    private readonly List<(int Owner,AimLocation Point,float Until)> _reservations = new();
    private List<CCSPlayerController> _awarenessPlayers = new();
    private bool _roundLive, _awarenessEnabled=true, _awarenessFailed, _freeForAll;
    private int _soundSequence;
    private int _audioTraceBudget=32;
    private long _heardCount, _reactCount, _smokeMoveCount, _spaceCount;
    private object? _controllerApi;
    private MethodInfo? _isReplaying, _isLocked;
    private object? _lockAll;
    private object? _lockAim;
    private static readonly TraceOptions AwarenessTrace = new() { InteractsWith=Masks.Solid };
    private static readonly TraceOptions AwarenessWorldTrace = new() { InteractsWith=Masks.SolidBrushOnly };

    private void LoadAwareness()
    {
        RegisterEventHandler<EventPlayerSound>((ev,_) => { HearPlayer(ev.Userid,ev.Radius,ev.Step); return HookResult.Continue; });
        RegisterEventHandler<EventPlayerFootstep>((ev,_) => { HearPlayer(ev.Userid,720,true); return HookResult.Continue; });
        RegisterEventHandler<EventWeaponFire>((ev,_) =>
        {
            var quiet=ev.Weapon.Contains("knife") || ev.Weapon.Contains("grenade") || ev.Weapon.Contains("molotov") || ev.Weapon.Contains("decoy") || ev.Weapon.Contains("c4");
            HearPlayer(ev.Userid,quiet ? 180 : ev.Silenced ? 650 : 1800,false); return HookResult.Continue;
        });
        RegisterEventHandler<EventBombBeep>((ev,_) => { HearBomb((int)ev.Entindex); return HookResult.Continue; });
        RegisterEventHandler<EventSmokegrenadeDetonate>((ev,_) => { _smokeClouds[ev.Entityid]=(new(ev.X,ev.Y,ev.Z),Server.CurrentTime+19); return HookResult.Continue; });
        RegisterEventHandler<EventSmokegrenadeExpired>((ev,_) => { _smokeClouds.Remove(ev.Entityid); return HookResult.Continue; });
        RegisterEventHandler<EventRoundStart>((_,_) => { ResetAwareness(); return HookResult.Continue; });
        RegisterEventHandler<EventRoundFreezeEnd>((_,_) => { _roundLive=true; return HookResult.Continue; });
        RegisterEventHandler<EventRoundEnd>((_,_) => { _roundLive=false; _awareness.Clear(); _reservations.Clear(); return HookResult.Continue; });
        RegisterEventHandler<EventPlayerDeath>((ev,_) => { if(ev.Userid is {} p) ForgetAwareness(p); return HookResult.Continue; });
        RegisterEventHandler<EventPlayerDisconnect>((ev,_) => { if(ev.Userid is {} p) ForgetAwareness(p); return HookResult.Continue; });
        RegisterListener<Listeners.OnMapStart>(_ => ResetAwareness());
        RegisterListener<Listeners.OnMapEnd>(ResetAwareness);
        RegisterListener<Listeners.OnTick>(SteerAwareness);
        AddTimer(0.2f,UpdateAwareness,CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);
        AddCommand("bot_fair_awareness","Sound, smoke movement and teammate-spacing trial: 0/1/status",(_,info) =>
        {
            var arg=info.ArgCount>1 ? info.GetArg(1) : "status";
            if(arg is "0" or "1") { var wasLive=_roundLive; _awarenessEnabled=arg=="1"; ResetAwareness(); _roundLive=wasLive; }
            Server.PrintToConsole($"[FairAwareness] enabled={_awarenessEnabled} controllerReady={_controllerApi is not null} failed={_awarenessFailed} heard={_heardCount} reacted={_reactCount} smokeMoves={_smokeMoveCount} spacing={_spaceCount}");
        });
        // Never enumerate entities during plugin loading. CSS caches the first
        // entity-list lookup in Lazy<T>, including a premature lookup's error.
        // Only round_freeze_end may open this gate, after world initialization.
        _roundLive=false;
    }

    private void ResetAwareness()
    {
        _roundLive=false; _awareness.Clear(); _smokeClouds.Clear(); _soundDebounce.Clear(); _reservations.Clear(); _awarenessPlayers.Clear();
    }
    private void ForgetAwareness(CCSPlayerController p)
    {
        _awareness.Remove(p.Slot); _soundDebounce.Remove(p.Slot); _reservations.RemoveAll(r=>r.Owner==p.Slot);
    }
    private static AimLocation Loc(Vector v) => new(v.X,v.Y,v.Z);
    private static Vector Vec(AimLocation p) => new(p.X,p.Y,p.Z);
    private static bool Alive(CCSPlayerController p) => p.IsValid && p.PawnIsAlive && p.TeamNum is 2 or 3 && p.PlayerPawn.Value?.AbsOrigin is not null;
    private bool Eligible(CCSPlayerController p, out CCSPlayerPawn pawn)
    {
        pawn=p.PlayerPawn.Value!;
        return Alive(p) && p.IsBot && !p.HasBeenControlledByPlayerThisRound && pawn.IsValid && pawn.Bot is not null;
    }
    private bool Busy(CCSPlayerController p,CCSPlayerPawn pawn)
    {
        if(IsFlashBlind(p) || pawn.IsDefusing || (pawn.Flags&1)==0 || pawn.MoveType==MoveType_t.MOVETYPE_LADDER) return true;
        var weapon=pawn.WeaponServices?.ActiveWeapon.Value?.DesignerName ?? "";
        if(weapon is "weapon_c4" or "weapon_hegrenade" or "weapon_smokegrenade" or "weapon_flashbang" or "weapon_molotov" or "weapon_incgrenade" or "weapon_decoy") return true;
        // Respect grenade/replay locks through the existing provider; no new native hook.
        if(_controllerApi is null) return true;
        return (bool?)_isReplaying!.Invoke(_controllerApi,new object[]{p.Slot})==true ||
            (bool?)_isLocked!.Invoke(_controllerApi,new[]{(object)p.Slot,_lockAll!})==true ||
            (bool?)_isLocked!.Invoke(_controllerApi,new[]{(object)p.Slot,_lockAim!})==true;
    }
    private void ResolveAwarenessApi()
    {
        if(_controllerApi is not null) return;
        try
        {
            // The registry is static per generic T; object would use a different,
            // empty registry. Resolve the provider's actual shared interface type.
            var contract=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("BotControllerApi.IBotControllerApi")).FirstOrDefault(t=>t is not null);
            if(contract is null) return;
            var capType=typeof(PluginCapability<>).MakeGenericType(contract);
            var cap=Activator.CreateInstance(capType,"botcontroller:api");
            var api=capType.GetMethod("Get")!.Invoke(cap,null);
            if(api is null) return;
            var t=api.GetType(); var replay=t.GetMethod("IsReplaying");
            var locked=t.GetMethods().FirstOrDefault(m=>m.Name=="IsLocked" && m.GetParameters().Length==2 && m.GetParameters()[1].ParameterType.IsEnum);
            if(replay is null || locked is null) return;
            _lockAll=Enum.Parse(locked.GetParameters()[1].ParameterType,"All");
            _lockAim=Enum.Parse(locked.GetParameters()[1].ParameterType,"Aim");
            _isLocked=locked; _isReplaying=replay; _controllerApi=api;
        } catch { /* Provider may not yet be loaded. Retry on next 5 Hz update. */ }
    }
    private Awareness GetAwareness(CCSPlayerController p,CCSPlayerPawn pawn,float now)
    {
        if(!_awareness.TryGetValue(p.Slot,out var a) || a.Pawn!=pawn.Handle)
        {
            a=new(){Pawn=pawn.Handle,Position=Loc(pawn.AbsOrigin!),LastProgress=now}; _awareness[p.Slot]=a;
        }
        return a;
    }
    private bool InsideSmoke(AimLocation p) => _smokeClouds.Values.Any(c=>c.Until>=Server.CurrentTime && MathF.Abs(p.Z-c.Center.Z)<128 && AwarenessPolicy.Distance(p,c.Center)<165);
    private static bool Obstructed(AimLocation from,AimLocation to)
    {
        // Bodies do not make their own footsteps inaudible and the listener's
        // own collision hull must not obstruct every hearing/vision probe.
        var trace=Trace.TraceEndShape(Vec(from),Vec(to),options:AwarenessWorldTrace);
        return !float.IsFinite(trace.Fraction) || trace.Fraction<0.97f;
    }
    private void HearPlayer(CCSPlayerController? source,float radius,bool step)
    {
        if(!_awarenessEnabled || _awarenessFailed || !_roundLive || source is null || !Alive(source) || radius<=0) return;
        try
        {
            var now=Server.CurrentTime;
            if(_soundDebounce.TryGetValue(source.Slot,out var last) && now-last<0.12f) return;
            _soundDebounce[source.Slot]=now; var point=Loc(source.PlayerPawn.Value!.AbsOrigin!); var sequence=++_soundSequence;
            foreach(var listener in _awarenessPlayers)
            {
                if(listener.Slot==source.Slot || (!_freeForAll && listener.TeamNum==source.TeamNum) || !Eligible(listener,out var pawn)) continue;
                var ear=Loc(pawn.AbsOrigin!); ear=new(ear.X,ear.Y,ear.Z+pawn.ViewOffset.Z);
                var sound=new AimLocation(point.X,point.Y,point.Z+36);
                if(!AwarenessPolicy.Audible(ear,sound,radius,false,step) || _audioTraceBudget<=0) continue;
                _audioTraceBudget--;
                if(!AwarenessPolicy.Audible(ear,sound,radius,Obstructed(ear,sound),step)) continue;
                var a=GetAwareness(listener,pawn,now);
                if(a.Heard is {} old && now-old.ReadyAt<0.4f) continue;
                a.Heard=AwarenessPolicy.Hear(AwarenessPolicy.Approximate(point,listener.Slot,sequence),now,step,a.Heard);
                _heardCount++;
            }
        } catch(Exception ex) { DisableAwareness(ex); }
    }
    private void HearBomb(int index)
    {
        if(!_awarenessEnabled || _awarenessFailed || !_roundLive) return;
        try
        {
            var bomb=Utilities.GetEntityFromIndex<CPlantedC4>(index);
            if(bomb?.IsValid!=true || !bomb.BombTicking || bomb.AbsOrigin is null) return;
            var point=Loc(bomb.AbsOrigin); var now=Server.CurrentTime;
            foreach(var p in _awarenessPlayers)
            {
                if(p.TeamNum!=3 || !Eligible(p,out var pawn)) continue;
                var ear=Loc(pawn.AbsOrigin!); ear=new(ear.X,ear.Y,ear.Z+pawn.ViewOffset.Z);
                if(!AwarenessPolicy.Audible(ear,point,650,false,false) || _audioTraceBudget<=0) continue;
                _audioTraceBudget--;
                if(!AwarenessPolicy.Audible(ear,point,650,Obstructed(ear,point),false)) continue;
                var a=GetAwareness(p,pawn,now);
                if(!a.ExactBomb && a.Bomb is null) a.Bomb=AwarenessPolicy.Approximate(point,p.Slot,index);
                a.BombUntil=now+6;
            }
        } catch(Exception ex) { DisableAwareness(ex); }
    }

    private void UpdateAwareness()
    {
        if(!_awarenessEnabled || _awarenessFailed || !_roundLive) return;
        try
        {
            var rules=Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault()?.GameRules;
            if(!_roundLive || rules is null || rules.FreezePeriod) { foreach(var a in _awareness.Values) a.Steer=null; return; }
            ResolveAwarenessApi(); _freeForAll=ConVar.Find("mp_teammates_are_enemies")?.GetPrimitiveValue<bool>()==true;
            _awarenessPlayers=Utilities.GetPlayers().Where(Alive).ToList();
            var now=Server.CurrentTime; _reservations.RemoveAll(r=>r.Until<now); _audioTraceBudget=32;
            var plans=0; var bombChecks=0;
            foreach(var key in _awareness.Keys.Where(k=>!_awarenessPlayers.Any(p=>p.Slot==k)).ToList()) _awareness.Remove(key);
            var bomb=rules.BombPlanted ? Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault(b=>b.IsValid && b.BombTicking) : null;
            foreach(var p in _awarenessPlayers.OrderBy(p=>p.Slot))
            {
                if(!Eligible(p,out var pawn)) continue;
                var a=GetAwareness(p,pawn,now); var bot=pawn.Bot!; var pos=Loc(pawn.AbsOrigin!);
                // Do not learn visual bomb/enemy information or steer/look for a blinded bot.
                // Sound snapshots can still arrive independently through event handlers.
                if(IsFlashBlind(p)) { a.Steer=null; continue; }
                if(AwarenessPolicy.Distance(pos,a.Position)>12) { a.LastProgress=now; a.Position=pos; }
                if(pawn.AbsVelocity.Length2D()>40)
                {
                    var goal=Loc(bot.GoalPosition);
                    if(goal.Finite && goal!=default && AwarenessPolicy.Distance(goal,pos)>96 && AwarenessPolicy.Distance(goal,pos)<2048) { a.NativeGoal=goal; a.GoalUntil=now+5; }
                }
                if(bomb is null) { a.Bomb=null; a.ExactBomb=false; }
                else if(p.TeamNum==3 && bombChecks<3 && ((p.Slot+(int)(now*5))%3)==0) { RememberVisibleBomb(pawn,a,bomb,now); bombChecks++; }
                var concealed=InsideSmoke(new(pos.X,pos.Y,pos.Z+pawn.ViewOffset.Z*0.6f));
                var visible=bot.IsEnemyVisible && !(concealed && _smokeSamples.TryGetValue(bot.Handle,out var sample) && sample.Blocked);
                var busy=Busy(p,pawn);
                if(!AwarenessPolicy.CanAssist(_roundLive,busy,p.HasBeenControlledByPlayerThisRound,(pawn.Flags&1)!=0,visible)) { a.Steer=null; continue; }
                if(a.Heard is {} heard && heard.Ready(now) && now-a.LastReacted>0.65f)
                {
                    var eye=new AimLocation(pos.X,pos.Y,pos.Z+pawn.ViewOffset.Z); var aim=AwarenessPolicy.Bearing(eye,heard.Position);
                    SetPoint(bot.LookAtSpot,aim); bot.LookAtSpotTimestamp=now; bot.LookAtSpotDuration=0.65f; bot.LookAtSpotAttack=false;
                    bot.LookAtSpotClearIfClose=false; bot.LookAtSpotAngleTolerance=18;
                    SetPoint(bot.NoisePosition,heard.Position); bot.NoiseTimestamp=now; bot.NoiseTravelDistance=AwarenessPolicy.Distance(pos,heard.Position);
                    a.LastReacted=now; _reactCount++;
                }
                if(plans>=3 || now<a.NextMove || a.SteerUntil>now || now-a.LastProgress<0.85f || pawn.AbsVelocity.Length2D()>24) continue;
                AimLocation? wanted=null; string reason="";
                if(!_freeForAll)
                {
                    var ally=_awarenessPlayers.FirstOrDefault(other=>other.Slot!=p.Slot && other.TeamNum==p.TeamNum &&
                        MathF.Abs(other.PlayerPawn.Value!.AbsOrigin!.Z-pos.Z)<42 && AwarenessPolicy.Distance(Loc(other.PlayerPawn.Value.AbsOrigin),pos)<52 &&
                        AwarenessPolicy.Yields(p.Slot,other.Slot,!other.IsBot,other.PlayerPawn.Value.IsDefusing));
                    if(ally is not null)
                    {
                        var other=Loc(ally.PlayerPawn.Value!.AbsOrigin!); var dx=pos.X-other.X; var dy=pos.Y-other.Y;
                        if(dx*dx+dy*dy<1) { var yaw=(p.Slot*137)%360*MathF.PI/180; dx=MathF.Cos(yaw);dy=MathF.Sin(yaw); }
                        var len=MathF.Sqrt(dx*dx+dy*dy); wanted=new(pos.X+dx/len*96,pos.Y+dy/len*96,pos.Z); reason="space";
                    }
                }
                if(wanted is null && concealed)
                {
                    if(p.TeamNum==3 && a.Bomb is {} known && a.BombUntil>now && AwarenessPolicy.Distance(pos,known)>48) wanted=known;
                    else if(a.Heard is {} cue && cue.Ready(now) && AwarenessPolicy.Distance(pos,cue.Position)>72) wanted=cue.Position;
                    else if(a.NativeGoal is {} goal && a.GoalUntil>now) wanted=goal;
                    else
                    {
                        var cloud=_smokeClouds.Values.OrderBy(c=>AwarenessPolicy.Distance(c.Center,pos)).FirstOrDefault();
                        var dx=pos.X-cloud.Center.X;var dy=pos.Y-cloud.Center.Y;
                        var dir=SmokeAimPolicy.Direction(dx*100,dy*100,pawn.EyeAngles.Y);
                        wanted=new(pos.X+dir.X*160,pos.Y+dir.Y*160,pos.Z);
                    }
                    reason="smoke";
                }
                else if(wanted is null && a.Heard is {} cue && cue.Ready(now) && AwarenessPolicy.Distance(pos,cue.Position)>96) { wanted=cue.Position; reason="sound"; }
                if(wanted is {} destination)
                {
                    plans++;
                    if(ChooseStep(p,pawn,pos,destination,out var step))
                    {
                        a.Steer=step; a.SteerUntil=now+0.6f; a.NextMove=now+1f;
                        _reservations.RemoveAll(r=>r.Owner==p.Slot); _reservations.Add((p.Slot,step,now+1.5f));
                        if(reason=="smoke") _smokeMoveCount++; else if(reason=="space") _spaceCount++;
                    }
                    else a.NextMove=now+0.6f;
                }
            }
        } catch(Exception ex) { DisableAwareness(ex); }
    }
    private static void SetPoint(Vector destination,AimLocation p) { destination.X=p.X; destination.Y=p.Y; destination.Z=p.Z; }
    private void RememberVisibleBomb(CCSPlayerPawn pawn,Awareness a,CPlantedC4 bomb,float now)
    {
        if(bomb.AbsOrigin is null || _smokeDensity is null) return;
        var eye=new AimLocation(pawn.AbsOrigin!.X,pawn.AbsOrigin.Y,pawn.AbsOrigin.Z+pawn.ViewOffset.Z); var point=Loc(bomb.AbsOrigin);
        if(AwarenessPolicy.Distance(eye,point)>1000) return;
        var yaw=MathF.Atan2(point.Y-eye.Y,point.X-eye.X)*180/MathF.PI;
        var delta=MathF.Abs(MathF.IEEERemainder(yaw-pawn.EyeAngles.Y,360));
        if(delta>55 || MathF.Abs(point.Z-eye.Z)>500 || Obstructed(eye,new(point.X,point.Y,point.Z+8))) return;
        var density=SmokeDensity(Vec(eye),new(point.X,point.Y,point.Z+8));
        if(!float.IsFinite(density) || density<0 || density>=SmokeAimPolicy.Threshold) return;
        a.Bomb=point; a.ExactBomb=true; a.BombUntil=now+60;
    }
    private bool ChooseStep(CCSPlayerController owner,CCSPlayerPawn pawn,AimLocation pos,AimLocation wanted,out AimLocation result)
    {
        result=pos; var best=float.NegativeInfinity; var direction=SmokeAimPolicy.Direction((wanted.X-pos.X)*100,(wanted.Y-pos.Y)*100,pawn.EyeAngles.Y);
        foreach(var angle in new[]{0f,-45,45,-90,90,135,-135,180})
        {
            var rad=angle*MathF.PI/180; var dx=direction.X*MathF.Cos(rad)-direction.Y*MathF.Sin(rad); var dy=direction.X*MathF.Sin(rad)+direction.Y*MathF.Cos(rad);
            var candidate=new AimLocation(pos.X+dx*72,pos.Y+dy*72,pos.Z);
            if(_awarenessPlayers.Any(p=>p.Slot!=owner.Slot && (!_freeForAll && p.TeamNum==owner.TeamNum) && MathF.Abs(p.PlayerPawn.Value!.AbsOrigin!.Z-pos.Z)<48 && AwarenessPolicy.Distance(Loc(p.PlayerPawn.Value.AbsOrigin),candidate)<48)) continue;
            if(_reservations.Any(r=>r.Owner!=owner.Slot && AwarenessPolicy.Distance(r.Point,candidate)<48)) continue;
            var hull=Trace.TraceHullShape(new(pos.X,pos.Y,pos.Z+2),new(candidate.X,candidate.Y,candidate.Z+2),new(-16,-16,0),new(16,16,70),pawn,AwarenessTrace);
            if(!float.IsFinite(hull.Fraction) || hull.IsAllSolid || hull.Fraction<0.98f) continue;
            var floor=Trace.TraceEndShape(new(candidate.X,candidate.Y,candidate.Z+16),new(candidate.X,candidate.Y,candidate.Z-24),pawn,AwarenessTrace);
            if(!float.IsFinite(floor.Fraction) || floor.IsAllSolid || floor.Fraction>=1 || floor.Normal.Z<0.7f) continue;
            var score=MathF.Cos(rad)*72;
            if(score>best) { best=score; result=candidate; }
        }
        return float.IsFinite(best);
    }
    private void SteerAwareness()
    {
        if(!_awarenessEnabled || _awarenessFailed || !_roundLive) return;
        try
        {
            var now=Server.CurrentTime;
            foreach(var p in _awarenessPlayers)
            {
                if(!_awareness.TryGetValue(p.Slot,out var a) || a.Steer is not {} goal || now>a.SteerUntil || !Eligible(p,out var pawn)) continue;
                var bot=pawn.Bot!;
                var hidden=_smokeSamples.TryGetValue(bot.Handle,out var sample) && sample.Blocked && now-sample.Time<0.2f;
                if(pawn.Handle!=a.Pawn || Busy(p,pawn) || (bot.IsEnemyVisible && !hidden)) { a.Steer=null; continue; }
                var pos=Loc(pawn.AbsOrigin!); var distance=AwarenessPolicy.Distance(pos,goal);
                if(distance<18) { a.Steer=null; continue; }
                var vx=(goal.X-pos.X)/distance*110;var vy=(goal.Y-pos.Y)/distance*110;
                // Short, bounded ground acceleration. No position teleport or path rewrite.
                var ax=vx-pawn.AbsVelocity.X;var ay=vy-pawn.AbsVelocity.Y; var length=MathF.Sqrt(ax*ax+ay*ay);
                var limit=600f*Server.TickInterval; var scale=length>limit ? limit/length : 1;
                pawn.AbsVelocity.X+=ax*scale; pawn.AbsVelocity.Y+=ay*scale;
            }
        } catch(Exception ex) { DisableAwareness(ex); }
    }
    private bool HeardBlindAim(CCSPlayerController p,Vector eye,out AimLocation aim)
    {
        aim=default;
        if(!_awarenessEnabled || _awarenessFailed || !_roundLive || !_awareness.TryGetValue(p.Slot,out var a) || a.Pawn!=p.PlayerPawn.Value?.Handle || a.Heard is not {} cue || !cue.Ready(Server.CurrentTime)) return false;
        aim=AwarenessPolicy.Bearing(Loc(eye),cue.Position); return true;
    }
    private void DisableAwareness(Exception ex)
    {
        if(_awarenessFailed) return; _awarenessFailed=true;
        foreach(var a in _awareness.Values) a.Steer=null;
        Logger.LogError(ex,"[FairAwareness] Disabled after an error; native behavior retained. Restart after reviewing the log.");
    }
}
