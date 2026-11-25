using BotAI;
try {
 var loaded=Gamedata.Load(Path.Combine(AppContext.BaseDirectory,"gamedata.json"),"windows");var keys=loaded.Patches.Select(p=>p.Name).ToHashSet();
 string[] blocked={"AttackState_SkipFireRateCheck","IsNoticable_AlwaysTrue","DefuseBomb_SkipIsVisibleCheck","BombBeep_CT_GlobalHearRange","OnBombPlanted_AllBotsLearnSite","OnAudibleEvent_GlobalHearRange","BombPickup_CT_GlobalHearRange"};
 foreach(var key in blocked)if(!keys.Contains(key)||FairplayPatchPolicy.IsEnabled(key))throw new Exception("Intentional fairness bypass enabled: "+key);
 var enabled=loaded.Patches.Where(p=>FairplayPatchPolicy.IsEnabled(p.Name)).ToArray();var bomb=enabled.Single(p=>p.Name=="TBot_BombsiteSearch_UseKnownPlantedSite");
 if(enabled.Length!=34||!enabled.Any(p=>p.Name=="GameState_Reset"))throw new Exception("Patch selection regression");
 if(bomb.Patch[0]!=0xe8||0x333d60+BitConverter.ToInt32(bomb.Patch,1)!=0x2c94c0)throw new Exception("Corrected official bomb getter target changed; re-audit required");
 if(loaded.Offsets.GameState!=0x5120||loaded.Offsets.IsRoundOver!=8||loaded.Offsets.BombState!=12)throw new Exception("Audited structured offsets changed");
 Console.WriteLine("PASS seven fairness exclusions;34 enabled official patches;corrected bomb getter restored;structured offsets retained");return 0;
}catch(Exception e){Console.Error.WriteLine(e);return 1;}
