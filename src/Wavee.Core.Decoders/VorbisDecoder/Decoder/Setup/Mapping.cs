using LanguageExt.Common;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure.BitReaders;

namespace Wavee.Core.Decoders.VorbisDecoder.Decoder.Setup;

internal sealed class Mapping
{
    private readonly ChannelCouple[] _couplings;

    public Mapping(ChannelCouple[] couplings, byte[] multiplex, Submap[] submaps)
    {
        _couplings = couplings;
        Multiplex = multiplex;
        Submaps = submaps;
    }
    
    public byte[] Multiplex { get; }
    public Submap[] Submaps { get; }
    public ChannelCouple[] Couplings => _couplings;


    public static Result<Mapping> Read(ref BitReaderRtlRef bs, byte audioChannels, byte maxFloor, byte maxResidue)
    {
        var countSubmaps = bs.ReadBool();
        byte numSubmaps = 1;
        if (countSubmaps)
        {
            var numSubmapsRes = bs.ReadBitsLeq32(4).Map(x => (byte)(x + 1));
            if (numSubmapsRes.IsFaulted)
                return new Result<Mapping>(numSubmapsRes.Error());
            numSubmaps = numSubmapsRes.Success();
        }
        
        var couplings = new List<ChannelCouple>();
        if (bs.ReadBool())
        {
            // Number of channel couplings (up-to 256).
            var couplingStepsRes = bs.ReadBitsLeq32(8).Map(x => (ushort)(x+1));
            if (couplingStepsRes.IsFaulted)
                return new Result<Mapping>(couplingStepsRes.Error());
            var couplingSteps = couplingStepsRes.Success();
            // Reserve space.
            couplings.Capacity = couplingSteps;
            
            // The maximum channel number.
            var maxChannel = audioChannels - 1;
            
            // The number of bits to read for the magnitude and angle channel numbers. Never exceeds 8.
            var couplingBits = ((uint)maxChannel).ilog();
            if (couplingBits > 8)
                return new Result<Mapping>(new DecodeError("vorbis: invalid coupling bits"));
            
            // Read each channel coupling.
            for (int i = 0; i < couplingSteps; i++)
            {
                var magnituteChRes = bs.ReadBitsLeq32(couplingBits).Map(x => (byte)x);
                if (magnituteChRes.IsFaulted)
                    return new Result<Mapping>(magnituteChRes.Error());
                var magnituteCh = magnituteChRes.Success();
                var angleChRes = bs.ReadBitsLeq32(couplingBits).Map(x => (byte)x);
                if (angleChRes.IsFaulted)
                    return new Result<Mapping>(angleChRes.Error());
                var angleCh = angleChRes.Success();
                
                // Ensure the channels to be coupled are not the same, and that neither channel number
                // exceeds the maximum channel in the stream.
                if (magnituteCh == angleCh || magnituteCh > maxChannel || angleCh > maxChannel)
                    return new Result<Mapping>(new DecodeError("vorbis: invalid coupling channel"));
                
                couplings.Add(new ChannelCouple(magnituteCh, angleCh));
            }
        }
        
        var reservedMappingBitsRes = bs.ReadBitsLeq32(2);
        if (reservedMappingBitsRes.IsFaulted)
            return new Result<Mapping>(reservedMappingBitsRes.Error());
        var reservedMappingBits = reservedMappingBitsRes.Success();
        if (reservedMappingBits != 0)
            return new Result<Mapping>(new DecodeError("vorbis: invalid mapping reserved bits"));
        
        var multiplex = new List<byte>(audioChannels);
        
        // If the number of submaps is > 1 read the multiplex numbers from the bitstream, otherwise
        // they're all 0.

        if (numSubmaps > 1)
        {
            for (int i = 0; i < audioChannels; i++)
            {
                var muxRes = bs.ReadBitsLeq32(4).Map(x => (byte)x);
                if (muxRes.IsFaulted)
                    return new Result<Mapping>(muxRes.Error());
                var mux = muxRes.Success();
                
                if (mux >= numSubmaps)
                    return new Result<Mapping>(new DecodeError("vorbis: invalid mapping mux"));
                
                multiplex.Add(mux);
            }
        }
        else
        {
            var oldLen = multiplex.Count;
            var extra = audioChannels - oldLen;
            multiplex.AddRange(Enumerable.Repeat((byte)0, extra));
        }
        
        var submaps = new List<Submap>(numSubmaps);
        
        for (int i = 0; i < numSubmaps; i++)
        {
            // Unused.
            var _r = bs.ReadBitsLeq32(8);
            if (_r.IsFaulted)
                return new Result<Mapping>(_r.Error());
            
            // The floor to use.
            var floorRes = bs.ReadBitsLeq32(8).Map(x => (byte)x);
            if (floorRes.IsFaulted)
                return new Result<Mapping>(floorRes.Error());
            var floor = floorRes.Success();
            
            if (floor >= maxFloor)
                return new Result<Mapping>(new DecodeError("vorbis: invalid mapping floor"));
            
            // The residue to use.
            var residueRes = bs.ReadBitsLeq32(8).Map(x => (byte)x);
            if (residueRes.IsFaulted)
                return new Result<Mapping>(residueRes.Error());
            var residue = residueRes.Success();
            
            if (residue >= maxResidue)
                return new Result<Mapping>(new DecodeError("vorbis: invalid mapping residue"));
            
            submaps.Add(new Submap(floor, residue));
        }
        
        var mapping = new Mapping(
            couplings: couplings.ToArray(),
            multiplex: multiplex.ToArray(),
            submaps: submaps.ToArray()
        );
        
        return new Result<Mapping>(mapping);
    }
}

internal readonly record struct Submap(byte Floor, byte Residue);

internal readonly record struct ChannelCouple(byte MagnituteCh, byte AngleCh);