using System;
namespace NadeSystem;
internal static class InventoryTransaction
{
    internal static bool Execute<T>(int count,Action<int> setCount,Func<T?> create,Func<T,bool> valid,Action finalize,Action<T> cancel,Func<bool> canRefund) where T:class
    {
        if(count<=0)return false;
        T? projectile=null;
        try
        {
            setCount(count-1);
            projectile=create();
            if(projectile is null || !valid(projectile))
            {if(canRefund())setCount(count);return false;}
            finalize();return true;
        }
        catch
        {
            if(projectile is not null && valid(projectile))cancel(projectile);
            if(canRefund())setCount(count);
            throw;
        }
    }
}
