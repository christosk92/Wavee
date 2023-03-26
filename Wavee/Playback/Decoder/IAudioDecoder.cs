using Wavee.Playback.Packets;

namespace Wavee.Playback.Decoder;

public interface IAudioDecoder
{
    (AudioPacketPosition Position, IAudioPacket Packet)? NextPacket();

    int SampleRate
    {
        get;
    }
    int Channels
    {
        get;
    }
}

public readonly record struct AudioPacketPosition(double PositionMs, bool Skipped);