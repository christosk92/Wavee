namespace Wavee.Spotify.Remote.Infrastructure.Traits;

internal interface TimeIO
{
    /// <summary>
    /// Current local date time
    /// </summary>
    ulong Timestamp { get; }
}

/// <summary>
/// Type-class giving a struct the trait of supporting time IO
/// </summary>
/// <typeparam name="RT">Runtime</typeparam>
[Typeclass("*")]
internal interface HasTime<RT> : HasCancel<RT>
    where RT : struct, HasCancel<RT>
{
    /// <summary>
    /// Access the time synchronous effect environment
    /// </summary>
    /// <returns>Time synchronous effect environment</returns>
    Eff<RT, TimeIO> TimeEff { get; }
}