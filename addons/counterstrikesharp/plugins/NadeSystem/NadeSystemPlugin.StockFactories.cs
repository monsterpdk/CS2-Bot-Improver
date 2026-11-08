using System;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
namespace NadeSystem;
public partial class NadeSystemPlugin
{
    // Audited native Create(pos, angles, velocity, angularVelocity, owner, itemDef).
    private static readonly MemoryFunctionWithReturn<IntPtr,IntPtr,IntPtr,IntPtr,IntPtr,int,CFlashbangProjectile> _inventoryFlashCreate=new(
        "48 89 5C 24 08 48 89 6C 24 10 48 89 74 24 18 57 48 83 EC 20 48 8B 6C 24 50 49 8B F8 4C 8B C2");
    private static readonly MemoryFunctionWithReturn<IntPtr,IntPtr,IntPtr,IntPtr,IntPtr,int,CDecoyProjectile> _inventoryDecoyCreate=new(
        "40 55 56 48 83 EC 58 48 89 5C 24 70 48 8B D9 48 89 7C 24 78 49 8B F8 4C 89 7C 24 50 4C 8B C2");
    private CBaseCSGrenadeProjectile? CreateStockProjectile(CCSPlayerPawn pawn,Stock stock,Vector release,Vector velocity)
    {
        float yaw=MathF.Atan2(velocity.Y,velocity.X)*180f/MathF.PI;
        float pitch=-MathF.Atan2(velocity.Z,MathF.Sqrt(velocity.X*velocity.X+velocity.Y*velocity.Y))*180f/MathF.PI;
        var angles=new QAngle(pitch,yaw,0);var spin=new Vector(0,0,0);
        CBaseCSGrenadeProjectile? result=stock.Kind switch
        {
            "smoke"=>_smokeCreate.Invoke(release.Handle,angles.Handle,velocity.Handle,spin.Handle,pawn.Handle,stock.Item,pawn.TeamNum),
            "he"=>_heCreate.Invoke(release.Handle,angles.Handle,velocity.Handle,spin.Handle,pawn.Handle,stock.Item),
            "molotov"=>_molotovCreate.Invoke(release.Handle,angles.Handle,velocity.Handle,spin.Handle,pawn.Handle,stock.Item),
            "flash"=>_inventoryFlashCreate.Invoke(release.Handle,angles.Handle,velocity.Handle,spin.Handle,pawn.Handle,stock.Item),
            "decoy"=>_inventoryDecoyCreate.Invoke(release.Handle,angles.Handle,velocity.Handle,spin.Handle,pawn.Handle,stock.Item),
            _=>null
        };
        if(result is {IsValid:true})
        {
            try
            {
            result.TeamNum=pawn.TeamNum;result.Thrower.Raw=pawn.EntityHandle.Raw;
            result.OriginalThrower.Raw=pawn.EntityHandle.Raw;result.OwnerEntity.Raw=pawn.EntityHandle.Raw;
            result.ItemIndex=(ushort)stock.Item;
            }
            catch{if(result.IsValid)result.Remove();throw;}
        }
        return result;
    }
}
