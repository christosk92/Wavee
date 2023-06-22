using System.Buffers.Binary;
using System.Numerics;
using Google.Protobuf;
using Wavee.Id;
using Wavee.Spotify.Infrastructure.Connection;

namespace Wavee.Infrastructure.AudioKey;

internal static class AesPacketBuilder
{
    public static SpotifyUnencryptedPackage BuildRequest(SpotifyId id, ByteString fileId, uint sequence)
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

        Span<byte> data = new byte[raw.Length + fileMemory.Length + 2 + sizeof(uint)];

        fileMemory.CopyTo(data);
        raw.CopyTo(data.Slice(fileMemory.Length));
        BinaryPrimitives.WriteUInt32BigEndian(data.Slice(fileMemory.Length + raw.Length), sequence);
        BinaryPrimitives.WriteUInt16BigEndian(data.Slice(fileMemory.Length + raw.Length + sizeof(uint)), 0x0000);

        return new SpotifyUnencryptedPackage(SpotifyPacketType.RequestKey, data);
    }
}