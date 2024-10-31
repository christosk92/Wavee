using LanguageExt;
using LanguageExt.Common;
using Wavee.Core.Decoders.VorbisDecoder.Format;

namespace Wavee.Core.Decoders.VorbisDecoder.Mapper;

internal sealed class OpusMapper : IMapper
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
        /*
         *   match &self.parser {
            Some(base_parser) => {
                let parser = Box::new(VorbisPacketParser::new(
                    base_parser.bs0_exp,
                    base_parser.bs1_exp,
                    base_parser.num_modes,
                    base_parser.modes_block_flags,
                ));
                Some(parser)
            }
            _ => None,
        }
         */
        return Option<IPacketParser>.None;
    }

    public void Reset()
    {
        throw new NotImplementedException();
    }
}