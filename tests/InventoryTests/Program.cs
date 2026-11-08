using NadeSystem;
void Check(bool condition,string message){if(!condition)throw new Exception(message);Console.WriteLine("PASS "+message);}
var stock=new Dictionary<string,int>();
bool Buy(string kind,int total=0,int money=1000,int budget=1000,int cost=300,int totalLimit=4,int flashLimit=2)=>InventoryPolicy.CanBuy(kind,stock,total,totalLimit,flashLimit,cost,money,budget);
Check(Buy("smoke"),"one smoke can be purchased");stock["smoke"]=1;
Check(!Buy("smoke",1),"a second carried smoke cannot be purchased");
stock["flash"]=1;Check(Buy("flash",2,cost:200),"second flash permitted");stock["flash"]=2;
Check(!Buy("flash",3,cost:200),"third flash denied");
Check(!Buy("he",4),"total four-item capacity enforced");
Check(!Buy("he",money:299),"money required before granting an item");
Check(!Buy("he",budget:299),"round budget required");
Check(!Buy("he",cost:0),"no accidental zero-price grant");
Check(!Buy("unknown"),"unknown grenade type cannot be bought");
stock["molotov"]=1;Check(!Buy("molotov",1),"molotov/incendiary share one carry slot");
Check(InventoryPolicy.Kind("weapon_incgrenade")=="molotov" && InventoryPolicy.Item("weapon_incgrenade")==48,"picked-up incendiary keeps its real item identity");
Check(InventoryPolicy.Item("weapon_molotov")==46,"picked-up molotov identity independent of team");
Check(!Buy("he",totalLimit:0),"disabled total capacity respected");
stock["flash"]=0;Check(!Buy("flash",flashLimit:0),"disabled flash capacity respected");
int count=1,created=0;bool finalized=false;
bool Throw()=>InventoryTransaction.Execute(count,n=>count=n,()=>{Check(count==0,"stock debited before projectile creation");created++;return new object();},_=>true,()=>finalized=true,_=>{},()=>count==0);
Check(Throw() && count==0 && created==1 && finalized,"one real smoke consumed once");
Check(!Throw() && !Throw() && created==1,"three rapid requests cannot create three smokes from one");
count=1;Check(Throw() && created==2,"a newly picked-up unit permits another throw");
count=2;int flashes=0;
for(int i=0;i<3;i++)InventoryTransaction.Execute(count,n=>count=n,()=>{flashes++;return new object();},_=>true,()=>{},_=>{},()=>true);
Check(flashes==2 && count==0,"two flashes are consumed, third attempt denied");
count=1;Check(!InventoryTransaction.Execute<object>(count,n=>count=n,()=>null,_=>true,()=>{},_=>{},()=>count==0) && count==1,"failed native creation refunds stock");
count=1;bool cancelled=false;
try{InventoryTransaction.Execute(count,n=>count=n,()=>new object(),_=>true,()=>throw new InvalidOperationException("detach failed"),_=>cancelled=true,()=>count==0);}catch(InvalidOperationException){}
Check(cancelled && count==1,"finalization failure removes projectile and refunds exact remaining owner stock");
count=1;
try{InventoryTransaction.Execute<object>(count,n=>count=n,()=>throw new InvalidOperationException("factory threw"),_=>true,()=>{},_=>{},()=>count==0);}catch(InvalidOperationException){}
Check(count==1,"throwing factory cannot leak an inventory debit");
count=1;InventoryTransaction.Execute<object>(count,n=>count=n,()=>null,_=>true,()=>{},_=>{},()=>false);
Check(count==0,"ownership loss does not invent a refund item");
var grenade=new GrenadeData{ProjectilePosition=new(){X=16,Z=64},ProjectileVelocity=new(){X=700,Z=300},LandingPosition=new(){X=800}};
Check(ReplaySafety.Reject(new(){Z=64},0,0,grenade)==null,"ordinary human-scale trajectory still allowed");
Check(ReplaySafety.Reject(new(){X=500,Z=64},0,0,grenade)=="remoteRelease","remote release still forbidden");
grenade.ProjectileVelocity.X=-700;Check(ReplaySafety.Reject(new(){Z=64},0,0,grenade)=="wrongHeading","backwards release still forbidden");
