using System.Buffers.Binary;
using System.Data;
using System.Net.Sockets;

namespace Wavee.Spotify.Auth.Tcp;

internal static class Connection
{
    public static void Send(TcpClient tcpClient, ReadOnlyMemory<byte> sendKey, int nonce, SpotifyPacketType packageType,
        Span<byte> packet)
    {
        var stream = tcpClient.GetStream();
        const int MacLength = 4;
        const int HeaderLength = 3;

        var shannon = new Shannon(sendKey.Span);
        Span<byte> encoded = stackalloc byte[HeaderLength + packet.Length + MacLength];
        encoded[0] = (byte)packageType;

        BinaryPrimitives.WriteUInt16BigEndian(encoded[1..], (ushort)packet.Length);


        packet.CopyTo(encoded[3..]);
        shannon.Nonce((uint)nonce);

        shannon.Encrypt(encoded[..(3 + packet.Length)]);

        Span<byte> mac = stackalloc byte[MacLength];
        shannon.Finish(mac);

        mac.CopyTo(encoded[(3 + packet.Length)..]);
        stream.Write(encoded);
    }

    public static SpotifyPacketType Receive(TcpClient tcpClient, ReadOnlyMemory<byte> receiveKey, int nonce,
        out ReadOnlySpan<byte> output)
    {
        var stream = tcpClient.GetStream();

        var key = new Shannon(receiveKey.Span);
        Span<byte> header = new byte[3];
        stream.ReadExactly(header);
        key.Nonce((uint)nonce);
        key.Decrypt(header);

        var payloadLength = (short)((header[1] << 8) | (header[2] & 0xFF));
        Span<byte> x = new byte[payloadLength];
        stream.ReadExactly(x);
        key.Decrypt(x);

        Span<byte> mac = stackalloc byte[4];
        stream.ReadExactly(mac);

        Span<byte> expectedMac = stackalloc byte[4];
        key.Finish(expectedMac);

        if (!mac.SequenceEqual(expectedMac))
        {
            throw new InvalidConstraintException();
            //  throw new Exception("MAC mismatch");
        }

        output = x;
        return (SpotifyPacketType)header[0];
    }
}