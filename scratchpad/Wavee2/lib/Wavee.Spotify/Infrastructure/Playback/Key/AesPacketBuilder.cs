using System.Buffers.Binary;
using System.Numerics;
using Google.Protobuf;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.Tcp;

namespace Wavee.Spotify.Infrastructure.Playback.Key;

internal static class AesPacketBuilder
{
    public static SpotifySendPacket BuildRequest(AudioId id, ByteString fileId, uint sequence)
    {
        var result = id.Id;
        var length = (int)Math.Ceiling(BigInteger.Log(result, 256));
        Span<byte> raw = stackalloc byte[length];
        // var bytes = new List<byte>();
        int index = 0;
        while (result > 0)
        {
            //bytes.Insert(0, (byte)(result & 0xff));
            raw[index++] = (byte)(result & 0xff);
            result >>= 8;
        }
        raw.Reverse();
        
        var fileMemory = fileId.Span;

        Memory<byte> data = new byte[raw.Length + fileMemory.Length + 2 + sizeof(uint)];

        fileMemory.CopyTo(data.Span);
        raw.CopyTo(data.Span.Slice(fileMemory.Length));
        BinaryPrimitives.WriteUInt32BigEndian(data.Span.Slice(fileMemory.Length + raw.Length), sequence);
        BinaryPrimitives.WriteUInt16BigEndian(data.Span.Slice(fileMemory.Length + raw.Length + sizeof(uint)), 0x0000);

        return new SpotifySendPacket(SpotifyPacketType.RequestKey, data);
    }
}