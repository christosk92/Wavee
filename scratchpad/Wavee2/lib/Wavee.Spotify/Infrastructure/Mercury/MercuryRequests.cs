using System.Buffers.Binary;
using System.Net.Sockets;
using Eum.Spotify;
using Google.Protobuf;
using LanguageExt;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.Connection.Crypto;
using Wavee.Spotify.Infrastructure.Tcp;

namespace Wavee.Spotify.Infrastructure.Mercury;

internal static class MercuryRequests
{
    // public static SpotifyConnectionRecord Send(
    //     NetworkStream stream,
    //     SpotifyConnectionRecord record,
    //     ulong sequenceNumber,
    //     MercuryMethod method,
    //     string uri,
    //     string? contentType,
    //     Seq<ReadOnlyMemory<byte>> payload)
    // {
    //     var packet = Build(sequenceNumber, method, uri, contentType, payload);
    //     record = SpotifyTcp.Send(stream, record, packet.Command, packet.Data);
    //     return record;
    // }

    public static SpotifySendPacket Build(
        ulong sequenceNumber,
        MercuryMethod method,
        string uri,
        string? contentType,
        Seq<ReadOnlyMemory<byte>> payload)
    {
        Span<byte> seq = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64BigEndian(seq, sequenceNumber);

        var cmd = method switch
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
            Method = method.ToString().ToUpper()
        };

        if (contentType != null) header.ContentType = contentType;

        Span<byte> headerSpan = header.ToByteArray();

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
        headerSpan.CopyTo(packet.Span.Slice(sizeof(ushort)
                                            + seq.Length + 1 + sizeof(ushort) + sizeof(ushort)));

        for (var index = 0; index < payloadCount; index++)
        {
            //if we are in this loop, we can assume that the payload is not empty
            var part = payload[index].Span;
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

        return new SpotifySendPacket(cmd, packet);
    }
}