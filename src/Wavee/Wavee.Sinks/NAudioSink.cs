using System.Buffers;
using NAudio.Wave;

namespace Wavee.Sinks;

public sealed class NAudioSink
{
    private static NAudioSink _instance;
    private readonly IWavePlayer _wavePlayer;
    private readonly WaveFormat waveFormat;
    private readonly BufferedWaveProvider _bufferedWaveProvider;

    public NAudioSink()
    {
        const int sampleRate = 44100;
        const int channels = 2;
        _wavePlayer = new WaveOutEvent();
        waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
        _bufferedWaveProvider = new BufferedWaveProvider(waveFormat);
        _wavePlayer.Init(_bufferedWaveProvider);
        _wavePlayer.Volume = 1;
    }

    public void Pause()
    {
        _wavePlayer.Pause();
    }

    public void Resume()
    {
        _wavePlayer.Play();
    }

    public void DiscardBuffer()
    {
        _bufferedWaveProvider.ClearBuffer();
    }

    public void Write(ReadOnlySpan<byte> samplesSpan)
    {
        var samples = ArrayPool<byte>.Shared.Rent(samplesSpan.Length);
        try
        {
            samplesSpan.CopyTo(samples);

            _bufferedWaveProvider.AddSamples(samples, 0, samplesSpan.Length);

            while (_bufferedWaveProvider.BufferedDuration.TotalSeconds > 0.5)
            {
                Thread.Sleep(1);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(samples);
        }
    }

    public void SetVolume(double volume)
    {
        volume = Math.Clamp(volume, 0, 1);
        _wavePlayer.Volume = (float)volume;
    }

    public double Volume
    {
        get => _wavePlayer.Volume;
        set => _wavePlayer.Volume = (float)value;
    }

    public static NAudioSink Instance => _instance ??= new NAudioSink();
}