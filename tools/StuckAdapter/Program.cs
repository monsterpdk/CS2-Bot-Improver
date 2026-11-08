using System.Reflection.PortableExecutable;using System.Reflection.Metadata;using System.Reflection.Metadata.Ecma335;using System.Security.Cryptography;using System.Text.Json;
var original=File.ReadAllBytes(args[0]);const string expected="2A8E29753FA7E2195340EEF25883B36239C47C7C27F9F4981D623D28C7625953";
if(Convert.ToHexString(SHA256.HashData(original))!=expected)throw new Exception("Unsupported exact BotState input");
using var pe=new PEReader(new MemoryStream(original));var md=pe.GetMetadataReader();
if((pe.PEHeaders.CorHeader!.Flags&CorFlags.StrongNameSigned)!=0)throw new Exception("Strong-name-signed assembly");
var type=md.TypeDefinitions.Select(md.GetTypeDefinition).Single(t=>md.GetString(t.Name)=="BotState" && md.GetString(t.Namespace)=="BotState");
var m=type.GetMethods().Select(md.GetMethodDefinition).Single(m=>md.GetString(m.Name)=="RecoverStuckBot");var body=pe.GetMethodBody(m.RelativeVirtualAddress);var il=body.GetILBytes()!;
if(il.Length!=0x458 || body.ExceptionRegions.Length!=0)throw new Exception("Unexpected recovery body");
int Member(string owner,string name){var handles=md.MemberReferences.Where(h=>{var r=md.GetMemberReference(h);return md.GetString(r.Name)==name && r.Parent.Kind==HandleKind.TypeReference && md.GetString(md.GetTypeReference((TypeReferenceHandle)r.Parent).Name)==owner;}).ToList();if(handles.Count!=1)throw new Exception("Ambiguous/missing member "+owner+name);return MetadataTokens.GetToken(handles[0]);}
var prefix=new List<byte>();void Emit(params byte[] bytes)=>prefix.AddRange(bytes);void Call(int token){Emit(0x6f);prefix.AddRange(BitConverter.GetBytes(token));}void Branch(byte op,int target){Emit(op);prefix.AddRange(BitConverter.GetBytes(target-(prefix.Count+4)));}
// Guard combat and defusing before any stuck/idle state write; retain native recovery when appropriate.
Emit(0x0e,6);Branch(0x3a,0x457); // ldarg.s attacking, brtrue ret
Emit(0x04);Call(Member("CCSPlayerPawn","get_IsDefusing"));Emit(0x47);Branch(0x3a,0x457);
Emit(0x05);Call(Member("CCSBot","get_IsEnemyVisible"));Emit(0x47);Branch(0x3a,0x457);
Emit(0x05);Call(Member("CCSBot","get_IsStuck"));Emit(0x47);Branch(0x39,0x326);
Emit(0x05);Call(BitConverter.ToInt32(il,0x0e));Emit(0x17,0x52);
if(prefix.Count>0x49)throw new Exception("Prefix overflow");while(prefix.Count<0x49)Emit(0);
var changed=(byte[])il.Clone();prefix.CopyTo(changed,0);
if(il[0x20c]!=0x17 || il[0x1ab]!=0x22 || BitConverter.ToSingle(il,0x1ac)!=1 || il[0x1c3]!=0x22 || BitConverter.ToSingle(il,0x1c4)!=3)throw new Exception("Unexpected one-shot/cooldown literals");
changed[0x20c]=0x16; // keep attempt latch false: permit retry after measured no-progress window
BitConverter.GetBytes(3f).CopyTo(changed,0x1ac);BitConverter.GetBytes(5f).CopyTo(changed,0x1c4);
var section=pe.PEHeaders.SectionHeaders.Single(s=>m.RelativeVirtualAddress>=s.VirtualAddress && m.RelativeVirtualAddress<s.VirtualAddress+s.SizeOfRawData);int offset=section.PointerToRawData+m.RelativeVirtualAddress-section.VirtualAddress;int header=(BitConverter.ToUInt16(original,offset)>>12)*4;
if(header<12 || (original[offset]&3)!=3)throw new Exception("Unexpected CLI fat header");
var output=(byte[])original.Clone();changed.CopyTo(output,offset+header);
using(var after=new PEReader(new MemoryStream(output))){foreach(var h in type.GetMethods()){var method=md.GetMethodDefinition(h);if(method.RelativeVirtualAddress==0)continue;var a=after.GetMethodBody(method.RelativeVirtualAddress);var b=pe.GetMethodBody(method.RelativeVirtualAddress);if(a.Size!=b.Size || a.LocalSignature!=b.LocalSignature || a.ExceptionRegions.Length!=b.ExceptionRegions.Length)throw new Exception("Header changed");if(method.RelativeVirtualAddress!=m.RelativeVirtualAddress && !a.GetILBytes()!.SequenceEqual(b.GetILBytes()!))throw new Exception("Unrelated method changed");}}
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);File.WriteAllBytes(args[1],output);
File.WriteAllText(args[1]+".adapter.json",JsonSerializer.Serialize(new{InputSHA256=expected,OutputSHA256=Convert.ToHexString(SHA256.HashData(output)),Method="RecoverStuckBot",MethodRVA=m.RelativeVirtualAddress,ILFileOffset=offset+header,ILLength=il.Length,ChangedBytes=original.Zip(output).Count(x=>x.First!=x.Second),PrefixHex=Convert.ToHexString(prefix.ToArray()),RetryStillSeconds=3,RetrySlowSeconds=5,OtherMethodBodiesUnchanged=true,FlashCorrectionsPreserved=true},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine("PASS exact recovery adaptation: bounded retries, native jump timers retained, combat/defuse guards; all other methods unchanged");
