namespace Wavee.Core.Decoders.VorbisDecoder.Decoder;

internal sealed class Dsp
{
    public Dsp(Windows windows, DspChannel[] dspChannels, Imdct imdctShort, Imdct imdctLong)
    {
        Windows = windows;
        Channels = dspChannels;
        ImdctShort = imdctShort;
        ImdctLong = imdctLong;
        LappingState = null;
    }

    public Windows Windows { get; }
    public DspChannel[] Channels { get; }
    public Imdct ImdctShort { get; }
    public Imdct ImdctLong { get; }
    public LappingState? LappingState { get; set; }
}

internal class LappingState
{
    public bool PrevBlockFlag { get; set; }
}