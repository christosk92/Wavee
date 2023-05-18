using System.Buffers.Binary;
using System.Threading.Channels;
using Eum.Spotify;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.Tcp;

namespace Wavee.Spotify.Infrastructure.Mercury;

public struct MercuryClient
{
    private static Atom<HashMap<Guid, ulong>> _seq = Atom(LanguageExt.HashMap<Guid, ulong>.Empty);

    private readonly Guid _connectionId;

    private readonly Func<Option<SpotifySendPacket>, Func<SpotifySendPacket, bool>, ChannelReader<SpotifySendPacket>>
        _subscribe;

    private readonly Action<ChannelReader<SpotifySendPacket>> _removePackageListener;

    internal MercuryClient(
        Guid connectionId,
        Func<
            Option<SpotifySendPacket>,
            Func<SpotifySendPacket, bool>, ChannelReader<SpotifySendPacket>> subscribe,
        Action<ChannelReader<SpotifySendPacket>> removePackageListener)
    {
        _connectionId = connectionId;
        _subscribe = subscribe;
        _removePackageListener = removePackageListener;
    }

    public readonly async Task<MercuryResponse> Get(string uri, CancellationToken ct = default)
    {
        var connectionId = _connectionId;
        var nextSequences = atomic(() =>
        {
            return _seq
                .Swap(x => x.AddOrUpdate(connectionId,
                    Some: s => s + 1,
                    None: () => 0))
                .ValueUnsafe();
        });
        var nextSequence = nextSequences.Find(connectionId).ValueUnsafe();

        var packet = MercuryRequests.Build(
            nextSequence,
            MercuryMethod.Get,
            uri,
            null,
            Empty);

        var reader = _subscribe(packet, p => IsOurPackage(p, nextSequence));

        Seq<ReadOnlyMemory<byte>> partials = Seq<ReadOnlyMemory<byte>>();
        await foreach (var receivedPacket in reader.ReadAllAsync(ct))
        {
            var data = receivedPacket.Data;
            var seqLen = SeqLenRef(ref data);
            var foundSeq = SeqRef(ref data, seqLen);
            var flags = Flag(ref data);
            var count = Count(ref data);
            for (int i = 0; i < count; i++)
            {
                var part = ParsePart(ref data);
                partials = partials.Add(part);
            }

            if (flags != 1)
                continue;
            var header = Header.Parser.ParseFrom(partials[0].Span);
            var bodyLength = partials.Skip(1).Fold(0, (acc, x) => acc + x.Length);
            Memory<byte> body = new byte[bodyLength];
            var offset = 0;
            foreach (var part in partials.Skip(1))
            {
                part.CopyTo(body.Slice(offset));
                offset += part.Length;
            }

            _removePackageListener(reader);
            return new MercuryResponse(header, body);
        }

        return default;
    }

    private static bool IsOurPackage(SpotifySendPacket spotifySendPacket, ulong nextSequence)
    {
        if (spotifySendPacket.Command is SpotifyPacketType.MercuryEvent
            or SpotifyPacketType.MercuryReq
            or SpotifyPacketType.MercurySub
            or SpotifyPacketType.MercuryUnsub)
        {
            var seqLength = BinaryPrimitives.ReadUInt16BigEndian(spotifySendPacket.Data.Span.Slice(0, 2));
            var calculatedSeq = BinaryPrimitives.ReadUInt64BigEndian(spotifySendPacket.Data.Span.Slice(2, seqLength));
            if (calculatedSeq != nextSequence)
                return false;
            return true;
        }

        return false;
    }

    private static ReadOnlyMemory<byte> ParsePart(ref ReadOnlyMemory<byte> data)
    {
        var size = BinaryPrimitives.ReadUInt16BigEndian(data.Span[..2]);
        data = data[2..];
        var body = data[..size];
        data = data[size..];
        return body;
    }

    private static ushort SeqLenRef(ref ReadOnlyMemory<byte> data)
    {
        var d = data.Span[..2];
        var l = BinaryPrimitives.ReadUInt16BigEndian(d);
        data = data[2..];
        return l;
    }

    private static ulong SeqRef(ref ReadOnlyMemory<byte> data, int len)
    {
        //  return BinaryPrimitives.ReadUInt64BigEndian(data.Span[2..len]);
        var d = data.Span[..len];
        var l = BinaryPrimitives.ReadUInt64BigEndian(d);
        data = data[len..];
        return l;
    }

    private static ushort SeqLen(ReadOnlyMemory<byte> data)
    {
        var d = data.Span[..2];
        var l = BinaryPrimitives.ReadUInt16BigEndian(d);
        return l;
    }

    private static ulong Seq(ReadOnlyMemory<byte> data, int len)
    {
        //  return BinaryPrimitives.ReadUInt64BigEndian(data.Span[2..len]);
        var d = data.Span[..len];
        var l = BinaryPrimitives.ReadUInt64BigEndian(d);
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
}