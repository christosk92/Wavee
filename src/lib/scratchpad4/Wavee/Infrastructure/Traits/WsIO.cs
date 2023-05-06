using LanguageExt.Attributes;
using LanguageExt.Effects.Traits;
using System.Net.Http.Headers;
using System.Net.WebSockets;

namespace Wavee.Infrastructure.Traits;

public interface WebsocketIO
{
    ValueTask<WebSocket> Connect(string url, CancellationToken ct = default);

    ValueTask<ReadOnlyMemory<byte>> Receive(WebSocket socket, CancellationToken ct = default);
    ValueTask<Unit> Write(WebSocket socket, ReadOnlyMemory<byte> data, CancellationToken ct = default);
}

/// <summary>
/// Type-class giving a struct the trait of supporting websocket IO
/// </summary>
/// <typeparam name="RT">Runtime</typeparam>
[Typeclass("*")]
public interface HasWebsocket<RT> : HasCancel<RT>
    where RT : struct, HasCancel<RT>
{
    /// <summary>
    /// Access the Websocket synchronous effect environment
    /// </summary>
    /// <returns>Websocket synchronous effect environment</returns>
    Eff<RT, WebsocketIO> WsEff { get; }
}
