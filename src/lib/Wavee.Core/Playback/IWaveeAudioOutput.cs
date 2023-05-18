using LanguageExt;

namespace Wavee.Core.Playback;

public interface IWaveeAudioOutput : IDisposable
{
    IAudioDecoder OpenDecoder(Stream stream, TimeSpan duration);
    Unit Pause();
    Unit Resume();
    Unit WriteSamples(ReadOnlySpan<byte> samples);
    Unit DiscardBuffer();
}
