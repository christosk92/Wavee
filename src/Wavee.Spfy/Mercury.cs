using System.Buffers;
using System.Buffers.Binary;
using System.Threading.Channels;
using Eum.Spotify;
using Google.Protobuf;
using Wavee.Spfy.DefaultServices;

namespace Wavee.Spfy;

public static class Mercury
{
    public static async Task<MercuryResponse> Get(this Guid instanceId, string mercuryUrl)
    {
        var (receiver, seq) = BuildAndSend(instanceId, mercuryUrl);
        var buffer = new ArrayBufferWriter<byte>();
        var headerSize = 0;
        await foreach (var (cmd, readOnlyMemory) in receiver.Reader.ReadAllAsync())
        {
            if (cmd is not (SpotifyPacketType.MercuryReq or SpotifyPacketType.MercuryEvent
                or SpotifyPacketType.MercuryUnsub or SpotifyPacketType.MercurySub))
            {
                continue;
            }

            if (!TryReadPartialMercuryResponse(readOnlyMemory, seq, buffer, ref headerSize, out var response))
            {
                continue;
            }

            receiver.Writer.TryComplete();
            return response;
        }

        throw new NotSupportedException();
    }

    private static bool TryReadPartialMercuryResponse(ReadOnlyMemory<byte> payload,
        ulong checkAgainst,
        ArrayBufferWriter<byte> buffer,
        ref int headerSize,
        out MercuryResponse o)
    {
        var data = payload;
        var seqLen = SeqLen(ref data);
        var seq = Seq(ref data, seqLen);
        if (seq != checkAgainst)
        {
            o = default;
            return false;
        }

        var flags = Flag(ref data);
        var count = Count(ref data);
        for (int i = 0; i < count; i++)
        {
            var part = ParsePart(ref data);
            if (buffer.WrittenCount is 0)
            {
                headerSize = part.Length;
            }

            buffer.Write(part.Span);
        }

        if (flags != 1)
        {
            o = default;
            return false;
        }

        var headerBytes = buffer.WrittenSpan[..headerSize];
        var body = buffer.WrittenMemory[headerSize..];
        var header = Header.Parser.ParseFrom(headerBytes);
        o = new MercuryResponse(header, body);
        return true;
    }

    private static (Channel<SendSpotifyPacket> Receiver, ulong Sequence) BuildAndSend(Guid instanceId,
        string mercuryUrl)
    {
        if (!EntityManager.TryGetMercurySeq(instanceId, out var seqNumber))
        {
            throw new NotSupportedException();
        }

        Span<byte> seq = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64BigEndian(seq, seqNumber);
        var cmd = SpotifyPacketType.MercuryReq;
        var header = new Header
        {
            Uri = mercuryUrl,
            Method = "GET",
            // UserFields =
            // {
            //     new UserField
            //     {
            //         Key = "MC-Etag",
            //         Value = ByteString.FromBase64("NWE3YjJhYWNlZWY0NTE1YTk0NTY4YjE5NjQxZTM1ZDU=")
            //     }
            // }
        };
        ReadOnlyMemory<byte> headerSpan = header.ToByteArray();


        var payloadCount = 0;
        Memory<byte> packet = new byte[
            sizeof(ushort) // seq length
            + seq.Length // seq
            + sizeof(byte) // flags
            + sizeof(ushort) // part count
            + sizeof(ushort) //header length
            + headerSpan.Length // header
            + payloadCount * (sizeof(ushort) + 1) // part length
        ];

        BinaryPrimitives.WriteUInt16BigEndian(packet.Span, (ushort)seq.Length);
        seq.CopyTo(packet.Span.Slice(sizeof(ushort)));
        packet.Span[sizeof(ushort) + seq.Length] = 1; // flags: FINAL
        BinaryPrimitives.WriteUInt16BigEndian(packet.Span.Slice(sizeof(ushort)
                                                                + seq.Length + 1),
            (ushort)(1 + payloadCount)); // part count

        // header length
        BinaryPrimitives.WriteUInt16BigEndian(packet.Span.Slice(sizeof(ushort)
                                                                + seq.Length + 1 + sizeof(ushort)),
            (ushort)headerSpan.Length);

        // header
        headerSpan.CopyTo(packet.Slice(sizeof(ushort)
                                       + seq.Length + 1 + sizeof(ushort) + sizeof(ushort)));

        // for (var index = 0; index < payloadCount; index++)
        // {
        //     //if we are in this loop, we can assume that the payload is not empty
        //     var part = payload.ValueUnsafe()[index].Span;
        //     BinaryPrimitives.WriteUInt16BigEndian(packet.Span.Slice(sizeof(ushort)
        //                                                             + seq.Length + 1 + sizeof(ushort)
        //                                                             + sizeof(ushort) + headerSpan.Length
        //                                                             + index * (sizeof(ushort) + 1)),
        //         (ushort)part.Length);
        //     BinaryPrimitives.WriteUInt16BigEndian(packet.Span.Slice(sizeof(ushort)
        //                                                             + seq.Length + 1 + sizeof(ushort)
        //                                                             + sizeof(ushort) + headerSpan.Length
        //                                                             + index * (sizeof(ushort) + 1)
        //                                                             + sizeof(ushort)),
        //         (ushort)part.Length);
        // }


        var toSend = new SendSpotifyPacket(cmd, packet);
        if (!EntityManager.TryGetClient(instanceId, out var client))
        {
            throw new NotSupportedException();
        }

        return (client.Send(toSend), seqNumber);
    }

    private static ReadOnlyMemory<byte> ParsePart(ref ReadOnlyMemory<byte> data)
    {
        var size = BinaryPrimitives.ReadUInt16BigEndian(data.Span[..2]);
        data = data[2..];
        var body = data[..size];
        data = data[size..];
        return body;
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
}

public readonly record struct MercuryResponse(Header Header, ReadOnlyMemory<byte> Body);