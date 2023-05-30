using System.Buffers.Binary;
using System.Net.Sockets;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.Connection.Crypto;

namespace Wavee.Spotify.Infrastructure.Tcp;

internal static class SpotifyTcp
{
    public static SpotifyConnectionRecord Send(
        NetworkStream stream,
        SpotifyConnectionRecord record,
        SpotifyPacketType package,
        ReadOnlySpan<byte> packet)
    {
        const int MacLength = 4;
        const int HeaderLength = 3;
        
        var shannon = new Shannon(record.SendKey.Span);
        Span<byte> encoded = stackalloc byte[HeaderLength + packet.Length + MacLength];
        encoded[0] = (byte)package;
        BinaryPrimitives.WriteUInt16BigEndian(encoded[1..],
            (ushort)packet.Length);

        packet.CopyTo(encoded[3..]);
        shannon.Nonce(record.SendSequence);

        shannon.Encrypt(encoded[..(3 + packet.Length)]);

        Span<byte> mac = stackalloc byte[MacLength];
        shannon.Finish(mac);

        mac.CopyTo(encoded[(3 + packet.Length)..]);

        record = record with { SendSequence = record.SendSequence + 1 };
        
        stream.Write(encoded);
        return record;
    }
    
    public static SpotifyRawPacket Receive(NetworkStream stream, ref SpotifyConnectionRecord connection)
    {
        var key = new Shannon(connection.ReceiveKey.Span);
        Span<byte> header = new byte[3];
        stream.ReadExactly(header);
        key.Nonce(connection.ReceiveSequence);
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

        connection = connection with { ReceiveSequence = connection.ReceiveSequence + 1 };
        return new SpotifyRawPacket
        {
            Header = header,
            Payload = payload
        };
    }
}