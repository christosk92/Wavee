namespace Wavee.Playback.Packets;

public interface IAudioPacket
{
    int SampleRate { get; }
    int Channels { get; }
}

public readonly record struct SamplesPacket(double[] Samples) : IAudioPacket
{
    public bool IsEmpty
    {
        get
        {
            return Samples.Length == 0;
        }
    }

    public int SampleRate
    {
        get;
        init;
    }

    public int Channels
    {
        get;
        init;
    }
}

public readonly record struct RawPacket(byte[] Raw) : IAudioPacket
{
    public int SampleRate
    {
        get;
    }

    public int Channels
    {
        get;
    }
}