using System.Numerics;
using System.Runtime.CompilerServices;
using LanguageExt;
using LanguageExt.Common;
using Wavee.Vorbis.Mapper;

namespace Wavee.Vorbis.Decoder;

/// <summary>
/// `AudioBuffer` is a container for multi-channel planar audio sample data. An `AudioBuffer` is
/// characterized by the duration (capacity), and audio specification (channels and sample rate).
/// The capacity of an `AudioBuffer` is the maximum number of samples the buffer may store per
/// channel. Manipulation of samples is accomplished through the Signal trait or direct buffer
/// manipulation.
/// </summary>
/// <typeparam name="S"></typeparam>
internal sealed class AudioBuffer<S> where S : unmanaged
{
    private readonly S[] _buf;
    private readonly SignalSpec _spec;
    private int _nFrames;
    private readonly int _nCapacity;


    private readonly S[] _sampleBuf;
    private int _nWritten;

    public AudioBuffer(ulong duration, SignalSpec spec)
    {
        // The number of channels * duration cannot exceed u64::MAX.
        //assert!(duration <= u64::MAX / spec.channels.count() as u64, "duration too large");
        var channels = spec.Channels.Count();
        if (duration > ulong.MaxValue / channels)
            throw new ArgumentException("duration too large");

        // The total number of samples the buffer will store.
        var nSamples = duration * (ulong)channels;

        // Practically speaking, it is not possible to allocate more than usize::MAX bytes of
        // samples. This assertion ensures the potential downcast of n_samples to usize below is
        // safe.
        unsafe
        {
            if (nSamples > (ulong)(int.MaxValue / sizeof(S)))
            {
                throw new ArgumentException("Duration too large");
            }
        }

        // Allocate sample buffer and default initialize all samples to silence.
        _buf = new S[(int)nSamples];
        for (int i = 0; i < (int)nSamples; ++i)
        {
            _buf[i] = default;
        }

        _spec = spec;
        _nFrames = 0;
        _nCapacity = (int)((int)nSamples / spec.Channels.Count());

        _sampleBuf = Array.Empty<S>();
        _nWritten = 0;
        // Allocate enough memory for all the samples and fill the buffer with silence.
        _sampleBuf = new S[(int)nSamples];
        _sampleBuf.AsSpan().Fill(default);

        _zeroes = new S[(int)nSamples];
        _zeroes.Span.Fill(default);
        _nWritten = 0;
    }
    private static Memory<S> _zeroes;

    public void Clear()
    {
        _nFrames = 0;
    }

    public Result<S[]> AsAudioBufferRef()
    {
        var nChannels = _spec.Channels.Count();
        var nSamples = (int)(_nFrames * nChannels);

        // Ensure that the capacity of the sample buffer is greater than or equal to the number
        // of samples that will be copied from the source buffer.
        if (_nCapacity < nSamples)
            return new Result<S[]>(new ArgumentException("Capacity exceeded"));

        // Interleave the source buffer channels into the sample buffer.
        for (int ch = 0; ch < nChannels; ch++)
        {
            var ch_slice =  Chan(ch);

            int dstIdx = ch;
            foreach (var sample in ch_slice)
            {
                _sampleBuf[dstIdx] = sample;
                dstIdx += (int)nChannels;
            }
        }

        // Commit the written samples.
        _nWritten = nSamples;
        return _sampleBuf[..nSamples];
    }

    private ReadOnlySpan<S> Chan(int ch)
    {
        var start = ch * _nCapacity;

        if (start + _nCapacity > _buf.Length())
        {
            throw new IndexOutOfRangeException();
        }

        return _buf.AsSpan().Slice(start, _nFrames);
    }

    public Result<Unit> RenderReserve(int? renderLen)
    {
        var nReservedFrames = renderLen ?? (_nCapacity - _nFrames);
        if (_nFrames + nReservedFrames > _nCapacity)
        {
            return new Result<Unit>(new ArgumentException("Capacity will be exceeded"));
        }

        _nFrames += nReservedFrames;
        return new Result<Unit>(Unit.Default);
    }

    public Span<S> ChanMut(int channels)
    {
        var start = channels * _nCapacity;

        if (start + _nCapacity > _buf.Length)
        {
            throw new IndexOutOfRangeException();
        }

        return _buf.AsSpan().Slice(start, _nFrames);
    }

    /// <summary>
    /// Trims samples from the start and end of the buffer.
    /// </summary>
    /// <param name="packetTrimStart"></param>
    /// <param name="packetTrimEnd"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void Trim(uint start, uint end)
    {
        // First, trim the end to reduce the number of frames have to be shifted when the front is
        // trimmed.
        Truncate((uint)(Math.Min(uint.MaxValue, _nFrames + end)));

        Shift(start);
    }

    private void Truncate(uint frames)
    {
        if (frames < _nFrames)
        {
            _nFrames = (int)frames;
        }
    }

    private void Shift(uint shift)
    {
        if (shift >= _nFrames)
            Clear();
        else if (shift > 0)
        {
            // Shift the samples down in each plane.
            int chunkSize = _nCapacity;
            int totalChunks = (int)Math.Ceiling((double)_buf.Length / chunkSize);

            for (int i = 0; i < totalChunks; i++)
            {
                int chunkStart = i * chunkSize;
                int chunkEnd = Math.Min(chunkStart + chunkSize, _buf.Length);
                int copyStart = (int)(chunkStart + shift);
                int copyEnd = Math.Min(copyStart + _nFrames, chunkEnd);

                if (copyStart < chunkEnd && copyEnd <= chunkEnd)
                {
                    _buf.AsSpan().Slice(copyStart, copyEnd - copyStart).CopyTo(_buf.AsSpan()[chunkStart..]);
                }
            }

            _nFrames -= (int)shift;
        }
    }
}

internal static class ChannelsExtensions
{
    public static uint Count(this Channels channels)
    {
        return (uint)BitOperations.PopCount((uint)channels);
    }
}