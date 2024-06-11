using Wavee.Core.Decoders.VorbisDecoder.Mapper;

namespace Wavee.Core.Decoders.VorbisDecoder.Decoder;

internal sealed class SignalSpec
{
    public SignalSpec(uint rate, Channels channels)
    {
        Rate = (uint)rate;
        Channels = channels;
    }

    public uint Rate { get; }
    public Channels Channels { get; }
}