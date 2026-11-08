using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
namespace Fairplay.Diagnostics;

// Fixed filenames and rotation bound storage across maps, processes and sessions.
public sealed class DiagnosticLog
{
    public const int FileLimit=1024*1024, FileCount=2, ErrorLimit=128*1024;
    private readonly object _gate=new();
    public string Path {get;}
    public string ErrorPath {get;}
    public bool Enabled {get;private set;}
    public DiagnosticLog(string moduleDirectory,string name)
    {
        if(string.IsNullOrEmpty(name)||name.Any(c=>!char.IsAsciiLetter(c)))throw new ArgumentException("Invalid log name");
        var dir=System.IO.Path.Combine(System.IO.Path.GetFullPath(moduleDirectory),"logs");
        Path=System.IO.Path.Combine(dir,"fairplay-"+name+".jsonl");
        ErrorPath=System.IO.Path.Combine(dir,"fairplay-"+name+"-errors.jsonl");
        try
        {
            var config=System.IO.Path.Combine(moduleDirectory,"diagnostics.json");
            if(File.Exists(config))
            {
                using var doc=JsonDocument.Parse(File.ReadAllText(config));
                Enabled=doc.RootElement.TryGetProperty("Enabled",out var value)&&value.ValueKind==JsonValueKind.True;
            }
        }
        catch(Exception ex){Enabled=false;Error(ex);}
    }
    public void SetEnabled(bool enabled){lock(_gate)Enabled=enabled;}
    public void Append(string json)
    {
        lock(_gate){if(Enabled)AppendBounded(Path,json,FileLimit);}
    }
    public void Error(Exception ex)
    {
        try
        {
            string message=ex.Message; if(message.Length>4000)message=message[..4000];
            lock(_gate)AppendBounded(ErrorPath,JsonSerializer.Serialize(new{utc=DateTime.UtcNow,error=ex.GetType().Name,message}),ErrorLimit);
        }
        catch { /* Diagnostics must not replace the original gameplay failure. */ }
    }
    private static void AppendBounded(string path,string json,int limit)
    {
        var line=json+"\n";var size=Encoding.UTF8.GetByteCount(line);
        if(size>limit)throw new InvalidOperationException("Diagnostic row exceeds fixed file limit");
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        if(File.Exists(path)&&new FileInfo(path).Length+size>limit)
        {
            // Only this logger's base file and .1 archive are managed.
            var previous=path+".1";
            if(File.Exists(previous))File.Delete(previous);
            File.Move(path,previous);
        }
        File.AppendAllText(path,line,new UTF8Encoding(false));
    }
}
