using System;

namespace Wavee.UI.WinUI.Navigation;

public readonly record struct CachingPolicy(Func<CachedPageRecord, int, bool> ShouldKeepInCache)
{
    public static CachingPolicy DropAfter(int depth) => new CachingPolicy((record, i) => KeepCacheFunc(record, i, depth));
    public static CachingPolicy AlwaysYesPolicy = new CachingPolicy(((_, _) => true));

    private static bool KeepCacheFunc(CachedPageRecord record, int depth, int depthDropCondition)
    {
        return true;
    }
}