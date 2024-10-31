using System.Buffers.Binary;
using Wavee.Exceptions;

namespace Wavee.Services.Session;

internal sealed class ApCodec
{
    private const int HeaderSize = 3;
    private const int MacSize = 4;

    private uint _encodeNonce;
    private readonly Shannon _sendCipher;

    private uint _decodeNonce;
    private readonly Shannon _recvCipher;

    public ApCodec(byte[] sendKey, byte[] recvKey)
    {
        _encodeNonce = 0;
        _sendCipher = new Shannon(sendKey);

        _decodeNonce = 0;
        _recvCipher = new Shannon(recvKey);
    }

    public void Encode((byte command, byte[] payload) item, Stream stream)
    {
        Span<byte> encoded = stackalloc byte[HeaderSize + item.payload.Length + MacSize];
        encoded[0] = (byte)item.command;

        BinaryPrimitives.WriteUInt16BigEndian(encoded[1..], (ushort)item.payload.Length);

        item.payload.CopyTo(encoded[3..]);
        _sendCipher.Nonce((uint)_encodeNonce);
        _encodeNonce++;

        _sendCipher.Encrypt(encoded[..(3 + item.payload.Length)]);

        Span<byte> mac = stackalloc byte[MacSize];
        _sendCipher.Finish(mac);

        mac.CopyTo(encoded[(3 + item.payload.Length)..]);
        stream.Write(encoded);
    }

    public (byte command, byte[] payload) Decode(Stream stream)
    {
        Span<byte> header = stackalloc byte[HeaderSize];
        
         stream.ReadExactly(header);
        _recvCipher.Nonce(_decodeNonce);
        _decodeNonce++;

        _recvCipher.Decrypt(header);
        byte command = header[0];
        int payloadLength = (header[1] << 8) | header[2];

        byte[] payload = new byte[payloadLength];
        stream.ReadExactly(payload);
        _recvCipher.Decrypt(payload);

        Span<byte> mac = stackalloc byte[MacSize];
        stream.ReadExactly(mac);

        Span<byte> expectedMac = stackalloc byte[MacSize];
        _recvCipher.Finish(expectedMac);

        if (!mac.SequenceEqual(expectedMac))
        {
            throw new InvalidOperationException("MAC mismatch");
        }

        return (command, payload);
    }

    public async Task SendAsync(Stream stream, byte command, byte[] item, CancellationToken cancellationToken)
    {
        Memory<byte> encoded = new byte[HeaderSize + item.Length + MacSize];
        encoded.Span[0] = (byte)command;

        BinaryPrimitives.WriteUInt16BigEndian(encoded.Span[1..], (ushort)item.Length);

        item.CopyTo(encoded[3..]);
        _sendCipher.Nonce((uint)_encodeNonce);
        _encodeNonce++;

        _sendCipher.Encrypt(encoded[..(3 + item.Length)].Span);

        Memory<byte> mac = new byte[MacSize];
        _sendCipher.Finish(mac.Span);

        mac.CopyTo(encoded[(3 + item.Length)..]);
        await stream.WriteAsync(encoded, cancellationToken);
    }

    public async Task<(byte, byte[])?> ReceiveAsync(Stream stream, CancellationToken cancellationToken)
    {
        Memory<byte> header = new byte[HeaderSize];
        
        await stream.ReadExactlyAsync(header, cancellationToken);
        _recvCipher.Nonce(_decodeNonce);
        _decodeNonce++;

        _recvCipher.Decrypt(header.Span);
        byte command = header.Span[0];
        int payloadLength = (header.Span[1] << 8) | header.Span[2];

        byte[] payload = new byte[payloadLength];
        stream.ReadExactly(payload);
        _recvCipher.Decrypt(payload);

        Memory<byte> mac = new byte[MacSize];
        stream.ReadExactly(mac.Span);

        Memory<byte> expectedMac = new byte[MacSize];
        _recvCipher.Finish(expectedMac.Span);

        if (!mac.Span.SequenceEqual(expectedMac.Span))
        {
            throw new WaveeUnknownException("MAC mismatch", null);
        }

        return (command, payload);    }
}