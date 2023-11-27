using LanguageExt;
using LanguageExt.Common;
using Wavee.Vorbis.Decoder.Setup.Codebooks;
using Wavee.Vorbis.Infrastructure.BitReaders;

namespace Wavee.Vorbis.Decoder.Setup.Floor;

internal interface IFloor
{
    Result<Unit> ReadChannel(ref BitReaderRtlRef bs, VorbisCodebook[] codebooks);
    Result<Unit> Synthesis(byte bsExp, Span<float> chFloor);
    bool IsUnused();
}