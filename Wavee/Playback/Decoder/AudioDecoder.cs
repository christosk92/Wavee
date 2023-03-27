using Wavee.Playback.Factories;
using Wavee.Playback.Packets;

namespace Wavee.Playback.Decoder;

public sealed class AudioDecoder : IAudioDecoder
{
    private readonly IAudioFormat _stream;
    private readonly int _bytesPerSample;

    internal AudioDecoder(IAudioFormat stream)
    {
        _stream = stream;
        _bytesPerSample = 4096 * _stream.Channels;
    }

    public (AudioPacketPosition Position, IAudioPacket Packet)? NextPacket()
    {
        var skipped = false;
        while (true)
        {
            try
            {
                var buffer = _stream.ReadSamples(_bytesPerSample);
                if (buffer.Length == 0)
                {
                    // End of the stream
                    return null;
                }

                var position = _stream.CurrentTime;

                var packetPosition = new AudioPacketPosition
                {
                    PositionMs = position.TotalMilliseconds,
                    Skipped = skipped,
                };

                return (packetPosition, new SamplesPacket(buffer)
                {
                    Channels = _stream.Channels,
                    SampleRate = _stream.SampleRate,
                });
            }
            catch (Exception x)
            {
                skipped = true;
            }
        }
    }
}