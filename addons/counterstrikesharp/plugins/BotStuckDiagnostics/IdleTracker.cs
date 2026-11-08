namespace BotStuckDiagnostics;
internal readonly record struct Point(float X,float Y,float Z)
{
    public bool Finite=>float.IsFinite(X)&&float.IsFinite(Y)&&float.IsFinite(Z);
    public float HorizontalDistance(Point p)=>MathF.Sqrt((X-p.X)*(X-p.X)+(Y-p.Y)*(Y-p.Y));
    public float Distance(Point p)=>MathF.Sqrt((X-p.X)*(X-p.X)+(Y-p.Y)*(Y-p.Y)+(Z-p.Z)*(Z-p.Z));
}
internal enum Notice { None,First,Repeat,Resumed,Gap }
internal readonly record struct Observation(Notice Notice,float Seconds);
internal sealed class IdleTracker
{
    public const float Radius=24,VerticalRadius=96,Threshold=15,RepeatInterval=30;
    public float MinZ {get;private set;}
    public float MaxZ {get;private set;}
    public float VerticalSpan=>MaxZ-MinZ;
    public Point Anchor {get;private set;}
    public float Started {get;private set;}
    public float LastSample {get;private set;}
    public bool Reported {get;private set;}
    private float _next;
    public IdleTracker(float now,Point point)=>Reset(now,point);
    private void Reset(float now,Point point){Anchor=point;MinZ=MaxZ=point.Z;Started=now;LastSample=now;Reported=false;_next=now+Threshold;}
    public Observation Observe(float now,Point point)
    {
        var seconds=MathF.Max(0,now-Started);var reported=Reported;
        if(!float.IsFinite(now)||!point.Finite)return new(Notice.None,0);
        if(now<LastSample || now-LastSample>4){Reset(now,point);return new(reported?Notice.Gap:Notice.None,seconds);}
        LastSample=now;
        if(point.HorizontalDistance(Anchor)>Radius || MathF.Abs(point.Z-Anchor.Z)>VerticalRadius){Reset(now,point);return new(reported?Notice.Resumed:Notice.None,seconds);}
        MinZ=MathF.Min(MinZ,point.Z);MaxZ=MathF.Max(MaxZ,point.Z);
        if(now<_next)return new(Notice.None,seconds);
        var notice=Reported?Notice.Repeat:Notice.First;Reported=true;_next=now+RepeatInterval;
        return new(notice,seconds);
    }
}
