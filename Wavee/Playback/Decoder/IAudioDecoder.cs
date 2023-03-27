using Wavee.Playback.Packets;

namespace Wavee.Playback.Decoder;

public interface IAudioDecoder
{
    (AudioPacketPosition Position, IAudioPacket Packet)? NextPacket();
}

public readonly record struct AudioPacketPosition(double PositionMs, bool Skipped);