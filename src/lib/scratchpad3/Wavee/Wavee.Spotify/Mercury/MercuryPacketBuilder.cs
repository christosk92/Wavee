using System.Buffers.Binary;
using Eum.Spotify;
using Google.Protobuf;
using LanguageExt.UnsafeValueAccess;
using Wavee.Spotify.Connection;

namespace Wavee.Spotify.Mercury;

internal static class MercuryPacketBuilder
{
    public static SpotifyPacket BuildGetRequest(string url, ulong nextSequence)
    {
        return BuildMercuryPacket(nextSequence, MercuryMethod.Get, url, None, None);
    }

    public static SpotifyPacket BuildRequest(MercuryMethod method, string url, ulong nextSequence,
        Option<string> contentType)
    {
        return BuildMercuryPacket(nextSequence, method, url, contentType, None);
    }

    private static SpotifyPacket BuildMercuryPacket(
        ulong seqNumber,
        MercuryMethod mercuryMethod,
        string uri,
        Option<string> contentType,
        Option<Seq<Memory<byte>>> payload)
    {
        Span<byte> seq = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64BigEndian(seq, seqNumber);

        var cmd = mercuryMethod switch
        {
            MercuryMethod.Get => SpotifyPacketType.MercuryReq,
            MercuryMethod.Sub => SpotifyPacketType.MercurySub,
            MercuryMethod.Unsub => SpotifyPacketType.MercuryUnsub,
            MercuryMethod.Send => SpotifyPacketType.MercuryReq,
            _ => throw new ArgumentOutOfRangeException()
        };

        var header = new Header
        {
            Uri = uri,
            Method = mercuryMethod.ToString().ToUpper()
        };

        // if (ContentType != null) header.ContentType = ContentType;
        contentType.IfSome(c => header.ContentType = c);
        ReadOnlyMemory<byte> headerSpan = header.ToByteArray();

        var payloadCount = payload.Count();
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

        for (var index = 0; index < payloadCount; index++)
        {
            //if we are in this loop, we can assume that the payload is not empty
            var part = payload.ValueUnsafe()[index].Span;
            BinaryPrimitives.WriteUInt16BigEndian(packet.Span.Slice(sizeof(ushort)
                                                                    + seq.Length + 1 + sizeof(ushort)
                                                                    + sizeof(ushort) + headerSpan.Length
                                                                    + index * (sizeof(ushort) + 1)),
                (ushort)part.Length);
            BinaryPrimitives.WriteUInt16BigEndian(packet.Span.Slice(sizeof(ushort)
                                                                    + seq.Length + 1 + sizeof(ushort)
                                                                    + sizeof(ushort) + headerSpan.Length
                                                                    + index * (sizeof(ushort) + 1)
                                                                    + sizeof(ushort)),
                (ushort)part.Length);
        }

        return new SpotifyPacket(cmd, packet);
    }
}