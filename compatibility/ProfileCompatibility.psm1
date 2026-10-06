$ErrorActionPreference = 'Stop'
if (-not ('FairplayProfiles' -as [type])) {
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Security.Cryptography;
public static class FairplayProfiles {
 static uint Crc(byte[] data) { uint c=0xffffffff; foreach(byte b in data) { c^=b; for(int i=0;i<8;i++) c=(c>>1)^((c&1)!=0?0xedb88320u:0u); } return ~c; }
 static string Str(BinaryReader r) { var b=new List<byte>(); byte x; while((x=r.ReadByte())!=0) b.Add(x); return Encoding.UTF8.GetString(b.ToArray()); }
 static byte[] Slice(byte[] a,int p,int n) { var b=new byte[n]; Buffer.BlockCopy(a,p,b,0,n); return b; }
 static void Equal(byte[] a,byte[] b) { if(BitConverter.ToString(a)!=BitConverter.ToString(b)) throw new Exception("Profile checksum mismatch"); }
 public static byte[] Transform(byte[] original,string[] sections,string[] values,string rush) {
  var r=new BinaryReader(new MemoryStream(original));
  if(r.ReadUInt32()!=0x55aa1234 || r.ReadUInt32()!=2) throw new Exception("Unsupported profile VPK");
  int ts=checked((int)r.ReadUInt32()), ds=checked((int)r.ReadUInt32());
  if(r.ReadUInt32()!=0 || r.ReadUInt32()!=48 || r.ReadUInt32()!=0 || original.Length!=28+ts+ds+48) throw new Exception("Unsupported VPK layout");
  int end=28+ts, md=end+ds; var hash=MD5.Create();
  Equal(hash.ComputeHash(Slice(original,28,ts)),Slice(original,md,16));
  Equal(hash.ComputeHash(new byte[0]),Slice(original,md+16,16));
  Equal(hash.ComputeHash(Slice(original,0,md+32)),Slice(original,md+32,16));
  byte[] tree=Slice(original,28,ts); var tw=new BinaryWriter(new MemoryStream(tree)); var data=new MemoryStream();
  int files=0; string ext,dir,name;
  while((ext=Str(r))!="") { while((dir=Str(r))!="") { while((name=Str(r))!="") {
   int entry=checked((int)r.BaseStream.Position); uint crc=r.ReadUInt32(); ushort preload=r.ReadUInt16(), archive=r.ReadUInt16();
   int offset=checked((int)r.ReadUInt32()), length=checked((int)r.ReadUInt32());
   if(preload!=0 || archive!=0x7fff || r.ReadUInt16()!=0xffff || offset<0 || length<0 || offset+length>ds) throw new Exception("Unsupported VPK entry");
   byte[] content=Slice(original,end+offset,length); if(Crc(content)!=crc) throw new Exception("Profile CRC mismatch");
   string path=(dir==" "?"":dir+"/")+name+"."+ext;
   if(path=="botprofile.db") {
    string text=new UTF8Encoding(false,true).GetString(content).TrimStart('\ufeff'), section=null;
    var found=new HashSet<string>(); var lines=Regex.Split(text,"(?<=\\n)");
    for(int i=0;i<lines.Length;i++) {
     string clean=lines[i].Split(new string[]{"//"},StringSplitOptions.None)[0].Trim();
     if(clean.Equals("End",StringComparison.OrdinalIgnoreCase)) section=null;
     else if(clean!="" && !clean.Contains("=")) section=clean.StartsWith("Template ")?clean.Substring(9).Trim():clean;
     int index=Array.IndexOf(sections,section); var match=Regex.Match(lines[i],@"^(\s*ReactionTime\s*=\s*)([0-9.]+)",RegexOptions.IgnoreCase);
     if(index>=0 && match.Success) { if(!found.Add(section)) throw new Exception("Duplicate reaction template"); lines[i]=lines[i].Remove(match.Groups[2].Index,match.Groups[2].Length).Insert(match.Groups[2].Index,values[index]); }
    }
    if(found.Count!=sections.Length) throw new Exception("Missing reaction template");
    content=Encoding.UTF8.GetBytes(string.Join("",lines));
   } else if(path=="scripts/ai/rush/bt_config.kv3") {
    string text=new UTF8Encoding(false,true).GetString(content); var rx=new Regex(@"^(\s*reaction_time\s*=\s*)([0-9.]+)",RegexOptions.Multiline);
    if(rx.Matches(text).Count!=1) throw new Exception("Missing Rush reaction time");
    content=Encoding.UTF8.GetBytes(rx.Replace(text,m=>m.Groups[1].Value+rush));
   } else if(path!="scripts/ai/rush/bt_default.kv3") throw new Exception("Unexpected profile file");
   tw.BaseStream.Position=entry-28; tw.Write(Crc(content)); tw.Write((ushort)0); tw.Write((ushort)0x7fff); tw.Write((uint)data.Length); tw.Write((uint)content.Length); tw.Write((ushort)0xffff);
   data.Write(content,0,content.Length); files++;
  } } }
  if(r.BaseStream.Position!=end || files!=3) throw new Exception("Unexpected profile tree");
  var output=new MemoryStream(); var w=new BinaryWriter(output);
  w.Write(0x55aa1234u); w.Write(2u); w.Write((uint)ts); w.Write((uint)data.Length); w.Write(0u); w.Write(48u); w.Write(0u);
  w.Write(tree); w.Write(data.ToArray()); w.Write(hash.ComputeHash(tree)); w.Write(hash.ComputeHash(new byte[0])); w.Flush(); w.Write(hash.ComputeHash(output.ToArray())); w.Flush(); return output.ToArray();
 }
}
'@
}
function Get-ProfileUpdates([string]$Csgo, [string]$SettingsPath) {
 $settings = Get-Content -LiteralPath $SettingsPath -Raw | ConvertFrom-Json
 $active = (Get-FileHash -LiteralPath (Join-Path $Csgo 'overrides\botprofile.vpk')).Hash
 $low = (Get-FileHash -LiteralPath (Join-Path $Csgo 'overrides\Low\botprofile.vpk')).Hash
 if ($low -ne $settings.LowSHA256) { throw 'Unknown Low profile; no files changed.' }
 $updates = @{}; $recognized = $active -eq $low
 foreach ($level in @('Medium','High')) {
  $spec = $settings.$level; $relative = 'overrides\' + $level + '\botprofile.vpk'; $path = Join-Path $Csgo $relative
  $current = (Get-FileHash -LiteralPath $path).Hash
  if ($current -notin @($spec.OriginalSHA256,$spec.OutputSHA256)) { throw ('Unknown ' + $level + ' profile; no files changed.') }
  [string[]]$names = @($spec.Templates.PSObject.Properties.Name)
  [string[]]$values = @($names | ForEach-Object { $spec.Templates.$_ })
  [byte[]]$bytes = [FairplayProfiles]::Transform([IO.File]::ReadAllBytes($path),$names,$values,$spec.Rush)
  $digest = [BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash($bytes)).Replace('-','')
  if ($digest -ne $spec.OutputSHA256) { throw ('Unexpected generated ' + $level + ' profile; no files changed.') }
  $updates[$relative] = $bytes
  if ($active -in @($spec.OriginalSHA256,$spec.OutputSHA256)) { $updates['overrides\botprofile.vpk']=$bytes; $recognized=$true }
 }
 if (!$recognized) { throw 'Unknown active profile; no files changed.' }
 return $updates
}
Export-ModuleMember -Function Get-ProfileUpdates
