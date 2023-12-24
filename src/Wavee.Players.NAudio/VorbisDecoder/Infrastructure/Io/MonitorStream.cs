using LanguageExt;
using LanguageExt.Common;
using Wavee.Vorbis.Infrastructure.Stream;

namespace Wavee.Vorbis.Infrastructure.Io;

internal sealed class MonitorStream<TB, TM> : IReadBytes where TB : IReadBytes
    where TM : IMonitor
{
    private readonly TB _inner;
    private readonly TM _monitor;

    public MonitorStream(TB inner, TM monitor)
    {
        _inner = inner;
        _monitor = monitor;
    }

    public Result<Unit> ReadBufferExactly(Span<byte> buffer)
    {
        var a = _inner.ReadBufferExactly(buffer);
        if (a.IsFaulted)
            return a;
        _monitor.ProcessBufferBytes(buffer);
        return Unit.Default;
    }

    public Span<byte> ReadQuadBytes()
    {
        var b = _inner.ReadQuadBytes();
        _monitor.ProcessQuadBytes(b);
        return b;
    }

    public byte ReadByte()
    {
        var b = _inner.ReadByte();
        _monitor.ProcessByte(b);
        return b;
    }

    public ulong Position()
    {
        throw new NotImplementedException();
    }

    public TB IntoInner() => _inner;
    public TM Monitor() => _monitor;
}