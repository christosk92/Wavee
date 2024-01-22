
using LanguageExt;
using LanguageExt.Common;
using Wavee.Vorbis.Infrastructure.Stream;

namespace Wavee.Vorbis.Infrastructure.Io;

/// <summary>
/// A <see cref="ScopedStream"/> restricts the number of bytes that may be read to an upper limit.
/// </summary>
public sealed class ScopedStream<T> : IReadBytes, ISeekBuffered where T : IReadBytes
{
    private T _inner;
    private ulong _start;
    private ulong _len;
    private ulong _read;

    public ScopedStream(T inner, ulong len)
    {
        _inner = inner;
        _start = inner.Position();
        _len = len;
        _read = 0;
    }

    public T Inner => _inner;

    public Span<byte> ReadQuadBytes()
    {
        if (_len - _read < 4)
            throw new ArgumentOutOfRangeException();

        _read += 4;
        return _inner.ReadQuadBytes();
    }

    public ReadOnlySpan<byte> ReadDoubleBytes()
    {
        throw new NotImplementedException();
    }

    public ReadOnlySpan<byte> ReadTripleBytes()
    {
        throw new NotImplementedException();
    }

    public byte ReadByte()
    {
        if (_len - _read < 1)
            throw new ArgumentOutOfRangeException();
        
        _read += 1;
        return _inner.ReadByte();
    }

    public Result<Unit> ReadBufferExactly(Span<byte> buf)
    {
        if (_len - _read < (ulong)buf.Length)
            throw new ArgumentOutOfRangeException();

        _read += (ulong)buf.Length;
        return _inner.ReadBufferExactly(buf);
    }

    public ulong Position() => _inner.Position();

    public void IgnoreBytes(ulong count)
    {
        throw new NotImplementedException();
    }

    public ulong SeekBuffered(ulong pos)
    {
        throw new NotImplementedException();
    }

    public void EnsureSeekBuffered(int len)
    {
        throw new NotImplementedException();
    }
}