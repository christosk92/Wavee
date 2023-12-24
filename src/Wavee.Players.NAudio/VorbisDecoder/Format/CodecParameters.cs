using LanguageExt;
using Wavee.Vorbis.Decoder;
using Wavee.Vorbis.Mapper;

namespace Wavee.Vorbis.Format;

public sealed class CodecParameters : ICloneable
{
    internal Option<byte[]> ExtraData { get; set; }
    internal uint CodecType { get; private set; }
    public Option<uint> SampleRate { get; private set; }
    internal Option<ulong> NFrames { get; private set; }
    internal Option<uint> Padding { get; private set; }
    internal TimeBase TimeBase { get; private set; }
    public Option<Channels> Channels { get; private set; } = Option<Channels>.None;
    internal ulong StartTs { get; private set; }
    internal uint Delay { get; private set; }
    public Option<int> ChannelsCount => Channels.Map(x => (int)x.Count());

    internal CodecParameters ForCodec(uint codecTypeVorbis)
    {
        CodecType = codecTypeVorbis;
        return this;
    }

    internal CodecParameters WithExtraData(byte[] extraData)
    {
        ExtraData = extraData;
        return this;
    }

    internal CodecParameters WithSampleRate(uint sampleRate)
    {
        SampleRate = sampleRate;
        return this;
    }

    internal CodecParameters WithTimeBase(TimeBase timeBase)
    {
        TimeBase = timeBase;
        return this;
    }

    internal CodecParameters WithChannels(Channels valueUnsafe)
    {
        Channels = valueUnsafe;
        return this;
    }

    public CodecParameters WithStartTs(ulong startTs)
    {
        StartTs = startTs;
        return this;
    }

    public CodecParameters WithDelay(uint boundDelay)
    {
        Delay = boundDelay;
        return this;
    }

    public CodecParameters WithNFrames(ulong nframes)
    {
        NFrames = nframes;
        return this;
    }

    public CodecParameters WithPadding(uint padding)
    {
        Padding = padding;
        return this;
    }

    public object Clone()
    {
        return this.MemberwiseClone();
    }
}