namespace Wavee.Spotify.Remote.Infrastructure.Traits;

internal interface WsIO
{
    ValueTask<Unit> Connect(string url, CancellationToken ct = default);
    
    ValueTask<ReadOnlyMemory<byte>> Receive(CancellationToken ct = default);
}

/// <summary>
/// Type-class giving a struct the trait of supporting Websocket IO
/// </summary>
/// <typeparam name="RT">Runtime</typeparam>
[Typeclass("*")]
internal interface HasWs<RT> : HasCancel<RT>
    where RT : struct, HasCancel<RT>
{
    /// <summary>
    /// Access the WS synchronous effect environment
    /// </summary>
    /// <returns>WS synchronous effect environment</returns>
    Eff<RT, WsIO> WsEff { get; }
}