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
        _bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(SampleRate, NumChannels));
        _waveOutEvent = new WaveOutEvent
        {
            DeviceNumber = -1, // Default playback device
            DesiredLatency = 100,
            NumberOfBuffers = 2
        };
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
                var casted = MemoryMarshal.Cast<float, byte>(converter.F64ToF32(s.Span)).ToArray();
                return casted;
            },
            Right: r => r.ToArray());

        _bufferedWaveProvider.AddSamples(samples, 0, samples.Length);

        while (_bufferedWaveProvider.BufferedBytes > 26 * 1628)
        {
            // Sleep and wait for NAudio to drain a bit
            //Thread.Sleep(10);
            await Task.Delay(10);
        }
    }
}

internal struct AudioSamplesConverter
{
    public ReadOnlySpan<float> F64ToF32(ReadOnlySpan<double> samples)
    {
        throw new NotImplementedException();
    }
}