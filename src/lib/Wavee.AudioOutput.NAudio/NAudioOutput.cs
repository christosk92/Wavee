using System.Buffers;
using System.Runtime.InteropServices;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using NAudio.Wave;
using Wavee.Core;
using Wavee.Core.Contracts;
using Wavee.Core.Infrastructure.Traits;
using static LanguageExt.Prelude;

namespace Wavee.AudioOutput.NAudio;

public sealed class NAudioOutput : AudioOutputIO
{
    private const int sampleRate = 44100;
    private const int channels = 2;
    private static readonly WaveOutEvent _waveOutEvent = new();
    private static readonly NAudioOutput _instance = new();
    private readonly WaveFormat waveFormat;

    private readonly BufferedWaveProvider _bufferedWaveProvider;

    public NAudioOutput()
    {
        waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
        _bufferedWaveProvider = new BufferedWaveProvider(waveFormat)
        {

        };
        _waveOutEvent.Init(_bufferedWaveProvider);
    }

    public static void SetAsMainOutput()
    {
        _ = WaveeCore.Runtime;
        atomic(() => WaveeCore.AudioOutput.Swap(_ => _instance));
    }


    public Unit Start()
    {
        _waveOutEvent.Play();
        return Prelude.unit;
    }

    public Unit Pause()
    {
        _waveOutEvent.Pause();
        return Prelude.unit;
    }

    public ValueTask<IAudioDecoder> OpenDecoder(IAudioStream stream)
    {
        var decoder = AudioDecoderRuntime.OpenAudioDecoder(stream.AsStream()).Run().ThrowIfFail();
        return new ValueTask<IAudioDecoder>(new NAudioMaskedAsAudioDecoder(decoder, stream.Track.Duration));
    }

    public Unit DiscardSamples()
    {
        _bufferedWaveProvider.ClearBuffer();
        return Prelude.unit;
    }

    public Unit WriteSamples(ReadOnlySpan<float> sample)
    {
        //cast to byte array
        var byteSpan = MemoryMarshal.Cast<float, byte>(sample).ToArray();

        //write to output
        _bufferedWaveProvider.AddSamples(byteSpan, 0, byteSpan.Length);
        while (_bufferedWaveProvider.BufferedDuration.TotalSeconds > 0.5)
        {
            Thread.Sleep(50);
        }

        return unit;
    }

    private class NAudioMaskedAsAudioDecoder : IAudioDecoder
    {
        private readonly WaveStream _waveStream;
        private readonly ISampleProvider _sampleProvider;

        public NAudioMaskedAsAudioDecoder(WaveStream waveStream, TimeSpan totalTime)
        {
            _waveStream = waveStream;
            TotalTime = totalTime;
            _sampleProvider = _waveStream.ToSampleProvider();
        }

        public void Dispose()
        {
            _waveStream.Dispose();
        }

        public Span<float> ReadSamples(int samples)
        {
            var output = new float[samples];
            var read = _sampleProvider.Read(output, 0, samples);
            return output.AsSpan(0, read);
        }

        public TimeSpan Position => _waveStream.CurrentTime;
        public TimeSpan TotalTime { get; }

        public void Seek(TimeSpan pPosition)
        {
            _waveStream.CurrentTime = pPosition;
        }
    }
}