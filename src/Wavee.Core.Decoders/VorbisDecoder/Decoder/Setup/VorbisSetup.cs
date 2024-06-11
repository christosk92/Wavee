using LanguageExt;
using LanguageExt.Common;
using Wavee.Core.Decoders.VorbisDecoder.Decoder.Setup.Codebooks;
using Wavee.Core.Decoders.VorbisDecoder.Decoder.Setup.Floor;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure.BitReaders;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure.Io;
using Wavee.Core.Decoders.VorbisDecoder.Mapper;

namespace Wavee.Core.Decoders.VorbisDecoder.Decoder.Setup;

internal static class VorbisSetup
{
    public static Result<SetupResult> ReadSetupHeader(BufReader reader, IdentHeader ident)
    {
        // The packet type must be an setup header.
        var packetType = reader.ReadByte();

        if (packetType != VorbisMapper.VORBIS_PACKET_TYPE_SETUP)
            return new Result<SetupResult>(new DecodeError("vorbis: invalid packet type for setup header"));

        // Next, the setup packet signature must be correct.
        Span<byte> setupPacketSignature = stackalloc byte[6];
        var readResult = reader.ReadBufferExactly(setupPacketSignature);
        if (readResult.IsFaulted)
            return new Result<SetupResult>(readResult.Error());

        if (!setupPacketSignature.SequenceEqual(VorbisMapper.VORBIS_HEADER_PACKET_SIGNATURE))
            return new Result<SetupResult>(new DecodeError("vorbis: invalid setup packet signature"));

        // The remaining portion of the setup header packet is read bitwise.
        var bs = new BitReaderRtlRef(reader.ReadBufBytesAvailableRef());

        // Read codebooks.
        var codebooksResult = ReadCodebooks(ref bs);
        if (codebooksResult.IsFaulted)
            return new Result<SetupResult>(codebooksResult.Error());
        var codebooks = codebooksResult.Success();

        // Read time-domain transforms (placeholders in Vorbis 1).
        var result = ReadTimeDomainTransforms(ref bs);
        if (result.IsFaulted)
            return new Result<SetupResult>(result.Error());

        // Read floors.
        var floorsResult = ReadFloors(ref bs, ident.Bs0Exp, ident.Bs1Exp, (byte)codebooks.Length);
        if (floorsResult.IsFaulted)
            return new Result<SetupResult>(floorsResult.Error());
        var floors = floorsResult.Success();


        // Read residues.
        var residuesResult = ReadResidues(ref bs, (byte)codebooks.Length);
        if (residuesResult.IsFaulted)
            return new Result<SetupResult>(residuesResult.Error());
        var residues = residuesResult.Success();

        // Read channel mappings.
        var mappingsResult = ReadMappings(ref bs, ident.NChannels, (byte)floors.Length, (byte)residues.Length);
        if (mappingsResult.IsFaulted)
            return new Result<SetupResult>(mappingsResult.Error());
        var mappings = mappingsResult.Success();

        // Read modes.
        var modesResult = VorbisMapper.ReadModes(ref bs);
        if (modesResult.IsFaulted)
            return new Result<SetupResult>(modesResult.Error());
        var modes = modesResult.Success();

        // Framing flag must be set.
        if (!bs.ReadBool())
            return new Result<SetupResult>(new DecodeError("vorbis: framing flag not set"));

        if (bs.BitsLeft > 0)
        {
          //  Log.Warning("vorbis: {bitsLeft} bits left in setup header", bs.BitsLeft);
        }

        return new Result<SetupResult>(new SetupResult(codebooks, floors, residues, modes, mappings));
    }

    private static Result<VorbisCodebook[]> ReadCodebooks(ref BitReaderRtlRef bs)
    {
        var countR = bs.ReadBitsLeq32(8).Map(x => x + 1);
        if (countR.IsFaulted)
            return new Result<VorbisCodebook[]>(countR.Error());

        var count = countR.Success();
        var codebooks = new VorbisCodebook[count];
        for (var i = 0; i < count; i++)
        {
            var codebookResult = VorbisCodebook.ReadCodebook(ref bs);
            if (codebookResult.IsFaulted)
                return new Result<VorbisCodebook[]>(codebookResult.Error());
            codebooks[i] = codebookResult.Success();
        }

        return new Result<VorbisCodebook[]>(codebooks);
    }

