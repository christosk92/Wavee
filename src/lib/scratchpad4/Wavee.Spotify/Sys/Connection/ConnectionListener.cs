using System.Diagnostics;
using System.Threading.Channels;
using LanguageExt.UnsafeValueAccess;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Sys.Connection.Contracts;

namespace Wavee.Spotify.Sys.Connection;

internal static class ConnectionListener<RT> where RT : struct, HasTCP<RT>, HasHttp<RT>
{
    private record FutureSpotifyPacket(Option<Func<SpotifyPacket, bool>> Handle,
        TaskCompletionSource<SpotifyPacket> TaskCompletionSource);

    private static AtomHashMap<Guid, Seq<FutureSpotifyPacket>> PacketFutures =
        LanguageExt.AtomHashMap<Guid, Seq<FutureSpotifyPacket>>.Empty;


    private static AtomHashMap<Guid, Seq<SpotifyPacket>> PacketsWithoutPurpose =
        LanguageExt.AtomHashMap<Guid, Seq<SpotifyPacket>>.Empty;


    // private static AtomHashMap<Guid, Seq<ChannelWriter<SpotifyPacket>>> PacketListeners =
    //     LanguageExt.AtomHashMap<Guid, Seq<ChannelWriter<SpotifyPacket>>>.Empty;
    //
    // private static AtomHashMap<Guid, Seq<SpotifyPacket>> PacketsWithoutPurpose =
    //     LanguageExt.AtomHashMap<Guid, Seq<SpotifyPacket>>.Empty;

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

                    //check if there are any futures that can handle this packet
                    //if so, complete the future
                    //if not, create a new packet without any canhandle

                    var futures = PacketFutures.Find(connectionId);
                    if (futures.IsSome)
                    {
                        var (handle, tcs) = futures.ValueUnsafe().Head;
                        if (handle.IsNone || (handle.IsSome && handle.ValueUnsafe()(packet)))
                        {
                            tcs.SetResult(packet);
                            PacketFutures.AddOrUpdate(connectionId,
                                None: () => Empty,
                                Some: existing => existing);
                        }
                    }
                    else
                    {
                        PacketsWithoutPurpose.AddOrUpdate(connectionId,
                            None: () => Seq1(packet),
                            Some: existing => existing.Add(packet));
                        //     PacketFutures.AddOrUpdate(connectionId,
                        //         None: () => Empty,
                        //         Some: existing =>
                        //             existing.Add(new FutureSpotifyPacket(None, new TaskCompletionSource<SpotifyPacket>())));
                    }
                }
            }, TaskCreationOptions.LongRunning);
            return unit;
        });
    }

    public static Aff<RT, SpotifyPacket> ConsumePacket(
        Guid connectionId,
        Func<SpotifyPacket, bool> shouldHandle,
        bool removeIfHandled,
        CancellationToken ct = default)
    {
        //check if there are packets without purpose
        //if so, check if we can handle any of them
        //if so, return
        //if not, wait for a packet to arrive and check if we can handle it
        //if so, return

        //check if there are packets without purpose
        var packetsWithoutPurpose = PacketsWithoutPurpose.Find(connectionId);
        if (packetsWithoutPurpose.IsSome)
        {
            var packets = packetsWithoutPurpose.ValueUnsafe();
            foreach (var packet in packets)
            {
                if (shouldHandle(packet))
                {
                    if (removeIfHandled)
                    {
                        PacketsWithoutPurpose.AddOrUpdate(connectionId,
                            None: () => Empty,
                            Some: existing => existing.Filter(x => x != packet));
                    }

                    return SuccessAff(packet);
                }
            }
        }

        var tcs = new TaskCompletionSource<SpotifyPacket>();
        PacketFutures
            .AddOrUpdate(connectionId,
                None: () => Seq1(new FutureSpotifyPacket(Some(shouldHandle), tcs)),
                Some: existing => existing.Add(new FutureSpotifyPacket(Some(shouldHandle), tcs)));

        //wait for a packet to arrive and check if we can handle it
        return tcs.Task.WaitAsync(ct).ToAff();
    }
}