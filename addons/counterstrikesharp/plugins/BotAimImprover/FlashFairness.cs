using Fairplay.Diagnostics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
namespace BotAimImprover;

public partial class BotAimImprover
{
    // CCSPlayerPawn::Blind(float hold, float fade, float alpha), Windows 2000939.
    private const string PawnBlindSignature="40 53 48 83 EC 50 80 B9 ? ? ? ? 00 48 8B D9 0F 29 74 24 ? 0F 28 F3 0F 29 7C 24 ? 0F 28 FA";
    private MemoryFunctionVoid<IntPtr,float,float,float>? _pawnBlind;
    private readonly Dictionary<nint,float> _flashUntil=new();
    private readonly Dictionary<nint,AimLocation> _flashHeldAim=new();
    private readonly List<string> _flashLogPending=new();
    private string _flashLogPath="";
    private DiagnosticLog _flashDiagnostics=null!;
    private int _flashLogRows;
    private bool _flashLogError;
    private long _flashBotEvents,_flashHumanEvents,_flashZeroEvents,_flashNativeCalls,_flashNativePositive,_flashAimSkipped,_flashShots,_flashKills;
    private float _lastBotFlashDuration,_lastNativeFlashDuration,_lastPositiveNativeDuration,_lastPositiveFlashDuration;

