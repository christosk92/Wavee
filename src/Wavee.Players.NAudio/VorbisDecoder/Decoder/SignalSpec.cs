using Wavee.Vorbis.Mapper;

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