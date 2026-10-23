using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
string apiDir=Path.GetFullPath(args[0]);
AssemblyLoadContext.Default.Resolving+=(_,name)=>
{
    var path=Path.Combine(apiDir,name.Name+".dll");
    return File.Exists(path)?AssemblyLoadContext.Default.LoadFromAssemblyPath(path):null;
};
var plugin=Assembly.LoadFrom(Path.GetFullPath(args[1]));
var type=plugin.GetType("BotAimImprover.BotAimImprover",true)!;
var flags=BindingFlags.NonPublic|BindingFlags.Instance;
var ops=typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static).Where(f=>f.FieldType==typeof(OpCode)).Select(f=>(OpCode)f.GetValue(null)!).ToDictionary(o=>unchecked((ushort)o.Value));
var load=type.GetMethod("LoadAwareness",flags)!;
var il=load.GetMethodBody()!.GetILAsByteArray()!; bool earlyQuery=false;
for(int pos=0;pos<il.Length;)
{
    ushort key=il[pos++]; if(key==0xfe) key=(ushort)(0xfe00|il[pos++]); var op=ops[key];
    if(op.OperandType==OperandType.InlineMethod)
    {
        var called=load.Module.ResolveMethod(BitConverter.ToInt32(il,pos));
        if(called?.DeclaringType?.FullName=="CounterStrikeSharp.API.Utilities" && called.Name.StartsWith("FindAllEntities")) earlyQuery=true;
    }
    pos+=op.OperandType switch {
        OperandType.InlineNone=>0,
        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar=>1,
        OperandType.InlineVar=>2,
        OperandType.InlineI8 or OperandType.InlineR=>8,
        OperandType.InlineSwitch=>4+BitConverter.ToInt32(il,pos)*4,
        _=>4
    };
}
if(args.Contains("--expect-unsafe"))
{
    if(!earlyQuery) throw new Exception("Regression test did not detect the faulty startup lookup");
    Console.WriteLine("PASS regression test detects premature entity lookup in the original awareness.1 DLL"); return;
}
if(earlyQuery) throw new Exception("LoadAwareness queries entities before a map is initialized");
var instance=RuntimeHelpers.GetUninitializedObject(type);
type.GetField("_awarenessEnabled",flags)!.SetValue(instance,true);
type.GetField("_roundLive",flags)!.SetValue(instance,false);
foreach(var name in new[]{"UpdateAwareness","SteerAwareness"})
{
    // Empty/uninitialized fields and no native game library: either callback
    // must return before touching game objects, timers or collections.
    type.GetMethod(name,flags)!.Invoke(instance,null);
    Console.WriteLine("PASS actual DLL: "+name+" returns safely before a live round");
}
Console.WriteLine("PASS actual DLL: no startup entity query in LoadAwareness");
