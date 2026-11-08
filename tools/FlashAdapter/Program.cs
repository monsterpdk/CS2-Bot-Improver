using System.Reflection.PortableExecutable;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text.Json;
var input=Path.GetFullPath(args[0]);var output=Path.GetFullPath(args[1]);
var original=File.ReadAllBytes(input);
var expected="7C794E8A99178B52D4659A03349E699BAF277401C937012E7873FC1C6513A1E2";
if(Convert.ToHexString(SHA256.HashData(original))!=expected)throw new Exception("Unsupported BotState binary");
using var stream=new MemoryStream(original);using var pe=new PEReader(stream);
if((pe.PEHeaders.CorHeader!.Flags&CorFlags.StrongNameSigned)!=0)throw new Exception("Cannot adapt a strong-name-signed assembly");
var md=pe.GetMetadataReader();
var type=md.TypeDefinitions.Select(h=>md.GetTypeDefinition(h)).Single(t=>md.GetString(t.Name)=="BotState" && md.GetString(t.Namespace)=="BotState");
var targets=new Dictionary<string,byte[]>{["OnPlayerBlind"]=new byte[]{0x0a,0x16,0x2a},["OnBotBlindPre"]=new byte[]{0x0a,0x16,0x2a},["ProcessFlashbangAvoidance"]=new byte[]{0x06,0x2a}};
var bytes=(byte[])original.Clone();var records=new List<object>();int patched=0;
foreach(var handle in type.GetMethods())
{
    var method=md.GetMethodDefinition(handle);var name=md.GetString(method.Name);
    if(!targets.TryGetValue(name,out var replacement))continue;
    var rva=method.RelativeVirtualAddress;var body=pe.GetMethodBody(rva);
    var section=pe.PEHeaders.SectionHeaders.Single(s=>rva>=s.VirtualAddress && rva<s.VirtualAddress+s.SizeOfRawData);
    var offset=section.PointerToRawData+rva-section.VirtualAddress;
    if(body.Size<replacement.Length)throw new Exception("Method body too small");
    replacement.CopyTo(bytes,offset);
    records.Add(new {Method=name,RVA=rva,FileOffset=offset,Before=Convert.ToHexString(original.AsSpan(offset,replacement.Length)),After=Convert.ToHexString(replacement),OriginalBodySize=body.Size});patched++;
}
if(patched!=3)throw new Exception("Unexpected method count");
using(var changed=new PEReader(new MemoryStream(bytes)))
{
    foreach(var handle in type.GetMethods())
    {
        var method=md.GetMethodDefinition(handle);var name=md.GetString(method.Name);
        if(method.RelativeVirtualAddress==0)continue;
        var after=changed.GetMethodBody(method.RelativeVirtualAddress);
        if(targets.TryGetValue(name,out var replacement))
        {
            if(after.ExceptionRegions.Length!=0 || after.LocalSignature.IsNil==false || !after.GetILBytes()!.SequenceEqual(replacement.Skip(1)))throw new Exception("Invalid replacement body: "+name);
        }
        else
        {
            var before=pe.GetMethodBody(method.RelativeVirtualAddress);
            if(!before.GetILBytes()!.SequenceEqual(after.GetILBytes()!) || before.Size!=after.Size)throw new Exception("Unrelated method changed: "+name);
        }
    }
}
Directory.CreateDirectory(Path.GetDirectoryName(output)!);File.WriteAllBytes(output,bytes);
var report=new {InputSHA256=expected,OutputSHA256=Convert.ToHexString(SHA256.HashData(bytes)),Methods=records,ChangedBytes=original.Zip(bytes).Count(p=>p.First!=p.Second),AllOtherTopLevelBodiesIdentical=true};
File.WriteAllText(output+".adapter.json",JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine("PASS exact original BotState adapted: 3 tiny CLI method bodies, no EH/locals, every other method body unchanged. "+report.OutputSHA256);
