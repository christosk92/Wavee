using System.Runtime.InteropServices;
using LanguageExt;
using NAudio.Vorbis;
using NAudio.Wave;

namespace Wavee.Player.Decoding;

internal static class AudioDecoderFactory
{
    public static IAudioDecoder CreateDecoder(Stream stream, TimeSpan duration)
    {
        var decoderActual = new VorbisWaveReader(
            sourceStream: stream,
            duration: duration,
            closeOnDispose: false
        );
        return new VorbisDecoderWrapper(decoderActual, duration);
    }
}

internal class VorbisDecoderWrapper : IAudioDecoder
{
    private readonly VorbisWaveReader _decoderActual;

    private TimeSpan _crossfadeDuration;

    private bool _crossfadingOut;

    private bool _crossfadingIn;
    //private ISampleProvider _sampleProvider;

    private const int SAMPLES = 4096;

    public VorbisDecoderWrapper(VorbisWaveReader decoderActual, TimeSpan totalTime)
    {
        //calculate optimal buffer size
        SampleSize = SAMPLES * decoderActual.WaveFormat.Channels;
        _decoderActual = decoderActual;
        TotalTime = totalTime;
        //   _sampleProvider = decoderActual.ToSampleProvider();
    }

    public int SampleSize { get; }
    public bool IsMarkedForCrossfadeOut => _crossfadingOut;
    public TimeSpan CurrentTime => _decoderActual.CurrentTime;
    public TimeSpan TotalTime { get; }

    public int Read(Span<float> buffer)
    {
        int read;
        //read until we have enough samples or read returns 0
        while ((read = _decoderActual.Read(buffer, 0, buffer.Length)) > 0)
        {
            //if we have enough samples, return
            if (read == buffer.Length)
            {
                var gain = CalculateGain(_decoderActual.CurrentTime);
                for (var i = 0; i < read; i++)
                {
                    buffer[i] *= gain;
                }
                return read;
            }
            //if we don't have enough samples, read more
        }

        return read;
    }

    public Unit MarkForCrossfadeOut(TimeSpan duration)
    {
        _crossfadeDuration = duration;
        _crossfadingOut = true;
        _crossfadingIn = false;
        return Unit.Default;
    }

    public Unit MarkForCrossfadeIn(TimeSpan duration)
    {
        _crossfadeDuration = duration;
        _crossfadingIn = true;
        _crossfadingOut = false;
        return Unit.Default;
    }

    private float CalculateGain(TimeSpan time)
    {
        if (_crossfadeDuration == TimeSpan.Zero)
        {
            return 1;
        }

        if (_crossfadingOut)
        {
            var diffrence = TotalTime - time;
            //if this approaches 0, then 0/(x) -> 0, 
            //if this approaches 10 seconds, and crossfadeDur = 10 seconds, then 10/10 -> 1
            var multiplier = (float)(diffrence.TotalSeconds / _crossfadeDuration.TotalSeconds);
            multiplier = multiplier.Clamp(0, 1);
            return multiplier;
        }

        if (_crossfadingIn)
        {
            var difference = _crossfadeDuration - time;
            var progress = (float)(difference.TotalSeconds / _crossfadeDuration.TotalSeconds);
            //if diff approaches 0, (meaning we have reached it) then this will result in 0/x -> 0
            //so we need to get the complement of this
            var multiplier = progress.Clamp(0, 1);
            multiplier = 1 - multiplier;
            return multiplier;
        }

        return 1;
    }

    public void Dispose()
    {
        _decoderActual.Dispose();
        //_sampleProvider = null;
    }
}