using System.Runtime.InteropServices;
using NAudio.Wave;

namespace Wavee.Infrastructure.Live;

internal sealed class NAudioHolder
{
    private readonly WaveOutEvent _waveOutEvent;
    private readonly BufferedWaveProvider _bufferedWaveProvider;
    private const int NumChannels = 2;
    private const int SampleRate = 44100;

    public NAudioHolder()
    {
        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, NumChannels);
        _bufferedWaveProvider = new BufferedWaveProvider(waveFormat);
        _waveOutEvent = new WaveOutEvent();
        _waveOutEvent.Init(_bufferedWaveProvider);
    }

    public void Start()
    {
        _waveOutEvent.Play();
    }

    public void Stop()
    {
        _waveOutEvent.Stop();
    }

    public async Task Write(
        Either<ReadOnlyMemory<double>, ReadOnlyMemory<byte>> packet,
        AudioSamplesConverter converter)
    {
        var samples = packet.Match(
            Left: s =>
            {
                //cast to byte
                var casted = MemoryMarshal.Cast<float, byte>(AudioSamplesConverter.F64ToF32(s.Span)).ToArray();
                return casted;
            },
            Right: r => r.ToArray());

        _bufferedWaveProvider.AddSamples(samples, 0, samples.Length);

        while (_bufferedWaveProvider.BufferedDuration.TotalSeconds > 0.5)
        {
            await Task.Delay(10);
        }
    }

    public void DiscardBuffer()
    {
        _bufferedWaveProvider.ClearBuffer();
    }
}

internal struct AudioSamplesConverter
{
    //In terms of F64 -> F32, since F64 has a higher precision than F32, we can just cast the bits
    public static ReadOnlySpan<float> F64ToF32(ReadOnlySpan<double> samples) =>
        MemoryMarshal.Cast<double, float>(samples);
}