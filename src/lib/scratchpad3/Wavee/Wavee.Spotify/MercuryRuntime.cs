using System.Buffers.Binary;
using Eum.Spotify;
using LanguageExt.UnsafeValueAccess;
using Wavee.Infrastructure.Live;
using Wavee.Spotify.Connection;
using Wavee.Spotify.Mercury;

namespace Wavee.Spotify;

public static class MercuryRuntime
{
    private static readonly Ref<HashMap<Guid, ulong>> SequenceIds = Ref(new HashMap<Guid, ulong>());

    public static async ValueTask<MercuryResponse> Send(MercuryMethod method, string uri,
        Option<string> contentType,
        Option<Guid> connectionId)
    {
        //if connectionId is none, return default   
        var connectionMaybe = connectionId.Match(
            Some: id =>
            {
                var k = SpotifyRuntime.Connections.Value.Find(id);
                return (id, k);
            },
            None: () =>
            {
                var k = SpotifyRuntime.Connections.Value.Find(_ => true);
                return k.Match(
                    Some: z => (z.Key, z.Value),
                    None: () => throw new Exception("No connection found"));
            });
        var connection = connectionMaybe.k.Match(Some: r => r, None: () => throw new Exception("No connection found"));

        var connId = connectionMaybe.id;
        var listenerMaybe =
            SpotifyRuntime.SetupListener<WaveeRuntime>(connId)
                .Run(WaveeCore.Runtime);
        var listener =
            listenerMaybe.Match(Succ: r => r, Fail: (e) => throw e);

        var nextSequence = GetNextSequenceId(SequenceIds, connId);
        var resultTask = Task.Run(async () =>
        {
            Seq<ReadOnlyMemory<byte>> partials = Seq<ReadOnlyMemory<byte>>();

            await foreach (var packet in listener.Listener.ReadAllAsync())
            {
                // we are only interested in mercury packets here that have the same sequence id as the request   
                if (packet.Command is SpotifyPacketType.MercuryReq)
                {
                    var data = packet.Data;
                    var seqLen = SeqLen(ref data);
                    var seq = Seq(ref data, seqLen);
                    if (seq != nextSequence)
                    {
                        //not our packet. ignore...
                        continue;
                    }

                    var flags = Flag(ref data);
                    var count = Count(ref data);

                    for (int i = 0; i < count; i++)
                    {
                        var part = ParsePart(ref data);
                        partials = partials.Add(part);
                    }

                    if (flags != 1) continue;

                    var header = Header.Parser.ParseFrom(partials[0].Span);
                    var bodyLength = partials.Skip(1).Fold(0, (acc, x) => acc + x.Length);
                    Memory<byte> body = new byte[bodyLength];
                    var offset = 0;
                    foreach (var part in partials.Skip(1))
                    {
                        part.CopyTo(body.Slice(offset));
                        offset += part.Length;
                    }

                    return new MercuryResponse(header, body);
                }
            }

            return new MercuryResponse();
        });

        //now that our listeners are setup, we can send the request
        var buildPacket = MercuryPacketBuilder.BuildRequest(method, uri, nextSequence, contentType);
        var wrote = connection.TryWrite(buildPacket);

        var response = await resultTask;
        //finished request, unregister listener
        SpotifyRuntime.RemoveListener<WaveeRuntime>(connId, listener.ListenerId);
        return response;
    }

    public static ValueTask<MercuryResponse> Get(string url, Option<Guid> connectionId)
    {
        return Send(MercuryMethod.Get, url, None, connectionId);
    }

    private static ReadOnlyMemory<byte> ParsePart(ref ReadOnlyMemory<byte> data)
    {
        var size = BinaryPrimitives.ReadUInt16BigEndian(data.Span[..2]);
        data = data[2..];
        var body = data[..size];
        data = data[size..];
        return body;
    }

    private static ushort SeqLen(ref ReadOnlyMemory<byte> data)
    {
        var d = data.Span[..2];
        var l = BinaryPrimitives.ReadUInt16BigEndian(d);
        data = data[2..];
        return l;
    }

    private static ulong Seq(ref ReadOnlyMemory<byte> data, int len)
    {
        //  return BinaryPrimitives.ReadUInt64BigEndian(data.Span[2..len]);
        var d = data.Span[..len];
        var l = BinaryPrimitives.ReadUInt64BigEndian(d);
        data = data[len..];
        return l;
    }

    private static ushort Count(ref ReadOnlyMemory<byte> readOnlyMemory)
    {
        var c = BinaryPrimitives.ReadUInt16BigEndian(readOnlyMemory.Span[..2]);
        readOnlyMemory = readOnlyMemory[2..];
        return c;
    }

    private static byte Flag(ref ReadOnlyMemory<byte> data)
    {
        var d = data.Span[..1];
        var l = d[0];
        data = data[1..];
        return l;
    }

    private static ulong GetNextSequenceId(Ref<HashMap<Guid, ulong>> sequenceIds, Guid connectionId)
    {
        //if no item exists, add 0 and return 0
        //if item exists, add 1 and return +1 (so if it was 0, return 0 + 1)

        var nextSequence = atomic(() =>
        {
            return sequenceIds.Swap(x =>
            {
                return x.AddOrUpdate(connectionId,
                    Some: r => r + 1,
                    None: () => 0);
            });
        });
        return nextSequence.Find(connectionId).ValueUnsafe();
    }
}

public readonly record struct MercuryResponse(Header Header, ReadOnlyMemory<byte> Body);