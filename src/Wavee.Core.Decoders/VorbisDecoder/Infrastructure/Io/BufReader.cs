using LanguageExt;
using LanguageExt.Common;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure.Stream;

namespace Wavee.Core.Decoders.VorbisDecoder.Infrastructure.Io;

/// <summary>
/// A <see cref="BufReader"/> reads bytes from a byte buffer.
/// </summary>
internal sealed class BufReader : IReadBytes
{
    private readonly byte[] _buffer;
    private int _position;

    public BufReader(byte[] buffer)
    {
        _buffer = buffer;
        _position = 0;
    }

    public Result<Unit> ReadBufferExactly(Span<byte> buffer)
    {
        var len = buffer.Length;

        if (_buffer.Length - _position < len)
            return new Result<Unit>(new InternalBufferOverflowException("Not enough bytes in buffer"));

        _buffer.AsSpan(_position, len).CopyTo(buffer);
        _position += len;
        return new Result<Unit>(Unit.Default);
    }

    public Span<byte> ReadQuadBytes()
    {
        if ((this._buffer.Length - _position) < 4)
            throw new UnderrunError();

        Span<byte> bytes = new byte[4];
        _buffer.AsSpan()[_position..(_position + 4)].CopyTo(bytes);
        _position += 4;
        return bytes;
    }

    public byte ReadByte()
    {
        if ((this._buffer.Length - _position) < 1)
            throw new UnderrunError();

        _position += 1;
        return _buffer[_position - 1];
    }

    public ulong Position() => (ulong)_position;

    public Result<Unit> IgnoreBytes(ulong count)
    {
        if (_buffer.Length - _position < (int)count)
            return new Result<Unit>(new InternalBufferOverflowException("Not enough bytes in buffer"));

        _position += (int)count;

        return new Result<Unit>(Unit.Default);
    }

    public Span<byte> ReadBufBytesAvailableRef()
    {
        var pos = _position;
        _position = _buffer.Length();
        return _buffer.AsSpan()[pos..];
    }
}