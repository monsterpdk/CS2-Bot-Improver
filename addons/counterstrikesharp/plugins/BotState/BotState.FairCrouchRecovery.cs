using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Fairplay.Diagnostics;
using BotControllerApi;
using System.Text.Json;

namespace BotState;

public partial class BotState
{
    private bool _fairRoundLive;
    private bool _crouchFailed;
    private int _recoveryTraceFrame=-1,_recoveryTraceRemaining;
    private DiagnosticLog? _crouchLog;
    private long _crouchChecks,_crouchPlans,_crouchJumps,_crouchFinished,_crouchAborted;
    private readonly Dictionary<int,RecoveryEntry> _crouchEntries=new();
    private sealed class RecoveryEntry
    {
        public nint Pawn;
        public RecoveryPoint Anchor,BudgetAnchor,StartPosition;
        public readonly List<CrouchJumpPlan> FailedDetours=new();
        public float Since,NextTry;
        public int Attempts;
        public CrouchJumpPlan? Plan,PreviousDetour;
        public float Started,NextSample;
        public bool Jumped;
        public long DuckToken,JumpToken,MovementToken;
        public IBotControllerApi? Owner;
    }
    private static readonly TraceOptions RecoveryTrace=new(){InteractsWith=Masks.PlayerSolid};
    private static RecoveryPoint RecoveryPosition(CCSPlayerPawn pawn)
    {var p=pawn.AbsOrigin!;return new(p.X,p.Y,p.Z);}
    private static CCSPlayer_MovementServices? RecoveryMovementServices(CPlayer_MovementServices? services)
        =>services is null||services.Handle==0?null:new CCSPlayer_MovementServices(services.Handle);

