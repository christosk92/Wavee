using System.Collections;
using System.Numerics;

namespace Wavee.Core.Decoders.VorbisDecoder.Decoder;

internal sealed class BitSet256 : IEnumerable<uint>
{
    private const int Size = 256;
    private uint[] _data = new uint[Size / 32];

    public void Set(int index)
    {
        Set(index, true);
    }

    public void Remove(int index)
    {
        Set(index, false);
    }

    private void Set(int index, bool value)
    {
        if (index < 0 || index >= Size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        int dataIndex = index / 32;
        int bitIndex = index % 32;

        if (value)
        {
            _data[dataIndex] |= (1U << bitIndex);
        }
        else
        {
            _data[dataIndex] &= ~(1U << bitIndex);
        }
    }

    public bool Contains(int index)
    {
        if (index < 0 || index >= Size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        int dataIndex = index / 32;
        int bitIndex = index % 32;

        return (_data[dataIndex] & (1U << bitIndex)) != 0;
    }

    public int Count()
    {
        int count = 0;
        foreach (uint value in _data)
        {
            count += BitOperations.PopCount(value);
        }
        return count;
    }

    public IEnumerator<uint> GetEnumerator()
    {
        for (uint i = 0; i < _data.Length; i++)
        {
            uint current = _data[i];
            while (current != 0)
            {
                uint bitIndex = (uint)BitOperations.TrailingZeroCount(current);
                yield return i * 32 + bitIndex;
                current &= current - 1;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}