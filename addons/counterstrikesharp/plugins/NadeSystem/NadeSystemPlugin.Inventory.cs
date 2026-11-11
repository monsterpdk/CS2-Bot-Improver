using Fairplay.Diagnostics;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
namespace NadeSystem;
public partial class NadeSystemPlugin
{
    private sealed record Stock(CBasePlayerWeapon Weapon,string Kind,int AmmoType,int Count,int Item);
    private sealed record Reservation(nint Pawn,uint PawnHandle,string Kind,int Epoch);
    private readonly HashSet<nint> _boughtInventory=new();
    private readonly Dictionary<nint,Reservation> _pendingInventory=new();
    private int _inventoryEpoch;
    private bool _inventoryFault;
    private long _inventoryRows;
    private string _inventoryLog="";
    private DiagnosticLog _inventoryDiagnostics=null!;
    private readonly List<string> _inventoryPendingLog=new();
    private bool _purchasingInventory;
    private long _boughtUnits,_consumedUnits,_noStock,_purchaseRejected;
    private static int CvarLimit(string name,int fallback)=>ConVar.Find(name)?.GetPrimitiveValue<int>()??fallback;
    private void InitInventory()
    {
        _inventoryDiagnostics=new(ModuleDirectory,"inventory");_inventoryLog=_inventoryDiagnostics.Path;
        AddCommand("bot_nades_diagnostics","Detailed inventory logs: on/off (default off)",(_,info)=>
        {
            var value=info.ArgCount>1?info.GetArg(1):"";
            if(value is not ("on" or "off")){info.ReplyToCommand("Usage: bot_nades_diagnostics on|off");return;}
            FlushInventoryLog();_inventoryDiagnostics.SetEnabled(value=="on");_inventoryRows=0;
            info.ReplyToCommand($"[NadeInventory] diagnostics={_inventoryDiagnostics.Enabled} maxBytes={2*DiagnosticLog.FileLimit}");
        });
        RegisterListener<Listeners.OnMapStart>(_=>ResetInventoryRound());
        RegisterEventHandler<EventItemPickup>((ev,_)=>
        {
            if(ev.Userid is {IsValid:true,IsBot:true} p && InventoryPolicy.Kind("weapon_"+ev.Item) is {} kind)
                InventoryLog(new {action="acquire_event",slot=p.Slot,kind,source=_purchasingInventory?"round_purchase":"native_pickup"},!_purchasingInventory);
            return HookResult.Continue;
        });
        AddCommand("bot_nades_inventory","Show actual carried grenade stock; purchases/consumption are bounded by inventory",(_,info)=>
        {
            info.ReplyToCommand($"[NadeInventory] version={ModuleVersion} diagnostics={_inventoryDiagnostics.Enabled} fault={_inventoryFault} bought={_boughtUnits} consumed={_consumedUnits} noStock={_noStock} purchaseRejected={_purchaseRejected} log={_inventoryLog}");
            if(_freezeEndTime<=0 || _roundOver)return;
            foreach(var p in Fairplay.Runtime.LivePlayers.Get().Where(p=>p.IsValid && p.IsBot && p.PawnIsAlive))
                info.ReplyToCommand($"[NadeInventory] {p.Slot} {p.PlayerName}: "+string.Join(" ",Owned(p).GroupBy(s=>s.Kind).Select(g=>$"{g.Key}={g.Sum(s=>s.Count)}")));
        });
    }
    private void ResetInventoryRound(){_inventoryEpoch++;_boughtInventory.Clear();_pendingInventory.Clear();}
    public override void Unload(bool hotReload){FlushInventoryLog();_inventoryFault=true;ResetInventoryRound();}
    private List<Stock> Owned(CCSPlayerController bot)
    {
        if(_inventoryFault)return new();
        try{return ReadOwned(bot);}catch(Exception ex){InventoryFailure(ex);return new();}
    }
    private List<Stock> ReadOwned(CCSPlayerController bot)
    {
        var result=new List<Stock>();
        var pawn=bot.PlayerPawn.Value;var services=pawn?.WeaponServices;
        if(!bot.IsValid || !bot.PawnIsAlive || pawn is not {IsValid:true} || services is null)return result;
        var seen=new HashSet<int>();
        foreach(var handle in services.MyWeapons)
        {
            var weapon=handle.Value;if(weapon is not {IsValid:true})continue;
            var kind=InventoryPolicy.Kind(weapon.DesignerName);if(kind is null)continue;
            if(weapon.OwnerEntity.Raw!=pawn.EntityHandle.Raw)continue;
            var data=weapon.VData;if(data is null)continue;int index=data.PrimaryAmmoType;
            if(index<0 || index>=services.Ammo.Length || !seen.Add(index))continue;
            int count=services.Ammo[index];if(count<=0)continue;
            if(count>InventoryPolicy.Limit(kind,CvarLimit("ammo_grenade_limit_flashbang",2)))throw new InvalidOperationException($"Unexpected carried grenade count {kind}={count}");
            result.Add(new(weapon,kind,index,count,InventoryPolicy.Item(weapon.DesignerName)));
        }
        return result;
    }
    private bool HasInventory(CCSPlayerController bot,string kind)=>!_inventoryFault && Owned(bot).Any(s=>s.Kind==kind);
    private void SetAmmo(CCSPlayerPawn pawn,int index,int value)
    {
        var services=pawn.WeaponServices!;services.Ammo[index]=(ushort)value;
        if(Schema.IsSchemaFieldNetworked("CPlayer_WeaponServices","m_iAmmo"))
            NativeAPI.SchemaNetworkStateChanged(services.__m_pChainEntity.Handle,(uint)Schema.GetSchemaOffset("CPlayer_WeaponServices","m_iAmmo"),(uint)index,uint.MaxValue);
    }
    private void RemoveEmpty(CCSPlayerPawn pawn,CBasePlayerWeapon weapon)
    {
        // In build 2000942 the CSS signature resolves to the WeaponServices method,
        // not a pawn method. Its callees read MyWeapons at +0x48 and ActiveWeapon
        // at +0x60. The public pawn wrapper passes the wrong receiver on this build.
        var services=pawn.WeaponServices ?? throw new InvalidOperationException("Missing weapon services");
        if(Schema.GetSchemaOffset("CPlayer_WeaponServices","m_hMyWeapons")!=0x48 ||
           Schema.GetSchemaOffset("CPlayer_WeaponServices","m_hActiveWeapon")!=0x60)
            throw new InvalidOperationException("Unaudited weapon-services layout");
        VirtualFunctions.RemovePlayerItemFunc.Invoke(services.Handle,weapon.Handle);
        if(pawn.WeaponServices!.MyWeapons.Any(w=>w.Raw==weapon.EntityHandle.Raw))throw new InvalidOperationException("Consumed grenade did not detach from inventory");
        if(weapon.IsValid)weapon.Remove();
    }
    private void BuyRoundInventory(CCSPlayerController bot)
    {
        if(_botNadesMode=="off" || _inventoryFault || !bot.IsValid || !bot.IsBot || !bot.PawnIsAlive || bot.HasBeenControlledByPlayerThisRound)return;
        var pawn=bot.PlayerPawn.Value;var money=bot.InGameMoneyServices;
        if(pawn is not {IsValid:true} || money is null || !_boughtInventory.Add(pawn.Handle))return;
        if(!pawn.InBuyZone && CvarLimit("mp_buy_anywhere",0)==0){_purchaseRejected++;return;}
        try
        {
            bool ct=bot.TeamNum==3;int budget=Math.Min(money.Account,GetRoundSpendCap(ct,_poorBots.Contains(bot.Index)));
            var optional=new[]{"he","flash","molotov","decoy"}.OrderBy(_=>Random.Shared.Next()).ToList();
            // Smoke first; optional types vary. A second flash is possible within the same four-item limit.
            var order=new[]{"smoke"}.Concat(optional).Concat(new[]{"flash"});
            foreach(var kind in order)
            {
                if(kind=="decoy" && Random.Shared.NextDouble()>=0.12)continue;
                string name=kind switch {"smoke"=>"weapon_smokegrenade","he"=>"weapon_hegrenade","flash"=>"weapon_flashbang","molotov"=>ct?"weapon_incgrenade":"weapon_molotov",_=>"weapon_decoy"};
                int quoted=kind=="decoy"?50:(ct?CostCT:CostT)[kind];
                var before=Owned(bot);var totals=before.GroupBy(s=>s.Kind).ToDictionary(g=>g.Key,g=>g.Sum(s=>s.Count));
                if(_inventoryFault)return;
                if(!InventoryPolicy.CanBuy(kind,totals,before.Sum(s=>s.Count),CvarLimit("ammo_grenade_limit_total",4),CvarLimit("ammo_grenade_limit_flashbang",2),quoted,money.Account,budget))
                {if(quoted>money.Account || quoted>budget){NadeStats(kind).MoneyRejected++;_purchaseRejected++;}continue;}
                // Debit before granting: a failed grant must never leave an unpaid grenade.
                money.Account-=quoted;budget-=quoted;
                Utilities.SetStateChanged(bot,"CCSPlayerController","m_pInGameMoneyServices");
                CCSWeaponBase? granted;
                try{_purchasingInventory=true;granted=bot.GiveNamedItem<CCSWeaponBase>(name);}
                finally{_purchasingInventory=false;}
                var after=Owned(bot);int countBefore=totals.GetValueOrDefault(kind);int countAfter=after.Where(s=>s.Kind==kind).Sum(s=>s.Count);
                if(granted is not {IsValid:true} || countAfter!=countBefore+1)throw new InvalidOperationException("Grenade purchase did not add exactly one carried unit");
                int price=granted.VData?.Price??quoted;if(price<=0)price=quoted;
                if(price-quoted>money.Account || price-quoted>budget)
                {
                    var item=after.First(s=>s.Kind==kind);SetAmmo(pawn,item.AmmoType,item.Count-1);
                    if(item.Count==1)RemoveEmpty(pawn,item.Weapon);
                    money.Account+=quoted;budget+=quoted;
                    Utilities.SetStateChanged(bot,"CCSPlayerController","m_pInGameMoneyServices");
                    NadeStats(kind).MoneyRejected++;_purchaseRejected++;continue;
                }
                money.Account+=quoted-price;budget+=quoted-price;_boughtUnits++;NadeStats(kind).Purchased++;
                Utilities.SetStateChanged(bot,"CCSPlayerController","m_pInGameMoneyServices");
                InventoryLog(new {action="purchase",slot=bot.Slot,kind,cost=price,before=countBefore,after=countAfter,money=money.Account},false);
            }
        }catch(Exception ex){InventoryFailure(ex);}
    }
    private void InventoryFailure(Exception ex)
    {
        if(_inventoryFault)return;_inventoryFault=true;_inventoryDiagnostics.Error(ex);
        InventoryLog(new {action="inventory_fault",error=ex.Message});
        Logger.LogError(ex,"[NadeInventory] Inventory processing stopped rather than granting unowned projectiles");
    }
    private void InventoryLog(object data,bool flush=true)
    {
        if(!_inventoryDiagnostics.Enabled || _inventoryRows>=4096)return;
        try
        {
            _inventoryPendingLog.Add(JsonSerializer.Serialize(new {utc=DateTime.UtcNow,map=Server.MapName,time=Server.CurrentTime,data}));_inventoryRows++;
            if(flush || _inventoryPendingLog.Count>=64)FlushInventoryLog();
        }catch(Exception ex){Logger.LogWarning(ex,"[NadeInventory] Could not write diagnostic row");_inventoryRows=4096;}
    }
    private void FlushInventoryLog()
    {
        if(_inventoryPendingLog.Count==0)return;
        try{foreach(var row in _inventoryPendingLog)_inventoryDiagnostics.Append(row);_inventoryPendingLog.Clear();}
        catch(Exception ex){Logger.LogWarning(ex,"[NadeInventory] Could not save diagnostic batch");_inventoryPendingLog.Clear();_inventoryRows=4096;}
    }
    private void QueueInventoryThrow(CCSPlayerController bot,string kind,Func<(Vector Release,Vector Velocity)?> geometry,Action? committed=null)
    {
        if(_inventoryFault || _roundOver || _botNadesMode=="off" || !bot.IsValid || !bot.PawnIsAlive || bot.HasBeenControlledByPlayerThisRound || _replayBots.Contains(bot.Index))return;
        var pawn=bot.PlayerPawn.Value;if(pawn is not {IsValid:true} || _pendingInventory.ContainsKey(pawn.Handle))return;
        if(!HasInventory(bot,kind)){_noStock++;NadeStats(kind).InventoryRejected++;return;}
        var reservation=new Reservation(pawn.Handle,pawn.EntityHandle.Raw,kind,_inventoryEpoch);_pendingInventory[pawn.Handle]=reservation;
        NadeStats(kind).Planned++;
        Server.NextFrame(()=>
        {
            try
            {
                if(_inventoryFault || _roundOver || _botNadesMode=="off" || reservation.Epoch!=_inventoryEpoch || !bot.IsValid || !bot.PawnIsAlive || bot.HasBeenControlledByPlayerThisRound || bot.PlayerPawn.Value?.Handle!=reservation.Pawn || bot.PlayerPawn.Value?.EntityHandle.Raw!=reservation.PawnHandle)return;
                var actual=bot.PlayerPawn.Value!;var stock=Owned(bot).FirstOrDefault(s=>s.Kind==kind);
                if(stock is null){_noStock++;NadeStats(kind).InventoryRejected++;return;}
                // Never invalidate an actively held weapon or race a native throw.
                if(InventoryPolicy.Kind(actual.WeaponServices!.ActiveWeapon.Value?.DesignerName??"") is not null)return;
                var values=geometry();if(values is null){NadeStats(kind).SafetyRejected++;return;}
                bool created=InventoryTransaction.Execute(stock.Count,
                    count=>SetAmmo(actual,stock.AmmoType,count),
                    ()=>CreateStockProjectile(actual,stock,values.Value.Release,values.Value.Velocity),
                    projectile=>projectile.IsValid,
                    ()=>{if(stock.Count==1)RemoveEmpty(actual,stock.Weapon);},
                    projectile=>projectile.Remove(),
                    ()=>actual.IsValid && stock.Weapon.IsValid && actual.WeaponServices.Ammo[stock.AmmoType]==stock.Count-1 && actual.WeaponServices.MyWeapons.Any(w=>w.Raw==stock.Weapon.EntityHandle.Raw));
                if(!created){NadeStats(kind).Failed++;return;}
                _consumedUnits++;NadeStats(kind).Consumed++;NadeStats(kind).Created++;
                InventoryLog(new {action="throw",slot=bot.Slot,kind,item=stock.Item,ammoType=stock.AmmoType,before=stock.Count,after=stock.Count-1});
                _replayBots.Add(bot.Index);var epoch=_inventoryEpoch;
                AddTimer(1,()=>{if(epoch==_inventoryEpoch)_replayBots.Remove(bot.Index);});
                committed?.Invoke();AnnounceGrenadeThrow(bot,kind);
            }catch(Exception ex){InventoryFailure(ex);}
            finally{if(_pendingInventory.TryGetValue(reservation.Pawn,out var current) && ReferenceEquals(current,reservation))_pendingInventory.Remove(reservation.Pawn);}
        });
    }
}
