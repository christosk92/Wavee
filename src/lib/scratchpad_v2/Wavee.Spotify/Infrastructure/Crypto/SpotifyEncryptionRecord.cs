using System.Buffers.Binary;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Sys.Crypto;

namespace Wavee.Spotify.Infrastructure.Crypto;

internal readonly record struct SpotifyEncryptionRecord(
    ReadOnlyMemory<byte> EncryptionKey, uint EncryptionNonce,
    ReadOnlyMemory<byte> DecryptionKey, uint DecryptionNonce)
{
    public SpotifyEncryptionRecord Encrypt(Memory<byte> data)
    {
        var shannon = new Shannon(EncryptionKey.Span);
        shannon.Nonce(EncryptionNonce);
        shannon.Encrypt(data.Span);
        return this with { EncryptionNonce = IncrementNonce(EncryptionNonce) };
    }

    public SpotifyEncryptionRecord Decrypt(Memory<byte> data, int length)
    {
        var shannon = new Shannon(DecryptionKey.Span);
        shannon.Nonce(DecryptionNonce);
        shannon.Decrypt(data.Span.Slice(0, length));
        return this with { DecryptionNonce = IncrementNonce(DecryptionNonce) };
    }

    internal (ReadOnlyMemory<byte> EnrcyptedMessage, SpotifyEncryptionRecord NewRecord) EncryptPacket(
        SpotifyPacket packet)
    {
        var key = new Shannon(EncryptionKey.Span);
        Memory<byte> encoded = new byte[3 + packet.Data.Length + MAC_SIZE];
        encoded.Span[0] = (byte)packet.Command;
        BinaryPrimitives.WriteUInt16BigEndian(encoded.Span[1..],
            (ushort)packet.Data.Length);

        packet.Data.Span.CopyTo(encoded.Span[3..]);
        key.Nonce(EncryptionNonce);

        key.Encrypt(encoded.Span[..(3 + packet.Data.Length)]);

        Span<byte> mac = new byte[MAC_SIZE];
        key.Finish(mac);

        //final packet = encoded + mac
        mac.CopyTo(encoded.Span[(3 + packet.Data.Length)..]);

        return (encoded, this with { EncryptionNonce = IncrementNonce(EncryptionNonce) });
    }

    internal const int MAC_SIZE = 4;
    internal static uint IncrementNonce(uint encryptionNonce) => encryptionNonce + 1;
}