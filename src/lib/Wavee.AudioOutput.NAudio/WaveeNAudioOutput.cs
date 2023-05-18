using LanguageExt;
using NAudio.Vorbis;
using NAudio.Wave;
using Wavee.Core.Playback;

namespace Wavee.AudioOutput.NAudio;

internal sealed class WaveeNAudioOutput : IWaveeAudioOutput
{
    private readonly WaveOutEvent _wavePlayer;
    private readonly BufferedWaveProvider _bufferedWaveProvider;

    public WaveeNAudioOutput()
    {
        const int sampleRate = 44100;
        const int channels = 2;
        _wavePlayer = new WaveOutEvent();
        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
        _bufferedWaveProvider = new BufferedWaveProvider(waveFormat);
        _wavePlayer.Init(_bufferedWaveProvider);
    }

    public IAudioDecoder OpenDecoder(Stream stream, TimeSpan duration)
    {
        var format = ReadFormat(stream);
        return format switch
        {
            MagicAudioFileType.Vorbis => new WaveStreamMasker(new VorbisWaveReader(stream, true), duration),
            MagicAudioFileType.Mp3 => new WaveStreamMasker(new WaveChannel32((new Mp3FileReader(stream))), duration),
            _ => throw new NotSupportedException($"Unsupported audio format: {format}")
        };
    }

    public Unit Pause()
    {
        _wavePlayer.Pause();
        return default;
    }

    public Unit Resume()
    {
        _wavePlayer.Play();
        return default;
    }

    public Unit WriteSamples(ReadOnlySpan<byte> samples)
    {
        _bufferedWaveProvider.AddSamples(samples.ToArray(), 0, samples.Length);
        while (_bufferedWaveProvider.BufferedDuration.TotalSeconds > 0.5)
        {
            Thread.Sleep(10);
        }

        return default;
    }

    public Unit DiscardBuffer()
    {
        _bufferedWaveProvider.ClearBuffer();
        return default;
    }

    public void Dispose()
    {
    }

    private static MagicAudioFileType ReadFormat(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        Span<byte> magic = new byte[4];
        stream.ReadExactly(magic);

        stream.Seek(0, SeekOrigin.Begin);
        if (magic.SequenceEqual(OggMagic))
        {
            return MagicAudioFileType.Vorbis;
        }

        if (IsMp3(stream, magic))
        {
            return MagicAudioFileType.Mp3;
        }

        return MagicAudioFileType.Unknown;
    }

    private static bool IsMp3(Stream stream, Span<byte> magic)
    {
        // Check for ID3v2 tags
        if (magic[0] == 0x49 && magic[1] == 0x44 && magic[2] == 0x33)
        {
            return true;
        }
        else
        {
            // Check for MP3 frame sync bits without ID3v2
            return ((magic[0] & 0xFF) == 0xFF) && ((magic[1] & 0xE0) == 0xE0);
        }
    }

    private static byte[] OggMagic = "OggS"u8.ToArray();

    private enum MagicAudioFileType
    {
        Unknown,
        Vorbis,
        Mp3
    }
}