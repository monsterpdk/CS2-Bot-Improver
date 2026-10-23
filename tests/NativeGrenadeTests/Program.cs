using NadeSystem;
void Check(string name,bool actual) { if(!actual)throw new Exception(name);Console.WriteLine("PASS "+name); }
Check("missing smoke factory cannot charge a throw",!NativeGrenadePolicy.Available("smoke",false,true,true));
Check("available smoke factory permits normal decision flow",NativeGrenadePolicy.Available("smoke",true,true,true));
Check("HE remains independent of smoke availability",NativeGrenadePolicy.Available("he",false,true,true));
Check("missing HE factory cannot charge a throw",!NativeGrenadePolicy.Available("he",true,false,true));
Check("molotov remains independently available",NativeGrenadePolicy.Available("molotov",false,false,true));
Check("incendiary alias uses molotov factory",!NativeGrenadePolicy.Available("incgrenade",true,true,false));
Check("flash requires no native factory",NativeGrenadePolicy.Available("flash",false,false,false));
Check("decoy keeps its existing entity path",NativeGrenadePolicy.Available("decoy",false,false,false));
Check("unknown grenade type rejected",!NativeGrenadePolicy.Available("invalid",true,true,true));
