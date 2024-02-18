using System.Runtime.InteropServices;
using NAudio.Wave;

namespace Wavee.Core;

internal sealed class NAudioDecoder : IAudioDecoder
{
    private readonly WaveStream _stream;
    private TimeSpan _totalDuration;

    public NAudioDecoder(WaveStream stream, TimeSpan totalDuration)
    {
        _stream = stream;
        _totalDuration = totalDuration;
    }

    public void Dispose()
    {
        _stream.Dispose();
    }

    public int Channels => _stream.WaveFormat.Channels;
    public int SampleRate => _stream.WaveFormat.SampleRate;

    public bool IsEndOfStream
    {
        get
        {
            // _stream.CurrentTime >= _totalDuration; might not work because of rounding errors
            //only check until milliseconds component (with a delta of 10ms)
            return _stream.CurrentTime.TotalSeconds >= _totalDuration.TotalSeconds && _stream.CurrentTime.Milliseconds >= _totalDuration.Milliseconds - 10;
        }
    }
    public TimeSpan Position => _stream.CurrentTime;
    public TimeSpan TotalDuration => _totalDuration;
    public int ReadSamples(Span<float> samples)
    {
        var bytes = samples.Length * sizeof(float);
        Span<byte> buffer = new byte[bytes];
        var read = _stream.Read(buffer);
        var floats = MemoryMarshal.Cast<byte, float>(buffer.Slice(0, read));
        floats.CopyTo(samples);
        return read / sizeof(float);
    }

    public void Seek(TimeSpan position)
    {
        Seek(position, 0);
    }

    private void Seek(TimeSpan position, int iteration)
    {
        const int maxIterations = 50;
        
        try
        {
            _stream.CurrentTime = position;
        }
        catch (Exception e)
        {
            //Log: "An error occurred while seeking." Seeking sligly earlier than provided position. Iteration: {iteration}
            if (iteration < maxIterations)
            {
                //Log.Warning(e, "An error occurred while seeking. Seeking sligly earlier than provided position. Iteration: {iteration}", iteration);
                Console.WriteLine($"An error occurred while seeking. Seeking sligly earlier than provided position. Iteration: {iteration}");
                Seek(position - TimeSpan.FromMilliseconds(10), iteration + 1);
            }
            else
            {
                //Log: "An error occurred while seeking." Seeking failed. Iteration: {iteration}
                Console.WriteLine($"An error occurred while seeking. Seeking failed. Iteration: {iteration}");
                //Log.Error(e, "An error occurred while seeking. Seeking failed. Iteration: {iteration}", iteration);
            }
        }
    }
}