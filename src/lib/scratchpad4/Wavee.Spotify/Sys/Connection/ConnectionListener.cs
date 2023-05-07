using System.Diagnostics;
using System.Threading.Channels;
using LanguageExt.UnsafeValueAccess;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Sys.Connection.Contracts;

namespace Wavee.Spotify.Sys.Connection;

internal static class ConnectionListener<RT> where RT : struct, HasTCP<RT>, HasHttp<RT>
{
    private static AtomHashMap<Guid, Seq<ChannelWriter<SpotifyPacket>>> PacketFutures =
        LanguageExt.AtomHashMap<Guid, Seq<ChannelWriter<SpotifyPacket>>>.Empty;


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
                    if (packet.Command is SpotifyPacketType.MercuryReq) Debugger.Break();
                    //check if there are any futures that can handle this packet
                    //if so, complete the future
                    //if not, create a new packet without any canhandle

                    var futures = PacketFutures.Find(connectionId);
                    if (futures.IsSome)
                    {
                        foreach (var future in futures.ValueUnsafe())
                        {
                            future.TryWrite(packet);
                        }
                    }
                    else
                    {
                        PacketsWithoutPurpose.AddOrUpdate(connectionId,
                            None: () => Seq1(packet),
                            Some: existing => existing.Add(packet));
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
        var newListener = Channel.CreateUnbounded<SpotifyPacket>();

        //check if there are any packets without purpose
        //if so, check if any of them can be handled
        //if so, return the first one
        //if not, add the listener to the listeners
        //if not, add the listener to the listeners

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

        PacketFutures.AddOrUpdate(connectionId,
            None: () => Seq1(newListener.Writer),
            Some: existing => existing.Add(newListener.Writer));

        return Aff<RT, SpotifyPacket>(async _ =>
        {
            try
            {
                await foreach (var package in newListener.Reader.ReadAllAsync(ct))
                {
                    if (shouldHandle(package))
                    {
                        if (removeIfHandled)
                        {
                            PacketFutures.AddOrUpdate(connectionId,
                                None: () => Empty,
                                Some: existing => existing.Filter(x => x != newListener.Writer));
                        }

                        return package;
                    }
                    else
                    {
                        PacketsWithoutPurpose.AddOrUpdate(connectionId,
                            None: () => Seq1(package),
                            Some: existing => existing.Add(package));
                    }
                }
            }
            finally
            {
                newListener.Writer.Complete();
                PacketFutures.AddOrUpdate(connectionId,
                    None: () => Empty,
                    Some: existing => existing.Filter(x => x != newListener.Writer));
            }

            throw new Exception("Connection closed");
        });
    }
}