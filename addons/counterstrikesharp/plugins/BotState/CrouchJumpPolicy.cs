namespace BotState;

internal readonly record struct RecoveryPoint(float X,float Y,float Z)
{
    public bool Finite=>float.IsFinite(X)&&float.IsFinite(Y)&&float.IsFinite(Z);
    public float HorizontalDistance(RecoveryPoint other)=>MathF.Sqrt((X-other.X)*(X-other.X)+(Y-other.Y)*(Y-other.Y));
}
internal readonly record struct RecoveryHit(float Fraction,bool StartSolid,float NormalZ,bool ActorOrDoor)
{
    public bool Clear=>float.IsFinite(Fraction)&&!StartSolid&&Fraction>=0.99f&&Fraction<=1;
}
internal readonly record struct CrouchJumpPlan(RecoveryPoint Target,float DirX,float DirY,float Rise);

internal static class CrouchJumpPolicy
{
    internal const float StandingHeight=72,CrouchingHeight=54,MaxRise=54;
    internal const int MaxAttempts=3;
    // Raised close goals can be the top of the obstacle blocking the bot.
    internal static float MinimumGoalDistance(RecoveryPoint origin,RecoveryPoint goal)
        =>goal.Z-origin.Z>8?8:32;
    internal static float LeaseSeconds(CrouchJumpPlan plan)=>plan.Rise==0?1.8f:0.9f;
    internal static float ForwardProgress(RecoveryPoint start,RecoveryPoint position,CrouchJumpPlan plan)
        =>(position.X-start.X)*plan.DirX+(position.Y-start.Y)*plan.DirY;
    internal static bool IsStalled(float elapsed,RecoveryPoint start,RecoveryPoint position,CrouchJumpPlan plan)
        =>plan.Rise==0&&elapsed>=0.7f&&ForwardProgress(start,position,plan)<2;
    internal static int PreferredSide(RecoveryPoint origin,RecoveryPoint goal,int attempt,CrouchJumpPlan? previous)
    {
        if(previous is not {} last||last.Rise!=0||!origin.Finite||!goal.Finite||origin.HorizontalDistance(last.Target)>96)return attempt;
        float distance=origin.HorizontalDistance(goal);
        if(distance<MinimumGoalDistance(origin,goal)||!float.IsFinite(distance))return attempt;
        float leftDot=(-(goal.Y-origin.Y)*last.DirX+(goal.X-origin.X)*last.DirY)/distance;
        return MathF.Abs(leftDot)>=0.5f?(leftDot>0?0:1):attempt;
    }
    // A zero-rise plan is a short walking detour, never an authorized jump.
    internal static CrouchJumpPlan? FindDetour(RecoveryPoint origin,RecoveryPoint goal,int attempt,
        Func<RecoveryPoint,RecoveryPoint,float,RecoveryHit> sweep,CrouchJumpPlan? previous=null,IReadOnlyList<CrouchJumpPlan>? failed=null)
    {
        if(!origin.Finite||!goal.Finite)return null;
        float distance=origin.HorizontalDistance(goal);
        if(distance<MinimumGoalDistance(origin,goal)||distance>2048||MathF.Abs(goal.Z-origin.Z)>256)return null;
        float dx=(goal.X-origin.X)/distance,dy=(goal.Y-origin.Y)/distance;
        var start=new RecoveryPoint(origin.X,origin.Y,origin.Z+2);
        var obstruction=sweep(start,new(start.X+dx*48,start.Y+dy*48,start.Z),StandingHeight);
        if(obstruction.Clear||obstruction.StartSolid||obstruction.ActorOrDoor||!float.IsFinite(obstruction.Fraction))return null;
        float sign=attempt%2==0?1:-1;
        var directions=new List<(float,float)>{(-dy*sign,dx*sign),(dy*sign,-dx*sign),(-dx,-dy)};
        if(previous is {} last&&last.Rise==0&&origin.HorizontalDistance(last.Target)<=96
            &&float.IsFinite(last.DirX)&&float.IsFinite(last.DirY)&&MathF.Abs(last.DirX*last.DirX+last.DirY*last.DirY-1)<0.01f)
            directions.Insert(0,(last.DirX,last.DirY));
        foreach(var direction in directions)
        {
            if(failed is not null&&failed.Any(p=>p.DirX*direction.Item1+p.DirY*direction.Item2>=0.85f))continue;
            var end=new RecoveryPoint(start.X+direction.Item1*48,start.Y+direction.Item2*48,start.Z);
            if(!sweep(start,end,StandingHeight).Clear)continue;
            bool supported=true;
            // Check support across the route, not only at its endpoint.
            for(int sample=0;sample<=4;sample++)
            {
                var top=new RecoveryPoint(start.X+(end.X-start.X)*sample/4,start.Y+(end.Y-start.Y)*sample/4,start.Z);
                var floor=sweep(top,new(top.X,top.Y,origin.Z-4),StandingHeight);
                if(floor.StartSolid||floor.ActorOrDoor||!float.IsFinite(floor.Fraction)||floor.Fraction<0||floor.Fraction>=1||!float.IsFinite(floor.NormalZ)||floor.NormalZ<0.7f){supported=false;break;}
            }
            if(supported)return new(new(end.X,end.Y,origin.Z),direction.Item1,direction.Item2,0);
        }
        return null;
    }
    // The trace delegate uses a full player-width hull and ignores only the owning pawn.
    internal static CrouchJumpPlan? Find(RecoveryPoint origin,RecoveryPoint goal,
        Func<RecoveryPoint,RecoveryPoint,float,RecoveryHit> sweep)
    {
        if(!origin.Finite||!goal.Finite)return null;
        float distance=origin.HorizontalDistance(goal);
        if(distance<MinimumGoalDistance(origin,goal)||distance>2048||MathF.Abs(goal.Z-origin.Z)>256)return null;
        float dx=(goal.X-origin.X)/distance,dy=(goal.Y-origin.Y)/distance;
        var start=new RecoveryPoint(origin.X,origin.Y,origin.Z+2);
        var obstruction=sweep(start,new(start.X+dx*48,start.Y+dy*48,start.Z),StandingHeight);
        if(obstruction.Clear||obstruction.StartSolid||obstruction.ActorOrDoor||!float.IsFinite(obstruction.Fraction))return null;
        foreach(float rise in new[]{16f,24,32,40,48,54})
        {
            var raised=new RecoveryPoint(start.X,start.Y,start.Z+rise);
            if(!sweep(start,raised,CrouchingHeight).Clear)continue;
            foreach(float reach in new[]{48f,72,96})
            {
                var end=new RecoveryPoint(raised.X+dx*reach,raised.Y+dy*reach,raised.Z);
                if(!sweep(raised,end,CrouchingHeight).Clear)continue;
                var down=new RecoveryPoint(end.X,end.Y,origin.Z-8);
                var floor=sweep(end,down,CrouchingHeight);
                if(!float.IsFinite(floor.Fraction)||floor.StartSolid||floor.ActorOrDoor||floor.Fraction<0||floor.Fraction>=1||!float.IsFinite(floor.NormalZ)||floor.NormalZ<0.7f)continue;
                float landingZ=end.Z+(down.Z-end.Z)*floor.Fraction;
                if(landingZ<origin.Z-4||landingZ>origin.Z+MaxRise)continue;
                return new(new(end.X,end.Y,landingZ),dx,dy,rise);
            }
        }
        return null;
    }
}
