using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure.BitReaders;

namespace Wavee.Core.Decoders.VorbisDecoder.Mapper;

internal class VorbisPacketParser : IPacketParser
{
    private Option<byte> _prevBsExp = Option<byte>.None;

    public VorbisPacketParser(byte numModes, ulong modesBlockFlags, byte bs0Exp, byte bs1Exp)
    {
        NumModes = numModes;
        ModesBlockFlags = modesBlockFlags;
        Bs0exp = bs0Exp;
        Bs1exp = bs1Exp;
    }

    public byte NumModes { get; }
    public ulong ModesBlockFlags { get; }
    public byte Bs1exp { get; }

    public byte Bs0exp { get; }

    public ulong ParseNextPacketDuration(byte[] packet)
    {
        var bs = new BitReaderRtlRef(packet.AsSpan());

        // First bit must be 0 to indicate audio packet.
        var bit = bs.ReadBool();
        if (bit)
            return 0;

        // Number of bits for the mode number.
        var modeNumBits = ((uint)(NumModes) - 1).ilog();

        // Read the mode number.
        var modeNum = bs.ReadBitsLeq32(modeNumBits)
            .Match(
                Succ: modeNum => modeNum,
                _ => 0
            );

        // Determine the current block size.
        byte curBsExp = 0;
        if (modeNum < NumModes)
        {
            var blockFlag = (this.ModesBlockFlags >> modeNum) & 1;
            if (blockFlag == 1)
            {
                curBsExp = this.Bs1exp;
            }
            else
            {
                curBsExp = this.Bs0exp;
            }
        }
        else
        {
            curBsExp = 0;
        }

        // Calculate the duration if the previous block size is available. Otherwise return 0.
        ulong dur;
        if (_prevBsExp.IsSome)
        {
            dur = ((ulong)1 << _prevBsExp.ValueUnsafe()) >> 2;
            dur += ((ulong)1 << curBsExp) >> 2;
        }
        else
        {
            dur = 0;
        }

        _prevBsExp = curBsExp;
        return dur;
    }

    public ulong ParseNextPacketDur(Span<byte> packet)
    {
        var bs = new BitReaderRtlRef(packet);
        
        // First bit must be 0 to indicate audio packet.
        var bit = bs.ReadBool();
        if (bit)
            return 0;
        
        // Number of bits for the mode number.
        var modeNumBits = ((uint)(NumModes) - 1).ilog();
        
        // Read the mode number.
        var modeNum = bs.ReadBitsLeq32(modeNumBits)
            .Match(
                Succ: modeNum => (byte)modeNum,
                _ => (byte)0
            );
        
        // Determine the current block size.
        byte curBsExp = 0;
        if(modeNum < this.NumModes)
        {
            var blockFlag = (this.ModesBlockFlags >> modeNum) & 1;
            if(blockFlag == 1)
            {
                curBsExp = this.Bs1exp;
            }
            else
            {
                curBsExp = this.Bs0exp;
            }
        }
        
        // Calculate the duration if the previous block size is available. Otherwise return 0.
        ulong dur = 0;
        if(_prevBsExp.IsSome)
        {
            dur = ((ulong)1 << _prevBsExp.ValueUnsafe()) >> 2;
            dur += ((ulong)1 << curBsExp) >> 2;
        }
        
        _prevBsExp = curBsExp;
        
        return dur;
    }

    public void Reset()
    {
        _prevBsExp = Option<byte>.None;
    }
}