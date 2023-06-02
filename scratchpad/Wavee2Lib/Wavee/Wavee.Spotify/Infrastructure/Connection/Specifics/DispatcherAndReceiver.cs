using System.Buffers.Binary;
using System.Net.Sockets;
using LanguageExt;
using Wavee.Spotify.Crypto;

namespace Wavee.Spotify.Infrastructure.Connection.Specifics;

internal static class DispatcherAndReceiverIO
{
    public static Unit Send(this NetworkStream stream,
        SpotifyUnencryptedPackage package,
        ReadOnlySpan<byte> sendKey, int sequence)
    {    
        const int MacLength = 4;
        const int HeaderLength = 3;

        var shannon = new Shannon(sendKey);
        Span<byte> encoded = stackalloc byte[HeaderLength + package.Payload.Length + MacLength];
        encoded[0] = (byte)package.Type;
        BinaryPrimitives.WriteUInt16BigEndian(encoded[1..],
            (ushort)package.Payload.Length);

        package.Payload.CopyTo(encoded[3..]);
        shannon.Nonce((uint)sequence);

        shannon.Encrypt(encoded[..(3 + package.Payload.Length)]);

        Span<byte> mac = stackalloc byte[MacLength];
        shannon.Finish(mac);

        mac.CopyTo(encoded[(3 + package.Payload.Length)..]);
        stream.Write(encoded);
        return default;
    }

    public static Unit Receive(this NetworkStream stream, ReadOnlySpan<byte> receiveKey, int sequence,
        out SpotifyUnencryptedPackage package)
    {
        var key = new Shannon(receiveKey);
        Span<byte> header = new byte[3];
        stream.ReadExactly(header);
        key.Nonce((uint)sequence);
        key.Decrypt(header);

        var payloadLength = (short)((header[1] << 8) | (header[2] & 0xFF));
        Span<byte> payload = new byte[payloadLength];
        stream.ReadExactly(payload);
        key.Decrypt(payload);

        Span<byte> mac = stackalloc byte[4];
        stream.ReadExactly(mac);

        Span<byte> expectedMac = stackalloc byte[4];
        key.Finish(expectedMac);

        if (!mac.SequenceEqual(expectedMac))
        {
            throw new Exception("MAC mismatch");
        }

        package = new SpotifyUnencryptedPackage((SpotifyPacketType)header[0], payload);

        return Unit.Default;
    }
}