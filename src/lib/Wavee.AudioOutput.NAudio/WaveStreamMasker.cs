using NAudio.Wave;
using Wavee.Core.Playback;

namespace Wavee.AudioOutput.NAudio;

internal sealed class WaveStreamMasker : IAudioDecoder
{
    private readonly WaveStream _waveStream;
    private readonly ISampleProvider _sampleProvider;
    private readonly TimeSpan _duration;

    public WaveStreamMasker(WaveStream waveStream, TimeSpan duration)
    {
        _waveStream = waveStream;
        _duration = duration;
        _sampleProvider = waveStream.ToSampleProvider();
    }

    public void Dispose()
    {
        _waveStream.Dispose();
    }

    public TimeSpan DecodePosition => _waveStream.CurrentTime;
    public bool Ended => _waveStream.CurrentTime >= _duration;

    public void ReadSamples(float[] buffer)
    {
        _sampleProvider.Read(buffer, 0, buffer.Length);
    }

    public IAudioDecoder Seek(TimeSpan to)
    {
        bool completed = false;
        while (!completed)
        {
            try
            {
                _waveStream.CurrentTime = to;
                completed = true;
            }
            catch (Exception)
            {
                //try again, a bit earlier
                to = to.Subtract(TimeSpan.FromMilliseconds(100));
            }
        }

        return this;
    }
}