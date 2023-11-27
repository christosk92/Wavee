using LanguageExt;
using LanguageExt.Common;
using Wavee.Vorbis.Format;

namespace Wavee.Vorbis.Mapper;

internal static class Mappings
{
    public static Option<IMapper> Detect(byte[] buf)
    {
        return
            FlacMapper.Detect(buf)
                .BiBind(Option<IMapper>.Some, () => VorbisMapper.Detect(buf))
                .BiBind(Option<IMapper>.Some, () => OpusMapper.Detect(buf));
    }
}

internal interface IMapper
{
    string Name { get; }
    CodecParameters CodecParams { get;  }
    bool IsReady { get; }
    Result<IMapResult> MapPacket(byte[] data);
    /// <summary>
    /// Convert an absolute granular position to a timestamp
    /// </summary>
    /// <param name="ts"></param>
    /// <returns></returns>
    ulong AbsGpToTs(ulong ts);

    Option<IPacketParser> MakeParser();
    void Reset();
}

internal interface IPacketParser
{
    ulong ParseNextPacketDur(Span<byte> packet);
}