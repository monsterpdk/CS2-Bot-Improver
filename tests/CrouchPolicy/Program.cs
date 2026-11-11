using BotState;
try { Tests.Run();return 0; }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
static class Tests {
record Box(float X0,float X1,float Z0,float Z1,bool Actor=false);
static void Check(bool b,string s){if(!b)throw new Exception(s);Console.WriteLine("PASS "+s);}
static RecoveryHit Sweep(List<Box> boxes,RecoveryPoint a,RecoveryPoint b,float height) {
 float best=1,normal=0;bool startSolid=false,actor=false;
 foreach(var box in boxes){float[] lo={box.X0-16,-1000,box.Z0-height},hi={box.X1+16,1000,box.Z1};float[] start={a.X,a.Y,a.Z},end={b.X,b.Y,b.Z};float enter=0,exit=1,nz=0;bool inside=true,hit=true;
 for(int j=0;j<3;j++){inside &= start[j]>lo[j]+0.001f&&start[j]<hi[j]-0.001f;float d=end[j]-start[j];if(MathF.Abs(d)<0.00001f){if(start[j]<=lo[j]||start[j]>=hi[j])hit=false;continue;}float t0=(lo[j]-start[j])/d,t1=(hi[j]-start[j])/d;float n=d>0?-1:1;if(t0>t1)(t0,t1)=(t1,t0);if(t0>=enter){enter=t0;nz=j==2?n:0;}exit=MathF.Min(exit,t1);if(enter>exit)hit=false;}
 if(inside)return new(0,true,0,box.Actor);if(hit&&enter>=0&&enter<best&&exit>=0){best=enter;normal=nz;actor=box.Actor;}}
 return new(best,startSolid,normal,actor);
}
public static void Run(){
 var floor=new Box(-1000,1000,-1000,0);var desk=new Box(20,70,0,32);var ceiling=new Box(-1000,1000,94,200);
 CrouchJumpPlan? Plan(List<Box> boxes)=>CrouchJumpPolicy.Find(new(0,0,0),new(140,0,0),(a,b,h)=>Sweep(boxes,a,b,h));
 var good=Plan(new(){floor,desk,ceiling});Check(good is {} p&&p.Rise==32&&p.Target.Z==32,"desk plus low ceiling yields a crouch-sized landing on desk");
 var close=CrouchJumpPolicy.Find(new(0,0,0),new(23,0,40),(a,b,h)=>Sweep(new(){floor,desk,ceiling},a,b,h));
 Check(close is {} closePlan&&closePlan.Rise==32,"near raised box goal is evaluated instead of rejected by the old 32-unit gate");
 Check(CrouchJumpPolicy.Find(new(0,0,0),new(23,0,40),(a,b,h)=>Sweep(new(){floor,desk,new(-1000,1000,78,200)},a,b,h)) is null,"near raised goal still cannot bypass crouch headroom checks");
 Check(CrouchJumpPolicy.Find(new(0,0,0),new(1,0,40),(a,b,h)=>Sweep(new(){floor,desk,ceiling},a,b,h)) is null,"nearly coincident raised waypoint does not authorize a jump");
 Check(CrouchJumpPolicy.Find(new(0,0,0),new(23,0,0),(a,b,h)=>Sweep(new(){floor,desk,ceiling},a,b,h)) is null,"flat nearby goal keeps its original reached-goal dead zone");
 Check(Plan(new(){floor,desk,new(-1000,1000,78,200)}) is null,"insufficient crouch headroom rejects jump");
 Check(Plan(new(){floor,new(20,70,0,200)}) is null,"full-height wall never triggers jump");
 Check(Plan(new(){floor}) is null,"clear route never triggers jump");
 Check(Plan(new(){floor,new(20,70,0,32,true)}) is null,"players/hostages/doors are not jump platforms");
 Check(Plan(new(){new(-1000,0,-1000,0),new(20,24,0,32)}) is null,"unsupported landing beyond thin obstruction rejected");
 Check(CrouchJumpPolicy.Find(new(float.NaN,0,0),new(100,0,0),(_,_,_)=>new(1,false,0,false)) is null,"invalid geometry cannot authorize a jump");
 Check(CrouchJumpPolicy.Find(new(0,0,0),new(1,0,0),(_,_,_)=>new(0,false,1,false)) is null,"goal already reached cannot authorize a jump");
 Check(CrouchJumpPolicy.MaxAttempts==3,"per-obstacle retry budget bounded");
 var wall=new List<Box>{floor,new(20,70,0,200)};
 var detour=CrouchJumpPolicy.FindDetour(new(0,0,0),new(140,0,0),0,(a,b,h)=>Sweep(wall,a,b,h));
 Check(CrouchJumpPolicy.FindDetour(new(0,0,0),new(23,0,40),0,(a,b,h)=>Sweep(wall,a,b,h)) is {} nearSide&&nearSide.Rise==0,"near raised goal can also select a supported walking detour");
 Check(detour is {} d&&d.Rise==0&&d.DirY==1&&d.Target.Y==48,"full-height wall permits supported walking detour without authorizing a jump");
 var other=CrouchJumpPolicy.FindDetour(new(0,0,0),new(140,0,0),1,(a,b,h)=>Sweep(wall,a,b,h));
 Check(other is {} o&&o.DirY==-1,"repeated detour alternates preferred side");
 Check(CrouchJumpPolicy.LeaseSeconds(detour!.Value)==1.8f&&CrouchJumpPolicy.LeaseSeconds(good!.Value)==0.9f,"walking permits crouched travel time while jump lease remains short");
 int retained=CrouchJumpPolicy.PreferredSide(new(0,20,0),new(140,0,0),1,detour);
 var continued=CrouchJumpPolicy.FindDetour(new(0,20,0),new(140,0,0),retained,(a,b,h)=>Sweep(wall,a,b,h),detour);
 Check(continued is {} same&&same.DirY>0,"next attempt continues previous clear side instead of reversing on attempt parity");
 Check(CrouchJumpPolicy.PreferredSide(new(0,0,0),new(140,0,0),1,null)==1,"first attempt keeps existing side selection without history");
 Check(!CrouchJumpPolicy.IsStalled(0.6f,new(0,0,0),new(0,0,0),detour.Value),"walking has an acceleration grace period");
 Check(CrouchJumpPolicy.IsStalled(0.8f,new(0,0,0),new(0,0,0),detour.Value),"zero movement terminates an ineffective walking attempt");
 Check(!CrouchJumpPolicy.IsStalled(0.8f,new(0,0,0),new(0,16,0),detour.Value),"useful crouched movement is not mistaken for stalled input");
 Check(CrouchJumpPolicy.IsStalled(0.8f,new(0,0,0),new(16,0,0),detour.Value),"sideways displacement alone does not prove progress toward the walking target");
 var avoided=CrouchJumpPolicy.FindDetour(new(0,0,0),new(140,0,0),0,(a,b,h)=>Sweep(wall,a,b,h),detour,new[]{detour.Value});
 Check(avoided is {} alternative&&alternative.DirY<0,"a failed previous direction is skipped in favor of another checked route");
 Check(CrouchJumpPolicy.FindDetour(new(0,0,0),new(140,0,0),0,(a,b,h)=>Sweep(new(){floor},a,b,h)) is null,"clear forward route cannot initiate a detour");
 Check(CrouchJumpPolicy.FindDetour(new(0,0,0),new(140,0,0),0,(a,b,h)=>Sweep(new(){floor,new(20,70,0,200,true)},a,b,h)) is null,"blocked actor or door cannot initiate detour");
 RecoveryHit Gap(RecoveryPoint a,RecoveryPoint b,float h){
  if(b.Z<a.Z&&MathF.Abs(a.X)+MathF.Abs(a.Y)>10)return new(1,false,0,false);
  return Sweep(wall,a,b,h);
 }
 Check(CrouchJumpPolicy.FindDetour(new(0,0,0),new(140,0,0),0,Gap) is null,"unsupported side or backward route rejects walking detour");
 RecoveryHit MidGap(RecoveryPoint a,RecoveryPoint b,float h){
  if(b.Z<a.Z&&MathF.Abs(a.X)+MathF.Abs(a.Y)>10&&MathF.Abs(a.X)+MathF.Abs(a.Y)<40)return new(1,false,0,false);
  return Sweep(wall,a,b,h);
 }
 Check(CrouchJumpPolicy.FindDetour(new(0,0,0),new(140,0,0),0,MidGap) is null,"supported endpoints do not hide unsupported route interior");
 Check(CrouchJumpPolicy.FindDetour(new(float.NaN,0,0),new(140,0,0),0,(_,_,_)=>new(1,false,0,false)) is null,"invalid position cannot initiate detour");
}
}
