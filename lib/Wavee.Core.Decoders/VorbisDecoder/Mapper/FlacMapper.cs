using LanguageExt;
using LanguageExt.Common;
using Wavee.Core.Decoders.VorbisDecoder.Format;

namespace Wavee.Core.Decoders.VorbisDecoder.Mapper;

internal sealed class FlacMapper : IMapper
{
    public static Option<IMapper> Detect(byte[] buf)
    {
        return Option<IMapper>.None;
    }

    public string Name { get; }
    public CodecParameters CodecParams { get; }
    public bool IsReady { get; }

    public Result<IMapResult> MapPacket(byte[] data)
    {
        throw new NotImplementedException();
    }

    public ulong AbsGpToTs(ulong ts)
    {
        throw new NotImplementedException();
    }

    public Option<IPacketParser> MakeParser()
    {
        throw new NotImplementedException();
    }

    public void Reset()
    {
        throw new NotImplementedException();
    }
}