    private void LoadFlashFairness()
    {
        _flashDiagnostics=new(ModuleDirectory,"flash");_flashLogPath=_flashDiagnostics.Path;
        AddCommand("bot_flash_diagnostics","Detailed flash logs: on/off (default off)",(_,info)=>
        {
            var value=info.ArgCount>1?info.GetArg(1):"";
            if(value is not ("on" or "off")){info.ReplyToCommand("Usage: bot_flash_diagnostics on|off");return;}
            FlushFlashLog();_flashDiagnostics.SetEnabled(value=="on");_flashLogRows=0;
            info.ReplyToCommand($"[FlashFairness] diagnostics={_flashDiagnostics.Enabled} maxBytes={2*DiagnosticLog.FileLimit}");
        });
        _pawnBlind=new(PawnBlindSignature);
        if(_pawnBlind.Handle==IntPtr.Zero) throw new InvalidOperationException("Audited pawn Blind function missing.");
        _pawnBlind.Hook(OnPawnBlindPost,HookMode.Post);
        RegisterEventHandler<EventPlayerBlind>((ev,_) =>
        {
            if(ev.Userid is not { IsValid:true } p || p.PlayerPawn.Value is not { IsValid:true } pawn) return HookResult.Continue;
            if(p.IsBot)
            {
                _flashBotEvents++;_lastBotFlashDuration=ev.BlindDuration;
                if(!float.IsFinite(ev.BlindDuration) || ev.BlindDuration<=0) _flashZeroEvents++;
                else _lastPositiveFlashDuration=ev.BlindDuration;
                ExtendFlash(pawn,FlashPolicy.Deadline(Server.CurrentTime,ev.BlindDuration));
                LogFlash(new {kind="event",time=Server.CurrentTime,slot=p.Slot,entity=ev.Entityid,
                    attackerSlot=ev.Attacker?.Slot,duration=SafeFloat(ev.BlindDuration),
                    nativeRemaining=SafeFloat(pawn.BlindUntilTime-Server.CurrentTime),
                    visualDuration=SafeFloat(pawn.FlashDuration),alpha=SafeFloat(pawn.FlashMaxAlpha)});
            }
            else _flashHumanEvents++;
            return HookResult.Continue;
        });
        RegisterEventHandler<EventWeaponFire>((ev,_)=>
        {
            if(ev.Userid is {} p && IsFlashBlind(p))
            {
                _flashShots++;
                LogFlash(new {kind="shot",time=Server.CurrentTime,slot=p.Slot,weapon=ev.Weapon});
            }
            return HookResult.Continue;
        });
        RegisterEventHandler<EventPlayerDeath>((ev,_)=>
        {
            if(ev.Attacker is {} attacker && IsFlashBlind(attacker))
            {
                _flashKills++;
                LogFlash(new {kind="kill",time=Server.CurrentTime,slot=attacker.Slot,weapon=ev.Weapon,nativeBlindIcon=ev.Attackerblind});
            }
            if(ev.Userid?.PlayerPawn.Value is {} pawn) { _flashUntil.Remove(pawn.Handle);_flashHeldAim.Remove(pawn.Handle); }
            return HookResult.Continue;
        });
        RegisterEventHandler<EventRoundStart>((_,_)=>{ClearFlashWindows();return HookResult.Continue;});
        RegisterEventHandler<EventRoundEnd>((_,_)=>{FlushFlashLog();return HookResult.Continue;});
        RegisterListener<Listeners.OnMapStart>(_=>
        {
            FlushFlashLog();ClearFlashWindows();
            _flashBotEvents=0;_flashHumanEvents=0;_flashZeroEvents=0;_flashNativeCalls=0;_flashNativePositive=0;_flashAimSkipped=0;_flashShots=0;_flashKills=0;
            _lastBotFlashDuration=0;_lastNativeFlashDuration=0;_lastPositiveNativeDuration=0;_lastPositiveFlashDuration=0;
        });
        RegisterListener<Listeners.OnMapEnd>(()=>{FlushFlashLog();ClearFlashWindows();});
        AddCommand("bot_flash_status","Show native blind calls, event durations and test log path",(_,info)=>
        {
            FlushFlashLog();
            info.ReplyToCommand($"[FlashFairness] version={ModuleVersion} nativeBlindCalls={_flashNativeCalls} nativePositive={_flashNativePositive} botBlindEvents={_flashBotEvents} zeroDurationEvents={_flashZeroEvents} humanBlindEvents={_flashHumanEvents} heldAimUpdates={_flashAimSkipped} shotsWhileBlind={_flashShots} killsWhileBlind={_flashKills} immunity=OFF");
            info.ReplyToCommand($"[FlashFairness] lastEventDuration={_lastBotFlashDuration:F3} lastNativeRemaining={_lastNativeFlashDuration:F3} lastPositiveNativeDuration={_lastPositiveNativeDuration:F3} lastPositiveEventDuration={_lastPositiveFlashDuration:F3}");
            info.ReplyToCommand($"[FlashFairness] diagnostics={_flashDiagnostics.Enabled} log={_flashLogPath} rows={_flashLogRows}/2048 diagnosticError={_flashLogError}");
        });
    }
    private static float? SafeFloat(float value)=>float.IsFinite(value)?value:null;
    private void ClearFlashWindows(){_flashUntil.Clear();_flashHeldAim.Clear();}
    private void ExtendFlash(CCSPlayerPawn pawn,float until)
    {
        var now=Server.CurrentTime;
        if(!float.IsFinite(until) || until<=now) return;
        _flashUntil.TryGetValue(pawn.Handle,out var previous);
        if(previous<=now || !_flashHeldAim.ContainsKey(pawn.Handle))
        {
            if(pawn.AbsOrigin is {} pos && pawn.EyeAngles is {} angles)
            {
                float yaw=angles.Y*MathF.PI/180f;
                var held=new AimLocation(pos.X+MathF.Cos(yaw)*768,pos.Y+MathF.Sin(yaw)*768,pos.Z+pawn.ViewOffset.Z);
                if(held.Finite) _flashHeldAim[pawn.Handle]=held;
            }
        }
        if(until>previous) _flashUntil[pawn.Handle]=until;
    }
    private HookResult OnPawnBlindPost(DynamicHook hook)
    {
        try
        {
            var ptr=hook.GetParam<IntPtr>(0);
            var p=Fairplay.Runtime.LivePlayers.Get().FirstOrDefault(p=>p.IsValid && p.IsBot && !p.HasBeenControlledByPlayerThisRound && p.PawnIsAlive && p.PlayerPawn.Value?.Handle==ptr);
            if(p?.PlayerPawn.Value is not {} pawn) return HookResult.Continue;
            var now=Server.CurrentTime;var hold=hook.GetParam<float>(1);var fade=hook.GetParam<float>(2);var alpha=hook.GetParam<float>(3);
            var until=FlashPolicy.NativeDeadline(now,hold,alpha,pawn.BlindUntilTime);
            _flashNativeCalls++;_lastNativeFlashDuration=MathF.Max(0,until-now);
            if(until>now){_flashNativePositive++;_lastPositiveNativeDuration=until-now;}
            ExtendFlash(pawn,until);
            LogFlash(new {kind="native",time=now,slot=p.Slot,hold=SafeFloat(hold),fade=SafeFloat(fade),alpha=SafeFloat(alpha),
                nativeRemaining=SafeFloat(pawn.BlindUntilTime-now),guardRemaining=SafeFloat(until-now),visualDuration=SafeFloat(pawn.FlashDuration)});
        }
        catch(Exception ex)
        {
            if(!_flashLogError){_flashLogError=true;_flashDiagnostics.Error(ex);Logger.LogError(ex,"[FlashFairness] Native blind observation failed");}
        }
        return HookResult.Continue;
    }
    private bool IsFlashBlind(CCSPlayerController p)
    {
        if(!p.IsValid || !p.IsBot || !p.PawnIsAlive || p.HasBeenControlledByPlayerThisRound || p.PlayerPawn.Value is not { IsValid:true } pawn) return false;
        _flashUntil.TryGetValue(pawn.Handle,out var until);
        return FlashPolicy.Active(Server.CurrentTime,pawn.BlindUntilTime,until);
    }
    private unsafe void HoldFlashAim(nint bot,CCSPlayerController controller)
    {
        var pawn=controller.PlayerPawn.Value;
        if(pawn is null) return;
        if(!_flashHeldAim.TryGetValue(pawn.Handle,out var held))
        {
            ExtendFlash(pawn,pawn.BlindUntilTime);
            if(!_flashHeldAim.TryGetValue(pawn.Handle,out held))return;
        }
        // Undo the native function's newly computed live target point as well.
        var native=new CCSBot(bot);native.IsEnemyVisible=false;
        var point=native.TargetSpot;point.X=held.X;point.Y=held.Y;point.Z=held.Z;
    }
    private void LogFlash(object row)
    {
        if(!_flashDiagnostics.Enabled || _flashLogRows>=2048)return;
        _flashLogPending.Add(JsonSerializer.Serialize(new {map=Server.MapName,data=row}));_flashLogRows++;
        if(_flashLogPending.Count>=64)FlushFlashLog();
    }
    private void FlushFlashLog()
    {
        if(_flashLogPending.Count==0)return;
        try
        {
            foreach(var row in _flashLogPending)_flashDiagnostics.Append(row);_flashLogPending.Clear();
        }
        catch(Exception ex)
        {
            if(!_flashLogError){_flashLogError=true;_flashDiagnostics.Error(ex);Logger.LogWarning(ex,"[FlashFairness] Could not save bounded test log");}
            _flashLogPending.Clear();
        }
    }
    private void UnloadFlashFairness()
    {
        try{_pawnBlind?.Unhook(OnPawnBlindPost,HookMode.Post);}finally{FlushFlashLog();ClearFlashWindows();}
    }
}
