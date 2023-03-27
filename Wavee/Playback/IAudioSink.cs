using Wavee.Playback.Packets;

namespace Wavee.Playback;

public interface IAudioSink
{
    void Write(IAudioPacket packet, IAudioConverter converter);
    void Start(int channels, int sampleRate);
    void Stop();
}

public interface IAudioConverter
{
    ReadOnlySpan<float> F64ToF32(ReadOnlySpan<double> samples);
    ReadOnlySpan<int> F64ToS32(ReadOnlySpan<double> samples);
    ReadOnlySpan<short> F64ToS16(ReadOnlySpan<double> samples);
}

public enum MagicAudioFormat
{
    Unknown,
    Wav,
    Mp3,
    Ogg
}