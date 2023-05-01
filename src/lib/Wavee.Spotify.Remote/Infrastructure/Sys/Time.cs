using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Wavee.Spotify.Remote.Infrastructure.Traits;

namespace Wavee.Spotify.Remote.Infrastructure.Sys;

/// <summary>
/// DateTime IO 
/// </summary>
internal static class Time<RT>
    where RT : struct, HasTime<RT>
{
    /// <summary>
    /// Current local date time
    /// </summary>
    public static Eff<RT, ulong> timestamp
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default(RT).TimeEff.Map(static e => e.Timestamp);
    }
}