    private static Result<Unit> ReadTimeDomainTransforms(ref BitReaderRtlRef bs)
    {
        var countR = bs.ReadBitsLeq32(6).Map(x => x + 1);
        if (countR.IsFaulted)
            return new Result<Unit>(countR.Error());

        var count = countR.Success();
        for (var i = 0; i < count; i++)
        {
            var transformType = bs.ReadBitsLeq32(16);
            if (transformType.IsFaulted)
                return new Result<Unit>(transformType.Error());

            // All these values are placeholders and must be 0.
            if (transformType.Success() != 0)
                return new Result<Unit>(new DecodeError("vorbis: invalid time-domain transform type"));
        }

        return new Result<Unit>(Unit.Default);
    }

    private static Result<IFloor[]> ReadFloors(ref BitReaderRtlRef bs, byte identBs0Exp, byte identBs1Exp,
        byte maxCodebook)
    {
        var countR = bs.ReadBitsLeq32(6).Map(x => x + 1);
        if (countR.IsFaulted)
            return new Result<IFloor[]>(countR.Error());

        var output = new IFloor[countR.Success()];
        for (var i = 0; i < output.Length; i++)
        {
            var floorType = bs.ReadBitsLeq32(16);
            if (floorType.IsFaulted)
                return new Result<IFloor[]>(floorType.Error());

            switch (floorType.Success())
            {
                case 0:
                    var floor0Result = Floor0.Read(ref bs, identBs0Exp, identBs1Exp, maxCodebook);
                    if (floor0Result.IsFaulted)
                        return new Result<IFloor[]>(floor0Result.Error());
                    output[i] = floor0Result.Success();
                    break;
                case 1:
                    var floor1Result = Floor1.Read(ref bs, maxCodebook);
                    if (floor1Result.IsFaulted)
                        return new Result<IFloor[]>(floor1Result.Error());
                    output[i] = floor1Result.Success();
                    break;
                default:
                    return new Result<IFloor[]>(new DecodeError("vorbis: invalid floor type"));
            }
        }

        return new Result<IFloor[]>(output);
    }

    private static Result<Residue[]> ReadResidues(ref BitReaderRtlRef bs, byte codebooksLength)
    {
        var countR = bs.ReadBitsLeq32(6).Map(x => x + 1);
        if (countR.IsFaulted)
            return new Result<Residue[]>(countR.Error());

        var output = new Residue[countR.Success()];

        for (var i = 0; i < output.Length; i++)
        {
            var residueType = bs.ReadBitsLeq32(16).Map(x=> (ushort)x);
            if (residueType.IsFaulted)
                return new Result<Residue[]>(residueType.Error());

            var type = residueType.Success();
            if (type > 2)
                return new Result<Residue[]>(new DecodeError("vorbis: invalid residue type"));

            var residueResult = Residue.Read(ref bs, (ushort)type, codebooksLength);
            if (residueResult.IsFaulted)
                return new Result<Residue[]>(residueResult.Error());
            output[i] = residueResult.Success();
        }

        return new Result<Residue[]>(output);
    }

    private static Result<Mapping[]> ReadMappings(ref BitReaderRtlRef bs, byte identNChannels, byte floorsLength,
        byte residuesLength)
    {
        var countR = bs.ReadBitsLeq32(6).Map(x => x + 1);
        if (countR.IsFaulted)
            return new Result<Mapping[]>(countR.Error());

        var output = new Mapping[countR.Success()];

        for (var i = 0; i < output.Length; i++)
        {
            var mappingType = bs.ReadBitsLeq32(16);
            if (mappingType.IsFaulted)
                return new Result<Mapping[]>(mappingType.Error());

            if (mappingType.Success() != 0)
                return new Result<Mapping[]>(new DecodeError("vorbis: invalid mapping type"));

            var mappingResult = Mapping.Read(ref bs, identNChannels, floorsLength, residuesLength);
            if (mappingResult.IsFaulted)
                return new Result<Mapping[]>(mappingResult.Error());
            output[i] = mappingResult.Success();
        }

        return new Result<Mapping[]>(output);
    }
}

internal readonly record struct SetupResult(
    VorbisCodebook[] Codebooks,
    IFloor[] Floors,
    Residue[] Residues,
    Mode[] Modes,
    Mapping[] Mappings
);