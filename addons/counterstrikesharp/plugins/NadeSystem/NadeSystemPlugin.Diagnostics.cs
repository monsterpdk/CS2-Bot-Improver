using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace NadeSystem;

public partial class NadeSystemPlugin
{
    private sealed class NadeCounters
    {
        public long Zones,Considered,Unavailable,MoneyRejected,SafetyRejected,Planned,Created,Failed,Detonated;
        public long SkippedCandidates;
        public readonly Dictionary<string,long> CandidateReasons = new(StringComparer.Ordinal);
        public readonly Dictionary<string,long> ReleaseReasons = new(StringComparer.Ordinal);
    }
    private readonly Dictionary<string,NadeCounters> _diagnostics=new(StringComparer.OrdinalIgnoreCase);
    private NadeCounters NadeStats(string type)
    {
        if(!_diagnostics.TryGetValue(type,out var value)) { value=new();_diagnostics[type]=value; }
        return value;
    }
    private void RecordCandidateRejection(string type, string reason, bool candidate)
    {
        var c=NadeStats(type);
        if(candidate) c.SkippedCandidates++;
        var reasons=candidate ? c.CandidateReasons : c.ReleaseReasons;
        reasons.TryGetValue(reason,out var count);reasons[reason]=count+1;
    }
    private static string RejectionSummary(Dictionary<string,long> reasons)
        => reasons.Count==0 ? "none" : string.Join(",",reasons.OrderBy(p=>p.Key).Select(p=>$"{p.Key}:{p.Value}"));
    private bool ReadyGrenade(string type) => NativeGrenadePolicy.Available(type,_smokeCreate.Handle!=IntPtr.Zero,_heCreate.Handle!=IntPtr.Zero,_molotovCreate.Handle!=IntPtr.Zero);
    private bool ValidateGrenadeBuild()
    {
        var path=Path.GetFullPath(Path.Combine(ModuleDirectory,"..","..","..","..","steam.inf"));
        if(OperatingSystem.IsWindows() && File.Exists(path) && File.ReadLines(path).Any(line=>line.Trim()=="ClientVersion=2000935")) return true;
        Logger.LogError("[NadeSystem] This local compatibility trial requires the audited Windows ClientVersion 2000935; inactive on this build.");
        return false;
    }
    private void InitGrenadeDiagnostics()
    {
        Logger.LogInformation("[NadeSystem] Native factories smoke={Smoke} he={HE} molotov={Molotov}",ReadyGrenade("smoke"),ReadyGrenade("he"),ReadyGrenade("molotov"));
        RegisterListener<Listeners.OnMapStart>(_=>_diagnostics.Clear());
        RegisterEventHandler<EventSmokegrenadeDetonate>((ev,_)=>{if(ev.Userid?.IsBot==true)NadeStats("smoke").Detonated++;return HookResult.Continue;});
        RegisterEventHandler<EventHegrenadeDetonate>((ev,_)=>{if(ev.Userid?.IsBot==true)NadeStats("he").Detonated++;return HookResult.Continue;});
        RegisterEventHandler<EventFlashbangDetonate>((ev,_)=>{if(ev.Userid?.IsBot==true)NadeStats("flash").Detonated++;return HookResult.Continue;});
        AddCommand("bot_nades_status","Show grenade creation and decision counters for the current map",(_,info)=>
        {
            info.ReplyToCommand($"[NadeSystem] version={ModuleVersion} mode={_botNadesMode}");
            foreach(var type in new[]{"smoke","he","flash","molotov"})
            {
                var c=NadeStats(type);var entries=_mapNades.Count(n=>n.GrenadeType==type);
                info.ReplyToCommand($"[NadeSystem] {type}: ready={ReadyGrenade(type)} entries={entries} zones={c.Zones} considered={c.Considered} unavailable={c.Unavailable} money={c.MoneyRejected} safety={c.SafetyRejected} planned={c.Planned} created={c.Created} failed={c.Failed} detonated={c.Detonated}");
                info.ReplyToCommand($"[NadeSystem] {type} selection: skipped={c.SkippedCandidates} reasons={RejectionSummary(c.CandidateReasons)} releaseReasons={RejectionSummary(c.ReleaseReasons)}");
            }
        });
    }
}
