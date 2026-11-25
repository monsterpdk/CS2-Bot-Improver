using System.Reflection;using System.Runtime.InteropServices;using System.Runtime.Loader;
try{Run(args);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}
static void Run(string[] args){
 var api=Path.GetFullPath(args[0]);AssemblyLoadContext.Default.Resolving+=(_,n)=>{var p=Path.Combine(api,n.Name+".dll");return File.Exists(p)?AssemblyLoadContext.Default.LoadFromAssemblyPath(p):null;};
 var asm=Assembly.LoadFrom(Path.GetFullPath(args[1]));var data=asm.GetType("BotAI.Gamedata",true)!;var load=data.GetMethod("Load",BindingFlags.Public|BindingFlags.Static)!;
 var value=load.Invoke(null,new object[]{args[2],"windows"})!;var patches=(System.Collections.IEnumerable)data.GetProperty("Patches")!.GetValue(value)!;if(patches.Cast<object>().Count()!=41)throw new Exception("Unexpected upstream patch count");
 Console.WriteLine("PASS actual upstream JSON parser reads all41 definitions and structured offsets");
 var file=Path.Combine(Path.GetTempPath(),"fairplay1410-parser-"+Guid.NewGuid()+".json");var original=File.ReadAllText(args[2]);
 try{
  foreach(var text in new[]{original.Replace("\"offsets\": {","\"offsets\": {}, \"offsets\": {"),original.Replace("20768","-1")}){
   File.WriteAllText(file,text);bool rejected=false;try{load.Invoke(null,new object[]{file,"windows"});}catch(TargetInvocationException){rejected=true;}if(!rejected)throw new Exception("Malformed/duplicate metadata accepted");
  }
 }finally{File.Delete(file);}Console.WriteLine("PASS actual parser rejects duplicate metadata and negative offsets before native writes");
 var memory=asm.GetType("Common.MemoryPatch",true)!;var capture=memory.GetMethod("Capture",BindingFlags.Public|BindingFlags.Static)!;var write=memory.GetMethod("Write",BindingFlags.Public|BindingFlags.Instance)!;
 nuint page=(nuint)Environment.SystemPageSize;var block=Native.VirtualAlloc(0,page*2,0x3000,4);if(block==0)throw new Exception("VirtualAlloc failed");
 try{
  if(!Native.VirtualProtect(block,page,2,out _)||!Native.VirtualProtect(block+(nint)page,page,0x20,out _))throw new Exception("Protect fixture failed");
  var address=block+(nint)page-2;var patch=capture.Invoke(null,new object[]{address,4})!;write.Invoke(patch,new object[]{new byte[]{3,5,7,9}});var bytes=new byte[4];Marshal.Copy(address,bytes,0,4);
  if(!bytes.SequenceEqual(new byte[]{3,5,7,9})||Protect(block)!=2||Protect(block+(nint)page)!=0x20)throw new Exception("Cross-region patch/protection restoration failed");
 }finally{Native.VirtualFree(block,0,0x8000);}Console.WriteLine("PASS actual upstream MemoryPatch writes across mixed read-only/executable regions and restores both protections");
 bool invalid=false;try{capture.Invoke(null,new object[]{(nint)0,4});}catch(TargetInvocationException){invalid=true;}if(!invalid)throw new Exception("Uncommitted memory accepted");Console.WriteLine("PASS actual MemoryPatch rejects uncommitted memory without writing");
}
static uint Protect(nint p){var b=Marshal.AllocHGlobal(48);try{if(Native.VirtualQuery(p,b,48)==0)throw new Exception("VirtualQuery failed");return (uint)Marshal.ReadInt32(b,36);}finally{Marshal.FreeHGlobal(b);}}
static class Native{
 [DllImport("kernel32.dll",SetLastError=true)]internal static extern nint VirtualAlloc(nint a,nuint n,uint t,uint p);
 [DllImport("kernel32.dll",SetLastError=true)]internal static extern bool VirtualProtect(nint a,nuint n,uint p,out uint old);
 [DllImport("kernel32.dll",SetLastError=true)]internal static extern bool VirtualFree(nint a,nuint n,uint t);
 [DllImport("kernel32.dll",SetLastError=true)]internal static extern nuint VirtualQuery(nint a,nint info,nuint n);
}
