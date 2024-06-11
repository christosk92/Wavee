using LanguageExt;
using LanguageExt.Common;
using Wavee.Core.Decoders.VorbisDecoder.Decoder.Setup.Codebooks;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure.BitReaders;

namespace Wavee.Core.Decoders.VorbisDecoder.Decoder.Setup.Floor;

internal sealed class Floor0 : IFloor
{
    public static Result<Floor0> Read(ref BitReaderRtlRef bs, byte identBs0Exp, byte identBs1Exp, byte maxCodebook)
    {
        throw new NotImplementedException();
    }

    public Result<Unit> ReadChannel(ref BitReaderRtlRef bs, VorbisCodebook[] codebooks)
    {
        throw new NotImplementedException();
    }

    public Result<Unit> Synthesis(byte bsExp, Span<float> chFloor)
    {
        throw new NotImplementedException();
    }

    public bool IsUnused()
    {
        throw new NotImplementedException();
    }
}