    private void InitFairCrouchRecovery()
    {
        _crouchLog=new(ModuleDirectory,"crouch");
        AddCommand("bot_crouch_diagnostics","Detailed recovery logs: on/off (default off)",(_,info)=>
        {
            var value=info.ArgCount>1?info.GetArg(1):"";
            if(value is not ("on" or "off")){info.ReplyToCommand("Usage: bot_crouch_diagnostics on|off");return;}
            _crouchLog.SetEnabled(value=="on");info.ReplyToCommand($"[CrouchRecovery] diagnostics={_crouchLog.Enabled}");
        });
        AddCommand("bot_crouch_recovery","Show bounded crouch-jump recovery counters",(_,info)=>
            info.ReplyToCommand($"[CrouchRecovery] version={ModuleVersion} enabled={!_crouchFailed} apiReady={_botController is IBotControllerApi} live={_fairRoundLive} checks={_crouchChecks} plans={_crouchPlans} jumps={_crouchJumps} finished={_crouchFinished} aborted={_crouchAborted} log={_crouchLog.Path}"));
    }
    private void ResetFairCrouchRecovery()
    {
        _fairRoundLive=false;
        foreach(var pair in _crouchEntries)CancelRecovery(pair.Key,pair.Value);
        _crouchEntries.Clear();
    }
    private void MaintainRecoveryLeases(float now)
    {
        foreach(var pair in _crouchEntries)
        {
            var entry=pair.Value;if(entry.Plan is null)continue;
            try
            {
                var p=Utilities.GetPlayerFromSlot(pair.Key);
                bool invalid=p is null||!p.IsValid||!p.IsBot||!p.PawnIsAlive||p.HasBeenControlledByPlayerThisRound||p.PlayerPawn.Value?.Handle!=entry.Pawn;
                if(invalid||now<entry.Started||now-entry.Started>CrouchJumpPolicy.LeaseSeconds(entry.Plan.Value))
                {
                    RecoveryLog(invalid?"abort_owner":"lease_end",pair.Key,entry);
                    CancelRecovery(pair.Key,entry);
                    if(invalid)_crouchAborted++;else _crouchFinished++;
                }
            }
            catch(Exception ex){CancelRecovery(pair.Key,entry);_crouchAborted++;_crouchLog?.Error(ex);}
        }
    }
    private static void CancelRecovery(int slot,RecoveryEntry entry)
    {
        // Cancel only our own tokens, never unlock or clear another plugin's work.
        try
        {
            if(entry.Owner is {} api)
            {
                try { if(entry.DuckToken>0)api.CancelUsercmdInjection(slot,entry.DuckToken); } catch { }
                try { if(entry.JumpToken>0)api.CancelUsercmdInjection(slot,entry.JumpToken); } catch { }
                try { if(entry.MovementToken>0)api.CancelUsercmdMovement(slot,entry.MovementToken); } catch { }
            }
        }
        finally {entry.Plan=null;entry.Owner=null;entry.DuckToken=entry.JumpToken=entry.MovementToken=0;}
    }
    private bool FairMovementBusy(CCSPlayerController player,CCSPlayerPawn pawn)
    {
        if(!_fairRoundLive||_isFreezeTime||!player.PawnIsAlive||player.HasBeenControlledByPlayerThisRound||pawn.IsDefusing||pawn.IsGrabbingHostage||pawn.BlindUntilTime>Server.CurrentTime)return true;
        if(pawn.HostageServices?.CarriedHostage.IsValid==true)return true;
        if(pawn.WeaponServices?.ActiveWeapon.Value is {} weapon&&weapon.DesignerName=="weapon_c4"&&weapon.As<CC4>().StartedArming)return true;
        if(_botController is not IBotControllerApi api)return true;
        return api.IsLocked(player.Slot,LockKind.All)||api.IsLocked(player.Slot,LockKind.Aim)||api.IsReplaying(player.Slot)||api.GetWeaponLock(player.Slot)!=LockTarget.None;
    }
    private RecoveryHit RecoverySweep(CCSPlayerPawn pawn,RecoveryPoint from,RecoveryPoint to,float height)
    {
        if(_recoveryTraceFrame!=Server.TickCount){_recoveryTraceFrame=Server.TickCount;_recoveryTraceRemaining=32;}
        if(_recoveryTraceRemaining--<=0)return new(0,true,0,false);
        var hit=Trace.TraceHullShape(new(from.X,from.Y,from.Z),new(to.X,to.Y,to.Z),new(-16,-16,0),new(16,16,height),pawn,RecoveryTrace);
        bool actor=false;
        if(hit.Fraction<1)
        {
            var entity=hit.HitEntity();
            if(entity.Handle!=0&&entity.IsValid)
            {
                var name=entity.DesignerName;
                actor=name.StartsWith("cs_player",StringComparison.Ordinal)||name.Contains("hostage",StringComparison.Ordinal)||name.Contains("door",StringComparison.Ordinal);
            }
        }
        return new(hit.Fraction,hit.IsAllSolid,hit.Normal.Z,actor);
    }
    private static (float Forward,float Left) RecoveryMovement(CCSPlayerPawn pawn,CrouchJumpPlan plan)
    {
        var yaw=pawn.EyeAngles.Y*MathF.PI/180;
        float speed=plan.Rise==0?0.55f:0.8f;
        return (speed*(plan.DirX*MathF.Cos(yaw)+plan.DirY*MathF.Sin(yaw)),
                speed*(-plan.DirX*MathF.Sin(yaw)+plan.DirY*MathF.Cos(yaw)));
    }
    private void RecoveryLog(string action,int slot,RecoveryEntry entry)
    {
        if(_crouchLog?.Enabled!=true)return;
        try
        {
            // Only event records and bounded samples during a recovery lease.
            // Include actual movement, rather than treating lease completion as escape.
            var player=Utilities.GetPlayerFromSlot(slot);
            var pawn=player?.PlayerPawn.Value;
            object? actual=null;
            if(pawn is {IsValid:true}&&pawn.Handle==entry.Pawn&&pawn.AbsOrigin is not null)
            {
                var pos=RecoveryPosition(pawn);
                var velocity=pawn.AbsVelocity;
                var movement=RecoveryMovementServices(pawn.MovementServices);
                var bot=pawn.Bot;
                var goal=bot?.GoalPosition;
                var input=entry.Plan is {} plan?RecoveryMovement(pawn,plan):(0f,0f);
                actual=new{position=pos,velocity=new{x=velocity.X,y=velocity.Y,z=velocity.Z},
                    yaw=pawn.EyeAngles.Y,duck=movement?.DuckAmount,grounded=pawn.GroundEntity.IsValid,
                    crouching=bot?.IsCrouching,running=bot?.IsRunning,stuck=bot?.IsStuck,
                    goal=goal is null?(RecoveryPoint?)null:new RecoveryPoint(goal.X,goal.Y,goal.Z),
                    targetDistance=entry.Plan is {} target?(float?)pos.HorizontalDistance(target.Target):null,
                    forward=input.Item1,left=input.Item2,entry.MovementToken,entry.DuckToken,entry.JumpToken};
            }
            _crouchLog.Append(JsonSerializer.Serialize(new{utc=DateTime.UtcNow,version=ModuleVersion,map=Server.MapName,time=Server.CurrentTime,action,slot,name=player?.PlayerName,entry.Attempts,entry.Anchor,entry.Plan,actual}));
        }
        catch(Exception ex){_crouchLog.Error(ex);_crouchLog.SetEnabled(false);}
    }
    private bool UpdateFairCrouchRecovery(CCSPlayerController player,CCSPlayerPawn pawn,CCSBot bot,int idx,float now,bool attacking,bool doorCooldown)
    {
        if(_crouchFailed)return false;
        int slot=player.Slot;
        try
        {
            if(pawn.AbsOrigin is null)return false;
            var pos=RecoveryPosition(pawn);
            if(!_crouchEntries.TryGetValue(slot,out var entry)||entry.Pawn!=pawn.Handle)
            {
                if(entry is not null)CancelRecovery(slot,entry);
                _crouchEntries[slot]=entry=new(){Pawn=pawn.Handle,Anchor=pos,BudgetAnchor=pos,Since=now};
            }
            if(attacking||bot.IsEnemyVisible||doorCooldown||FairMovementBusy(player,pawn)||pawn.MoveType!=MoveType_t.MOVETYPE_WALK)
            {
                if(entry.Plan is not null){RecoveryLog("abort_busy",slot,entry);CancelRecovery(slot,entry);_crouchAborted++;}
                entry.Since=now;return false;
            }
            if(entry.Plan is {} active)
            {
                if(now>=entry.NextSample){entry.NextSample=now+0.2f;RecoveryLog("progress",slot,entry);}
                if(active.Rise==0)
                {
                    float progress=CrouchJumpPolicy.ForwardProgress(entry.StartPosition,pos,active);
                    if(progress>=8)entry.PreviousDetour=active;
                    if(CrouchJumpPolicy.IsStalled(now-entry.Started,entry.StartPosition,pos,active))
                    {
                        entry.FailedDetours.Add(active);entry.PreviousDetour=null;
                        RecoveryLog("detour_no_progress",slot,entry);CancelRecovery(slot,entry);_crouchAborted++;return false;
                    }
                    float remaining=(active.Target.X-pos.X)*active.DirX+(active.Target.Y-pos.Y)*active.DirY;
                    var ahead=new RecoveryPoint(pos.X+active.DirX*12,pos.Y+active.DirY*12,pos.Z+2);
                    var floor=RecoverySweep(pawn,ahead,new(ahead.X,ahead.Y,pos.Z-4),CrouchJumpPolicy.StandingHeight);
                    bool safe=pawn.GroundEntity.IsValid&&RecoverySweep(pawn,new(pos.X,pos.Y,pos.Z+2),ahead,CrouchJumpPolicy.StandingHeight).Clear
                        &&!floor.StartSolid&&!floor.ActorOrDoor&&float.IsFinite(floor.Fraction)&&floor.Fraction>=0&&floor.Fraction<1&&float.IsFinite(floor.NormalZ)&&floor.NormalZ>=0.7f;
                    if(remaining<=10||!safe){RecoveryLog(remaining<=10?"detour_end":"detour_blocked",slot,entry);CancelRecovery(slot,entry);return false;}
                }
                if(now<entry.Started||now-entry.Started>CrouchJumpPolicy.LeaseSeconds(active))
                {RecoveryLog("lease_end",slot,entry);CancelRecovery(slot,entry);_crouchFinished++;return false;}
                if(!entry.Jumped&&now-entry.Started>=0.18f)
                {
                    // MovementServices is a base schema wrapper, even for a CS pawn.
                    // Rewrap its native handle; a managed subtype test always rejects it.
                    var movement=RecoveryMovementServices(pawn.MovementServices);
                    if(movement is null||movement.DuckAmount<0.75f)
                    {
                        if(now-entry.Started>0.45f){RecoveryLog("duck_unavailable",slot,entry);CancelRecovery(slot,entry);_crouchAborted++;return false;}
                        return true;
                    }
                    if(!pawn.GroundEntity.IsValid||!RecoverySweep(pawn,pos,new(pos.X,pos.Y,pos.Z+active.Rise),CrouchJumpPolicy.CrouchingHeight).Clear)
                    {CancelRecovery(slot,entry);_crouchAborted++;return false;}
                    entry.JumpToken=entry.Owner!.InjectUsercmd(slot,(ulong)PlayerButtons.Jump,100);
                    if(entry.JumpToken<=0){CancelRecovery(slot,entry);_crouchAborted++;return false;}
                    var move=RecoveryMovement(pawn,active);
                    entry.MovementToken=entry.Owner.StartUsercmdMovement(slot,move.Forward,move.Left);
                    if(entry.MovementToken<=0){CancelRecovery(slot,entry);_crouchAborted++;return false;}
                    entry.Jumped=true;_crouchJumps++;RecoveryLog("jump",slot,entry);
                }
                if(entry.MovementToken>0)
                {var move=RecoveryMovement(pawn,active);if(!entry.Owner!.UpdateUsercmdMovement(slot,entry.MovementToken,move.Forward,move.Left)){CancelRecovery(slot,entry);return false;}}
                return true;
            }
            if(pos.HorizontalDistance(entry.BudgetAnchor)>64){entry.BudgetAnchor=pos;entry.Attempts=0;entry.FailedDetours.Clear();entry.PreviousDetour=null;}
            if(pos.HorizontalDistance(entry.Anchor)>24){entry.Anchor=pos;entry.Since=now;}
            if(!bot.IsStuck||!pawn.GroundEntity.IsValid||now<entry.NextTry||now-entry.Since<3||entry.Attempts>=CrouchJumpPolicy.MaxAttempts)return false;
            entry.NextTry=now+5;_crouchChecks++;
            var goal=bot.GoalPosition;
            var destination=new RecoveryPoint(goal.X,goal.Y,goal.Z);
            int side=CrouchJumpPolicy.PreferredSide(pos,destination,entry.Attempts,entry.PreviousDetour);
            var plan=now-entry.Since>=8?CrouchJumpPolicy.FindDetour(pos,destination,side,(a,b,height)=>RecoverySweep(pawn,a,b,height),entry.PreviousDetour,entry.FailedDetours):null;
            plan??=CrouchJumpPolicy.Find(pos,destination,(a,b,height)=>RecoverySweep(pawn,a,b,height));
            if(plan is null){if(_recoveryTraceRemaining<=0)entry.NextTry=now+0.2f;else RecoveryLog("no_safe_route",slot,entry);return false;}
            entry.Attempts++;entry.Plan=plan;entry.StartPosition=pos;entry.Started=now;entry.NextSample=now+0.2f;entry.Jumped=false;entry.Owner=(IBotControllerApi)_botController!;
            if(plan.Value.Rise==0)
            {
                var move=RecoveryMovement(pawn,plan.Value);
                entry.MovementToken=entry.Owner.StartUsercmdMovement(slot,move.Forward,move.Left);
                if(entry.MovementToken<=0){CancelRecovery(slot,entry);_crouchAborted++;return false;}
                entry.Jumped=true;_crouchPlans++;RecoveryLog("detour",slot,entry);return true;
            }
            entry.DuckToken=entry.Owner.InjectUsercmd(slot,(ulong)PlayerButtons.Duck,950);
            if(entry.DuckToken<=0){CancelRecovery(slot,entry);_crouchAborted++;return false;}
            _crouchPlans++;RecoveryLog("prepare",slot,entry);return true;
        }
        catch(Exception ex)
        {
            _crouchFailed=true;_crouchLog?.Error(ex);
            foreach(var pair in _crouchEntries){try{CancelRecovery(pair.Key,pair.Value);}catch{}}
            Logger.LogError(ex,"[CrouchRecovery] Disabled after error; native movement retained");return false;
        }
    }
}
