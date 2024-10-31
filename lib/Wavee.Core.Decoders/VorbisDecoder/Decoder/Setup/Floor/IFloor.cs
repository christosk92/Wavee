using LanguageExt;
using LanguageExt.Common;
using Wavee.Core.Decoders.VorbisDecoder.Decoder.Setup.Codebooks;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure.BitReaders;

namespace Wavee.Core.Decoders.VorbisDecoder.Decoder.Setup.Floor;

internal interface IFloor
{
    Result<Unit> ReadChannel(ref BitReaderRtlRef bs, VorbisCodebook[] codebooks);
    Result<Unit> Synthesis(byte bsExp, Span<float> chFloor);
    bool IsUnused();
}