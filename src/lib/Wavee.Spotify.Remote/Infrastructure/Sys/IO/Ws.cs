using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using LanguageExt.Common;
using Wavee.Spotify.Remote.Infrastructure.Traits;

namespace Wavee.Spotify.Remote.Infrastructure.Sys.IO;

internal static class Ws<RT>
    where RT : struct, HasCancel<RT>, HasWs<RT>
{
    /// <summary>
    /// Connect to a Websocket server
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<RT, Unit> Connect(string url) =>
        from ct in cancelToken<RT>()
        from _ in default(RT).WsEff.MapAsync(e => e.Connect(url, ct))
        select unit;

    /// <summary>
    /// Receive a message from the Websocket server. This will block until a message is received so be careful of deadlocks.
    /// </summary>
    /// <returns></returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<RT, ReadOnlyMemory<byte>> Receive() =>
        from ct in cancelToken<RT>()
        from res in default(RT).WsEff.MapAsync(e => e.Receive(ct))
        select res;

    /// <summary>
    /// Continuously listen for incoming messages and apply the provided function to handle them.
    /// </summary>
    /// <param name="handleMessage">A function to handle incoming messages.</param>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<RT, Unit> Listen(
        Func<ReadOnlyMemory<byte>, Eff<RT, Unit>> handleMessage,
        Func<Error, Aff<RT, Unit>> onError,
        CancellationToken cancelToken) =>
        Aff<RT, Unit>(async env =>
        {
            while (!cancelToken.IsCancellationRequested)
            {
                // Get the cancellation token
                var ct = env.CancellationToken;

                // Receive a message
                var message =
                    await default(RT).WsEff.MapAsync(e => e.Receive(ct)).Run(env);

                // Match the result and execute handleMessage or onError
                await message.Match(
                    Succ: msg => handleMessage(msg).ToAff().Run(env),
                    Fail: ex => onError(ex).Run(env)
                );
            }

            return unit;
        });

    /// <summary>
    /// Continuously listen for incoming messages and apply the provided function to handle them.
    /// </summary>
    /// <param name="handleMessage">A function to handle incoming messages.</param>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<RT, Unit> SendText(string json) =>
        from ct in cancelToken<RT>()
        from _ in default(RT).WsEff.MapAsync(e => e.SendText(json, ct))
        select unit;
}