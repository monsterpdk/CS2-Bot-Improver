using System.Reflection;
using System.Collections;
using System.Linq.Expressions;
var api=Assembly.LoadFrom(Path.GetFullPath(args[0]));
var shared=Assembly.LoadFrom(Path.GetFullPath(args[1]));
var contract=shared.GetType("BotControllerApi.IBotControllerApi",true)!;
var capDefinition=api.GetType("CounterStrikeSharp.API.Core.Capabilities.PluginCapability`1",true)!;
var capType=capDefinition.MakeGenericType(contract);
var fake=DispatchProxy.Create(contract,typeof(FakeApi));
var funcType=typeof(Func<>).MakeGenericType(contract);
var provider=Expression.Lambda(funcType,Expression.Constant(fake,contract)).Compile();
var list=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(funcType))!;
list.Add(provider);
var registry=(IDictionary)capType.GetField("Providers",BindingFlags.NonPublic|BindingFlags.Static)!.GetValue(null)!;
registry.Add("botcontroller:api",list);
var capability=Activator.CreateInstance(capType,"botcontroller:api");
var value=capType.GetMethod("Get")!.Invoke(capability,null)!;
if(!ReferenceEquals(value,fake)) throw new Exception("Provider registry mismatch");
var replay=value.GetType().GetMethod("IsReplaying")!;
var locked=value.GetType().GetMethods().Single(m=>m.Name=="IsLocked");
var all=Enum.Parse(locked.GetParameters()[1].ParameterType,"All");
if((bool)replay.Invoke(value,new object[]{3})! || (bool)locked.Invoke(value,new object[]{3,all})!) throw new Exception("Unexpected lock result");
Console.WriteLine("PASS actual installed CSS/shared API generic capability registry, replay and All-lock method/enum contract; no native game calls.");
public class FakeApi:DispatchProxy
{
    protected override object? Invoke(MethodInfo? targetMethod,object?[]? args)
    {
        if(targetMethod is null) return null;
        var t=targetMethod.ReturnType;
        return t==typeof(void) ? null : t.IsValueType ? Activator.CreateInstance(t) : null;
    }
}
