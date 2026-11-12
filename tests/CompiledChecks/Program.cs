using System.Reflection;using System.Reflection.Emit;using System.Runtime.CompilerServices;using System.Runtime.Loader;
try{Run(args);return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
static void Run(string[] args){
 string api=Path.GetFullPath(args[0]),shared=Path.GetFullPath(args[1]);AssemblyLoadContext.Default.Resolving+=(_,n)=>{var p=n.Name=="BotControllerApi"?shared:Path.Combine(api,n.Name+".dll");return File.Exists(p)?AssemblyLoadContext.Default.LoadFromAssemblyPath(p):null;};
 var asm=Assembly.LoadFrom(Path.GetFullPath(args[2]));var t=asm.GetType("BotState.BotState",true)!;var flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
 var obj=RuntimeHelpers.GetUninitializedObject(t);t.GetMethod("OnTick",flags)!.Invoke(obj,null);Console.WriteLine("PASS actual BotState OnTick returns before native queries outside a live round");
 var ops=typeof(OpCodes).GetFields(BindingFlags.Static|BindingFlags.Public).Where(f=>f.FieldType==typeof(OpCode)).Select(f=>(OpCode)f.GetValue(null)!).ToDictionary(o=>unchecked((ushort)o.Value));
 List<string> Calls(string name){var m=t.GetMethod(name,flags)!;var il=m.GetMethodBody()!.GetILAsByteArray()!;var calls=new List<string>();for(int pos=0;pos<il.Length;){ushort k=il[pos++];if(k==0xfe)k=(ushort)(0xfe00|il[pos++]);var op=ops[k];if(op.OperandType==OperandType.InlineMethod)calls.Add(m.Module.ResolveMethod(BitConverter.ToInt32(il,pos))!.Name);pos+=op.OperandType switch {OperandType.InlineNone=>0,OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar=>1,OperandType.InlineVar=>2,OperandType.InlineI8 or OperandType.InlineR=>8,OperandType.InlineSwitch=>4+BitConverter.ToInt32(il,pos)*4,_=>4};}return calls;}
 if(Calls("Load").Any(x=>x is "InstallBotBlindHook" or "OnPlayerBlind")||Calls("OnTick").Contains("ProcessFlashbangAvoidance"))throw new Exception("Artificial immunity remains active");Console.WriteLine("PASS compiled Load/Tick do not install/call artificial flash immunity");
 if(Calls("RecoverStuckBot").Any(x=>x is "get_JumpTimestamp" or "get_StuckJumpTimer"))throw new Exception("Per-tick jump rearming returned");Console.WriteLine("PASS legacy recovery leaves native jump timers alone");
 var protectedTimers=new[]{"get_IsRapidFiring","get_PeripheralTimestamp","get_FireWeaponTimestamp","get_AlertTimer","get_IgnoreEnemiesTimer","get_PanicTimer","get_SurpriseTimer","get_IsEnemySniperVisible","get_SawEnemySniperTimer"};
 if(Calls("RecoverStuckBot").Any(x=>x is "Cos" or "Sin" or "get_EyeAngles"))throw new Exception("Unchecked backwards-push recovery returned");
 Console.WriteLine("PASS legacy recovery no longer computes unchecked backwards velocity");
 if(Calls("UpdateBotBehavior").Intersect(protectedTimers).Any())throw new Exception("Combat timing/readiness override remains");
 var combat=Calls("UpdateBotCombat");
 if(combat.Contains("get_IsAimingAtEnemy")||combat.Contains("set_IsAttacking")||combat.Count(x=>x=="get_IsAttacking")!=1)throw new Exception("Aim-to-attack shortcut remains");
 Console.WriteLine("PASS compiled behavior leaves native perception/fire/surprise timers untouched and combat only reads the attack state");
 var ladder=asm.GetType("BotState.LadderMovementPolicy",true)!.GetMethod("ShouldDefer",BindingFlags.NonPublic|BindingFlags.Static)!;
 bool Defer(bool climbing,float now,float? last)=>(bool)ladder.Invoke(null,new object?[]{climbing,now,last})!;
 if(!Defer(true,10,null)||!Defer(false,14,10)||Defer(false,15,10)||Defer(false,10,null)||Defer(false,9,10)||Defer(false,float.NaN,10))throw new Exception("Ladder protection lifetime failed");
 var tick=Calls("OnTick");
 if(tick.IndexOf("UpdateLadderProtection")<0||tick.IndexOf("UpdateLadderProtection")>tick.IndexOf("UpdateFairCrouchRecovery")||!tick.Contains("CancelRecovery"))throw new Exception("Ladder guard/cancellation not in traversal");
 Console.WriteLine("PASS ladder policy protects climb and bounded exit, expires safely, and tick checks it before recovery with owned-input cancellation");
 var contract=Assembly.LoadFrom(shared).GetType("BotControllerApi.IBotControllerApi",true)!;
 foreach(var name in new[]{"InjectUsercmd","CancelUsercmdInjection","StartUsercmdMovement","UpdateUsercmdMovement","CancelUsercmdMovement","IsReplaying","IsLocked","GetWeaponLock"})if(contract.GetMethods().All(m=>m.Name!=name))throw new Exception("Missing controller contract "+name);
 Console.WriteLine("PASS actual latest shared API exposes bounded-input and lock contracts");
 var proxy=DispatchProxy.Create(contract,typeof(ControllerProbe));var entryType=t.GetNestedType("RecoveryEntry",BindingFlags.NonPublic)!;
 var cancel=t.GetMethod("CancelRecovery",BindingFlags.Static|BindingFlags.NonPublic)!;
 foreach(bool failFirst in new[]{false,true}) {
  var e=Activator.CreateInstance(entryType,true)!;entryType.GetField("Owner")!.SetValue(e,proxy);
  foreach(var field in new[]{"DuckToken","JumpToken","MovementToken"})entryType.GetField(field)!.SetValue(e,field=="DuckToken"?11L:field=="JumpToken"?12L:13L);
  ControllerProbe.Calls.Clear();ControllerProbe.FailFirst=failFirst;cancel.Invoke(null,new object[]{3,e});
  if(ControllerProbe.Calls.Count!=3||ControllerProbe.Calls[2]!="CancelUsercmdMovement:13"||entryType.GetField("Owner")!.GetValue(e)!=null)throw new Exception("Owned input cleanup failed");
 }
 Console.WriteLine("PASS actual compiled cleanup cancels only owned tokens and still cancels movement if button cancellation throws");
 var sdk=Assembly.LoadFrom(Path.Combine(api,"CounterStrikeSharp.API.dll"));
 var baseMovement=sdk.GetType("CounterStrikeSharp.API.Core.CPlayer_MovementServices",true)!;
 var cast=t.GetMethod("RecoveryMovementServices",BindingFlags.Static|BindingFlags.NonPublic)!;
 var wrapper=Activator.CreateInstance(baseMovement,new object[]{(nint)0x1234})!;
 var specific=cast.Invoke(null,new[]{wrapper})!;
 if(specific.GetType().Name!="CCSPlayer_MovementServices"||(nint)specific.GetType().GetProperty("Handle")!.GetValue(specific)!!=(nint)0x1234)throw new Exception("Base movement wrapper conversion failed");
 if(cast.Invoke(null,new object?[]{null})!=null||cast.Invoke(null,new[]{Activator.CreateInstance(baseMovement,new object[]{(nint)0})})!=null)throw new Exception("Null movement guard failed");
 Console.WriteLine("PASS real SDK base movement wrapper becomes a CS movement wrapper without reading native memory; null handles rejected");
 if(args.Length>3){
  var ai=Assembly.LoadFrom(Path.GetFullPath(args[3]));var policy=ai.GetType("BotAI.FairplayPatchPolicy",true)!;
  var enabled=policy.GetMethod("IsEnabled",BindingFlags.NonPublic|BindingFlags.Static)!;
  var defs=(System.Collections.IEnumerable)ai.GetType("BotAI.WindowsPatchDefinitions",true)!.GetProperty("All",BindingFlags.NonPublic|BindingFlags.Static)!.GetValue(null)!;
  var keys=defs.Cast<object>().Select(x=>(string)x.GetType().GetProperty("Key")!.GetValue(x)!).ToHashSet();
  foreach(string key in new[]{"AttackState_SkipFireRateCheck","IsNoticable_AlwaysTrue","DefuseBomb_SkipIsVisibleCheck","BombBeep_CT_GlobalHearRange","OnBombPlanted_AllBotsLearnSite","OnAudibleEvent_GlobalHearRange","BombPickup_CT_GlobalHearRange","TBot_BombsiteSearch_UseKnownPlantedSite"})
   if(!keys.Contains(key)||(bool)enabled.Invoke(null,new object[]{key})!)throw new Exception("Unfair patch enabled/missing: "+key);
  if(!(bool)enabled.Invoke(null,new object[]{"GameState_Reset"})!)throw new Exception("Unrelated compatibility patch disabled");
  Console.WriteLine("PASS actual BotAI disables native fire-rate/notice bypasses and retains existing fairness exclusions plus compatibility patch");
 }
 if(args.Length>4){
  var observer=Assembly.LoadFrom(Path.GetFullPath(args[4])).GetType("BotStuckDiagnostics.Plugin",true)!;
  observer.GetMethod("Sample",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(RuntimeHelpers.GetUninitializedObject(observer),null);
  Console.WriteLine("PASS disabled public observer returns before any game/native query");
 }
}

public class ControllerProbe:DispatchProxy {
 public static List<string> Calls=new();public static bool FailFirst;
 protected override object? Invoke(MethodInfo? method,object?[]? args){
  Calls.Add(method!.Name+":"+args![1]);if(FailFirst&&Calls.Count==1)throw new Exception("synthetic cancellation error");return true;
 }
}
