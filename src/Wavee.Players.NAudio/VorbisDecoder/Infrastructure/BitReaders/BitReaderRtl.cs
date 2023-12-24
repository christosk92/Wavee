using System.Buffers.Binary;
using LanguageExt;
using LanguageExt.Common;
using Wavee.Vorbis.Decoder.Setup.Codebooks;

namespace Wavee.Vorbis.Infrastructure.BitReaders;

internal ref struct BitReaderRtlRef
{
    private Span<byte> _buf;
    private ulong _bits;
    private uint _nBitsLeft;

    public BitReaderRtlRef(Span<byte> bytes)
    {
        _buf = bytes;
        _bits = 0;
        _nBitsLeft = 0;
    }

    public ulong BitsLeft => (8 * (ulong)_buf.Length) + (ulong)(_nBitsLeft);

    /// <summary>
    /// Reads and returns up to 32-bits or returns an error.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public Result<int> ReadBitsLeq32(uint bitWidth)
    {
        //Debug.Assert(bitWidth <= 32);
        if (bitWidth > 32)
        {
            return new Result<int>(new ArgumentOutOfRangeException(nameof(bitWidth)));
        }

        var bits = _bits;
        var bitsNeeded = bitWidth;

        while (bitsNeeded > _nBitsLeft)
        {
            bitsNeeded -= _nBitsLeft;

            var fetchresult = FetchBits();
            if (fetchresult.IsFaulted)
            {
                return new Result<int>(fetchresult.Error());
            }

            bits |= _bits << (int)(bitWidth - bitsNeeded);
        }

        ConsumeBits(bitsNeeded);

        // Since bitWidth is <= 32, this shift will never throw an exception.
        uint mask = bitWidth == 32 ? ~0U : ~(~0U << (int)bitWidth);
        return (int)(bits & mask);
    }

    private void ConsumeBits(uint num)
    {
        _nBitsLeft -= num;
        _bits >>= (int)num;
    }

    private Result<Unit> FetchBits()
    {
        Span<byte> buf = stackalloc byte[sizeof(ulong)];

        var readLen = Math.Min(_buf.Length, sizeof(ulong));

        if (readLen == 0)
        {
            return new Result<Unit>(new EndOfStreamException());
        }

        _buf[..readLen].CopyTo(buf);

        _buf = _buf[readLen..];

        _bits = BinaryPrimitives.ReadUInt64LittleEndian(buf);
        _nBitsLeft = (uint)readLen << 3;
        return new Result<Unit>(Unit.Default);
    }

    public bool ReadBool()
    {
        if (_nBitsLeft < 1)
        {
            FetchBits();
        }

        var bit = (_bits & 1) == 1;
        ConsumeBits(1);
        return bit;
    }

    public void IgnoreBits(uint numBits)
    {
        if (numBits <= _nBitsLeft)
        {
            ConsumeBits(numBits);
        }
        else
        {
            // Consume whole bit caches directly.
            while (numBits > _nBitsLeft)
            {
                numBits -= _nBitsLeft;
                FetchBits();
            }

            if (numBits > 0)
            {
                // Shift out in two parts to prevent panicing when num_bits == 64.
                ConsumeBits(numBits - 1);
                ConsumeBits(1);
            }
        }
    }


    private void FetchBitsPartial()
    {
        Span<byte> buf = stackalloc byte[sizeof(ulong)];

        var readLen = Math.Min(_buf.Length, (int)(64 - _nBitsLeft) >> 3);
        _buf[..readLen].CopyTo(buf[..readLen]);

        _buf = _buf[readLen..];

        _bits |= BinaryPrimitives.ReadUInt64LittleEndian(buf) << (int)_nBitsLeft;
        _nBitsLeft += (uint)readLen << 3;
    }

    public bool TryReadBool(out bool value)
    {
        try
        {
            value = this.ReadBool();
            return true;
        }
        catch (IOException e)
        {
            // If the error is an end-of-stream error, return false without an error
            if (e.InnerException is EndOfStreamException)
            {
                value = false;
                return false;
            }
            else
            {
                throw;
            }
        }
    }

    public void TryReadBits(uint len, out Result<int> output)
    {
        try
        {
            output = ReadBitsLeq32(len);
            return;
        }
        catch (IOException e)
        {
            // If the error is an end-of-stream error, return false without an error
            if (e.InnerException is EndOfStreamException)
            {
                output = 0;
            }
            else
            {
                throw;
            }
        }
    }

    public Result<(EValueType, uint)> ReadCodebook<E, EValueType>(Codebook<E, EValueType> codebook) where E : ICodebookEntry<EValueType>
    {
       /*
        *  if self.num_bits_left() < codebook.max_code_len {
            self.fetch_bits_partial()?;
        }
        */
       if (_nBitsLeft < codebook.MaxCodeLen)
       {
            FetchBitsPartial();;
       }
       
       // The number of bits actually buffered in the bit buffer.
       var numBitsLeft = _nBitsLeft;
       
       var bits = _bits;
       
       var blockLen = codebook.InitBlockLen;
       //        let mut entry = codebook.table[(bits & ((1 << block_len) - 1)) as usize + 1];
       int result = (int)((bits & ((1UL << (int)blockLen) - 1)) + 1);
       var entry = codebook.Table[result];
       uint consumed = 0;

       while (entry.IsJump)
       {
           // Consume the bits used for the initial or previous jump iteration.
           consumed += blockLen;
           bits >>= (int)blockLen;

           // Since this is a jump entry, if there are no bits left then the bitstream ended early.
           if (consumed > numBitsLeft)
           {
               return new Result<(EValueType, uint)>(new EndOfStreamException());
           }

           //prepare for next jump
           blockLen = entry.JumpLength;
           // ulong index = bits >> (64 - (int)blockLen);
           //
           // let index = bits & ((1 << block_len) - 1);
           ulong index = bits & (((ulong)1 << (int)blockLen) - 1);

           // Jump to the next entry.
           var jmp = entry.JumpOffset;
           entry = codebook.Table[jmp + (int)index];
       }

       // The entry is always a value entry at this point. Consume the bits containing the value.
       consumed += entry.ValueLen;

       if (consumed > numBitsLeft)
       {
           return new Result<(EValueType, uint)>(new EndOfStreamException());
       }

       ConsumeBits(consumed);
       return (entry.Value, consumed);
    }
}