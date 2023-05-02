using LanguageExt.UnsafeValueAccess;
using NAudio.Wave;
using Wavee.VorbisDecoder.Convenience;

namespace Wavee.Infrastructure.Live;

internal readonly struct AudioInputIO : Traits.AudioInputIO
{
    public static Traits.AudioInputIO Default = new AudioInputIO();
    public bool CanReadRaw => true;
    public bool CanReadSamples => false;

    public IAsyncEnumerable<ReadOnlyMemory<double>> ReadSamples(Stream stream)
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadRaw(Stream stream,
        Ref<TimeSpan> position,
        Ref<bool> close,
        int chunkSize)
    {
        //setup a continuous loop to receive the samples and return them as async enumerable
        //first lets find the magic number and open the appropriate reader/format
        //then we can start reading the samples

        var audioFormat = GetFormat(stream);

        if (audioFormat.IsNone)
        {
            throw new InvalidOperationException("Invalid or unsupported audio format.");
        }

        using var cts = new CancellationTokenSource();
        var format = audioFormat.ValueUnsafe();
        await using var sampleReader = OpenSampleReader(stream, format);

        using var positionObs = position.OnChange().Subscribe(ts =>
        {
            sampleReader.CurrentTime = ts;
        });
        
        using var closeObs = close.OnChange().Subscribe(close =>
        {
            if (close)
                cts.Cancel();
        });

        var buffer = new byte[chunkSize];
        int bytesRead;

        while ((bytesRead = await sampleReader.ReadAsync(buffer, 0, chunkSize, cts.Token)) > 0)
        {
            yield return new ReadOnlyMemory<byte>(buffer, 0, bytesRead);
        }
    }

    private WaveStream OpenSampleReader(Stream stream, MagicAudioFormat format)
    {
        switch (format)
        {
            case MagicAudioFormat.Mp3:
            {
                var mp3Reader = new Mp3FileReader(stream);
                var reader32 = new WaveChannel32(mp3Reader);
                return reader32;
            }
            case MagicAudioFormat.Ogg:
            {
                var vorbisStream = new VorbisWaveReader(stream, false);
                return vorbisStream;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }
    }

    private static Option<MagicAudioFormat> GetFormat(Stream stream)
    {
        Span<byte> magic = stackalloc byte[4];
        if (stream.Read(magic) != magic.Length)
        {
            return Option<MagicAudioFormat>.None;
        }

        stream.Position = 0; // Reset the position after reading the magic number

        // Check for Ogg format
        if (magic.SequenceEqual(OGG_MAGIC_NUMBER)) // OggS in ASCII
        {
            return Option<MagicAudioFormat>.Some(MagicAudioFormat.Ogg);
        }

        // Check for MP3 format
        if (magic.SequenceEqual(MP3ID3V2)) // ID3v2 in ASCII
        {
            return Option<MagicAudioFormat>.Some(MagicAudioFormat.Mp3);
        }

        // Check for MP3 format (without ID3v2)
        // MP3 files without ID3v2 tags start with a sync word (0xFFE) in the frame header
        if ((magic[0] == 0xFF) && ((magic[1] & 0xE0) == 0xE0))
        {
            return Option<MagicAudioFormat>.Some(MagicAudioFormat.Mp3);
        }

        return Option<MagicAudioFormat>.None;
    }

    private static byte[] OGG_MAGIC_NUMBER = "OggS"u8.ToArray();
    private static byte[] MP3ID3V2 = { 0x49, 0x44, 0x33, 0x03 };

    private enum MagicAudioFormat
    {
        Mp3,
        Ogg
    }
}