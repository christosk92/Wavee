using System.Buffers.Binary;
using Google.Protobuf;
using Wavee.Spotify.Clients.Info;
using Wavee.Spotify.Infrastructure.Connection;

namespace Wavee.Spotify.Clients.Mercury.Key;

internal static class AesPacketBuilder
{
    public static SpotifyPacket BuildRequest(SpotifyId id, ByteString fileId, uint sequence)
    {
        var raw = id.ToRaw();
        var fileMemory = fileId.Span;

        Memory<byte> data = new byte[raw.Length + fileMemory.Length + 2 + sizeof(uint)];

        fileMemory.CopyTo(data.Span);
        raw.CopyTo(data.Span.Slice(fileMemory.Length));
        BinaryPrimitives.WriteUInt32BigEndian(data.Span.Slice(fileMemory.Length + raw.Length), sequence);
        BinaryPrimitives.WriteUInt16BigEndian(data.Span.Slice(fileMemory.Length + raw.Length + sizeof(uint)), 0x0000);
        
        return new SpotifyPacket(SpotifyPacketType.RequestKey, data);
    }
}