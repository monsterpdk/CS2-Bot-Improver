using BotAI;
try {
 string[] blocked={"TBot_BombsiteSearch_UseKnownPlantedSite","AttackState_SkipFireRateCheck","IsNoticable_AlwaysTrue","DefuseBomb_SkipIsVisibleCheck","BombBeep_CT_GlobalHearRange","OnBombPlanted_AllBotsLearnSite","OnAudibleEvent_GlobalHearRange","BombPickup_CT_GlobalHearRange"};
 foreach(var key in blocked)if(!WindowsPatchDefinitions.All.ContainsKey(key)||FairplayPatchPolicy.IsEnabled(key))throw new Exception("Unsafe bypass enabled: "+key);
 var enabled=WindowsPatchDefinitions.All.Keys.Where(FairplayPatchPolicy.IsEnabled).ToArray();
 if(enabled.Contains(blocked[0])||!enabled.Contains("GameState_Reset"))throw new Exception("Patch selection regression");
 var bytes=WindowsPatchDefinitions.All[blocked[0]].patch.Split(' ').Select(x=>Convert.ToByte(x,16)).ToArray();
 if(0x336980+BitConverter.ToInt32(bytes,1)!=0x2cc0a0)throw new Exception("Legacy evidence changed: review rather than enabling it");
 Console.WriteLine("PASS eight unsafe bypasses excluded; original bomb search preserved; unrelated compatibility retained; legacy wrong-call regression guarded");
 return 0;
}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
