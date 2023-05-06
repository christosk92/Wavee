using System.Collections.Concurrent;
using System.Diagnostics;
using LanguageExt.UnsafeValueAccess;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Sys.Connection.Contracts;

namespace Wavee.Spotify.Sys.Connection;

internal static class ConnectionListener<RT> where RT : struct, HasTCP<RT>, HasHttp<RT>
{
    private static AtomHashMap<Guid, (SemaphoreSlim, Seq<SpotifyPacket>)> Packets =
        LanguageExt.AtomHashMap<Guid, (SemaphoreSlim, Seq<SpotifyPacket>)>.Empty;

    public static Aff<RT, Unit> StartListening(Guid connectionId)
    {
        return Aff<RT, Unit>(async (rt) =>
        {
            await Task.Factory.StartNew(async () =>
            {
                //check if connectionId is in Connections and CancellationTokens
                //if not, return
                var channelMaybe = SpotifyConnection<RT>.ConnectionConsumer.Value.Find(connectionId);
                if (channelMaybe.IsNone)
                {
                    return;
                }

                var channel = channelMaybe.ValueUnsafe();
                await foreach (var packet in channel.ReadAllAsync())
                {
                    Debug.WriteLine($"Received packet {packet}");
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

    public static Aff<RT, SpotifyPacket> ConsumePacket(
        Guid connectionId,
        Func<SpotifyPacket, bool> consume,
        CancellationToken ct = default)
    {
        return Aff<RT, SpotifyPacket>(async _ =>
        {
            while (!ct.IsCancellationRequested)
            {
                var queueData = Packets.Find(connectionId);
                if (queueData.IsNone) break;

                var (semaphore, packets) = queueData.ValueUnsafe();

                await semaphore.WaitAsync(ct);

                var packetToConsume = packets.Find(consume);
                if (packetToConsume.IsSome)
                {
                    var result = packetToConsume.ValueUnsafe();
                    Packets.AddOrUpdate(connectionId,
                        None: () => (semaphore, packets.Filter(x => x != result)),
                        Some: existing => (existing.Item1, existing.Item2.Filter(x => x != result)));
                    return result;
                }

                semaphore.Release();
            }

            throw new OperationCanceledException("Cancelled");
        });
    }
}