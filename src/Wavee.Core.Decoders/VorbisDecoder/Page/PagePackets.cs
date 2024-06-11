using System.Collections;
using LanguageExt;

namespace Wavee.Core.Decoders.VorbisDecoder.Page;

internal sealed class PagePackets : IDisposable
{
    private List<ushort> lensList;  // Changed to List for easier handling
    private IEnumerator<ushort> lens;
    private byte[] data;
    private Option<byte[]> _current;

    public PagePackets(List<ushort> lens, byte[] data)
    {
        lensList = lens;
        this.lens = lens.GetEnumerator();
        this.data = data;
        _current = Next();
    }
    public bool CanMoveNext => lens.MoveNext();
    public Option<byte[]> Current => _current;

    /// <summary>
    /// If this page ends with an incomplete (partial) packet, get a slice to the data associated
    /// with the partial packet.
    /// </summary>
    public Option<byte[]> PartialPacket()
    {
        var discard = lensList.Sum(len => (int)len);

        if (data.Length > discard)
        {
            return data.Skip(discard).ToArray();
        }
        else
        {
            return Option<byte[]>.None;
        }
    }
    
    public Option<byte[]> Next()
    {
        if (lens.MoveNext())
        {
            var len = lens.Current;
            var packet = data.Take((int)len).ToArray();
            data = data.Skip((int)len).ToArray();
            _current = Option<byte[]>.Some(packet);
            return packet;
        }
        _current = Option<byte[]>.None;
        return Option<byte[]>.None;
    }

    public void Dispose()
    {
        lens.Dispose();
    }

    public IEnumerator GetEnumerator()
    {
        throw new NotImplementedException();
    }
}