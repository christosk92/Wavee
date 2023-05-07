using System.Diagnostics;
using LanguageExt.UnsafeValueAccess;
using Wavee.Infrastructure.Traits;

namespace Wavee.Spotify.Sys.Remote;

internal static class WebsocketConnectionListener<RT> where RT : struct, HasWebsocket<RT>, HasHttp<RT>
{
    private static AtomHashMap<Guid, (SemaphoreSlim, Seq<SpotifyWebsocketMessage>)> Packets =
        LanguageExt.AtomHashMap<Guid, (SemaphoreSlim, Seq<SpotifyWebsocketMessage>)>.Empty;

    public static Aff<RT, Unit> StartListening(Guid connectionId)
    {
        return Aff<RT, Unit>(async (rt) =>
        {
            await Task.Factory.StartNew(async () =>
            {
                //check if connectionId is in Connections and CancellationTokens
                //if not, return
                var channelMaybe = SpotifyRemoteClient<RT>.ConnectionConsumer.Value.Find(connectionId);
                if (channelMaybe.IsNone)
                {
                    return;
                }

                var channel = channelMaybe.ValueUnsafe();
                await foreach (var packet in channel.ReadAllAsync())
                {
                    Debug.WriteLine($"Received websocket message {packet}");
                    Packets.AddOrUpdate(connectionId,
                        None: () =>
                        {
                            var semp = new SemaphoreSlim(0);
                            return (semp, Seq1(packet));
                        },
                        Some: existing => (existing.Item1, existing.Item2.Add(packet)));
                    Packets.Find(connectionId).IfSome(existing => { existing.Item1.Release(); });
                }
            }, TaskCreationOptions.LongRunning);
            return unit;
        });
    }

    public static Aff<RT, SpotifyWebsocketMessage> ConsumePacket(
        Guid connectionId,
        Func<SpotifyWebsocketMessage, bool> shouldHandle,
        Func<bool> removeIfHandled,
        CancellationToken ct = default)
    {
        return Aff<RT, SpotifyWebsocketMessage>(async _ =>
        {
            while (!ct.IsCancellationRequested)
            {
                var queueData = Packets.Find(connectionId);
                if (queueData.IsNone) break;

                var (semaphore, packets) = queueData.ValueUnsafe();

                await semaphore.WaitAsync(ct);

                var packetToConsume = packets.Find(shouldHandle);
                if (packetToConsume.IsSome)
                {
                    var result = packetToConsume.ValueUnsafe();
                    Packets.AddOrUpdate(connectionId,
                        None: () => (semaphore, packets.Filter(x => x != result)),
                        Some: existing => (existing.Item1, 
                            removeIfHandled() ?
                            existing.Item2.Filter(x => x != result) : existing.Item2));
                    return result;
                }

                semaphore.Release();
            }

            throw new OperationCanceledException("Cancelled");
        });
    }
}