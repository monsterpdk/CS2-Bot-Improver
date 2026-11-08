using Fairplay.Diagnostics;using System.Text.Json;
var root=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"fairplay-diagnostics-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);}
var log=new DiagnosticLog(root,"test");log.Append("{}");Check(!log.Enabled&&!Directory.Exists(System.IO.Path.Combine(root,"logs")),"default disabled creates no log files");
log.SetEnabled(true);var row=JsonSerializer.Serialize(new {text=new string('x',65500)});for(int i=0;i<65;i++)log.Append(row);
Check(Directory.GetFiles(System.IO.Path.Combine(root,"logs")).Length==2,"rotating detail logs use two files total");Check(Directory.GetFiles(System.IO.Path.Combine(root,"logs")).All(p=>new FileInfo(p).Length<=DiagnosticLog.FileLimit),"detail byte bound holds");
new DiagnosticLog(root,"test").Append("{}");Check(Directory.GetFiles(System.IO.Path.Combine(root,"logs")).Length==2,"new process-style instance adds no timestamp file");
var unrelated=System.IO.Path.Combine(root,"logs","unrelated.txt");File.WriteAllText(unrelated,"keep");for(int i=0;i<65;i++)log.Append(row);Check(File.ReadAllText(unrelated)=="keep","rotation preserves unrelated files");
var before=File.ReadAllText(log.Path);try{log.Append(new string('x',DiagnosticLog.FileLimit));throw new Exception("oversize accepted");}catch(InvalidOperationException){}Check(File.ReadAllText(log.Path)==before,"oversize rows cannot truncate current file");
for(int i=0;i<100;i++)log.Error(new Exception(new string('e',4000)));Check(new FileInfo(log.ErrorPath).Length<=DiagnosticLog.ErrorLimit&&new FileInfo(log.ErrorPath+".1").Length<=DiagnosticLog.ErrorLimit,"errors remain bounded independently");
log.SetEnabled(false);before=File.ReadAllText(log.Path);log.Append("{}");Check(File.ReadAllText(log.Path)==before,"runtime off stops detail writes");
File.WriteAllText(System.IO.Path.Combine(root,"diagnostics.json"),"{\"Enabled\":true}");Check(new DiagnosticLog(root,"test").Enabled,"explicit config enables subsequent sessions");
File.WriteAllText(System.IO.Path.Combine(root,"diagnostics.json"),"invalid");Check(!new DiagnosticLog(root,"test").Enabled,"invalid config fails closed");
Console.WriteLine("Test files: "+root);
