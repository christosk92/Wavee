using System.Buffers.Binary;
using Wavee.Spotify.Crypto;

namespace Wavee.Spotify.Models.Internal;

internal readonly record struct SpotifyPacket(PacketType Command, ReadOnlyMemory<byte> Payload)
{
    internal const int MAC_SIZE = 4;

    public ReadOnlyMemory<byte> Encrypt(uint sequence, ReadOnlySpan<byte> keySpan)
    {
        var key = new Shannon(keySpan);
        Memory<byte> encoded = new byte[3 + Payload.Length + MAC_SIZE];
        encoded.Span[0] = (byte)Command;
        BinaryPrimitives.WriteUInt16BigEndian(encoded.Span[1..],
            (ushort)Payload.Length);

        Payload.Span.CopyTo(encoded.Span[3..]);
        key.Nonce(sequence);

        key.Encrypt(encoded.Span[..(3 + Payload.Length)]);

        Span<byte> mac = new byte[MAC_SIZE];
        key.Finish(mac);

        //final packet = encoded + mac
        mac.CopyTo(encoded.Span[(3 + Payload.Length)..]);

        return encoded;
    }
}