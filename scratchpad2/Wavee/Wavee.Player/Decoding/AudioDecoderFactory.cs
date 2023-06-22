using System.Runtime.InteropServices;
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
        return new VorbisDecoderWrapper(decoderActual);
    }
}

internal class VorbisDecoderWrapper : IAudioDecoder
{
    private readonly VorbisWaveReader _decoderActual;
    //private ISampleProvider _sampleProvider;

    private const int SAMPLES = 4096;
    public VorbisDecoderWrapper(VorbisWaveReader decoderActual)
    {
        //calculate optimal buffer size
        SampleSize = SAMPLES * decoderActual.WaveFormat.Channels;
        _decoderActual = decoderActual;
     //   _sampleProvider = decoderActual.ToSampleProvider();
    }

    public int SampleSize { get; }

    public int Read(Span<float> buffer)
    {
        int read;
        //read until we have enough samples or read returns 0
        while ((read = _decoderActual.Read(buffer, 0, buffer.Length)) > 0)
        {
            //if we have enough samples, return
            if (read == buffer.Length)
            {
                return read;
            }
            //if we don't have enough samples, read more
        }
        return read;
    }

    public void Dispose()
    {
        _decoderActual.Dispose();
        //_sampleProvider = null;
    }
}