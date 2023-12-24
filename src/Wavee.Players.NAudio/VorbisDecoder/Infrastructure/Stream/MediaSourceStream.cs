using LanguageExt;
using LanguageExt.Common;

namespace Wavee.Vorbis.Infrastructure.Stream;

public sealed class MediaSourceStream : IReadBytes, ISeekBuffered, IDisposable
{
    const int MIN_BLOCK_LEN = 1 * 1024;
    const int MAX_BLOCK_LEN = 32 * 1024;

    private System.IO.Stream Inner { get; }
    private ArraySegment<byte> Ring { get; set; }
    private int RingMask { get; set; }
    private int ReadPos { get; set; }
    private int WritePos { get; set; }
    private int ReadBlockLen { get; set; }
    private long AbsPos { get; set; } // Assuming 64-bit because of the name 'abs'
    private long RelPos { get; set; } // Assuming 64-bit because of the name 'rel'

    public MediaSourceStream(System.IO.Stream stream, MediaSourceStreamOptions options)
    {
        // The buffer length must be a power of 2, and > the maximum read block length.
        // // The buffer length must be a power of 2, and > the maximum read block length.
        if (Convert.ToString(options.BufferLength, 2).Count(c => c == '1') != 1)
        {
            throw new ArgumentException("Buffer length must be a power of 2.", nameof(options));
        }

        if (options.BufferLength <= MAX_BLOCK_LEN)
        {
            throw new ArgumentException($"Buffer length must be greater than {MAX_BLOCK_LEN}.", nameof(options));
        }


        Inner = stream;
        Ring = new byte[options.BufferLength];
        RingMask = Ring.Count - 1;
        ReadPos = 0;
        WritePos = 0;
        ReadBlockLen = MIN_BLOCK_LEN;
        AbsPos = 0;
        RelPos = 0;
    }

    private bool IsBufferExhausted => this.ReadPos == this.WritePos;
    public bool CanSeek => this.Inner.CanSeek;

    public Option<ulong> Length
    {
        get
        {
            try
            {
                var l = (ulong)this.Inner.Length;
                return l;
            }
            catch (Exception)
            {
                return Option<ulong>.None;
            }
        }
    }

    private Result<Unit> Fetch()
    {
        try
        {
            // Only fetch when the ring buffer is empty.
            if (this.IsBufferExhausted)
            {
                // Split the vector at the write position to get slices of the two contiguous regions of
                // the ring buffer.
                var (vec1, vec0) = this.Ring.SplitAt(mid: this.WritePos);

                // If the first contiguous region of the ring buffer starting from the write position
                // has sufficient space to service the entire read do a simple read into that region's
                // slice.
                int actualReadLength = 0;
                if (vec0.Count >= this.ReadBlockLen)
                {
                    actualReadLength = this.Inner.Read(vec0.Slice(0, this.ReadBlockLen));
                }
                else
                {
                    // Otherwise, perform a vectored read into the two contiguous region slices.
                    var rem = this.ReadBlockLen - vec0.Count;

                    ArraySegment<byte>[] ringVectors = new[] { vec0, vec1.Slice(0, rem) };
                    actualReadLength = this.Inner.ReadVectored(ringVectors);
                }

                // Increment the write position, taking into account wrap-around.
                this.WritePos = (this.WritePos + actualReadLength) & this.RingMask;

                // Update the stream position accounting.
                this.AbsPos += actualReadLength;
                this.RelPos += actualReadLength;

                // Grow the read block length exponentially to reduce the overhead of buffering on
                // consecutive seeks.
                this.ReadBlockLen = Math.Min(this.ReadBlockLen << 1, MAX_BLOCK_LEN);
            }

            return Unit.Default;
        }
        catch (Exception ex)
        {
            return new Result<Unit>(ex);
        }
    }


    /// <summary>
    /// If the buffer has been exhausted, fetch a new block of data to replenish the buffer. If
    /// no more data could be fetched, return an end-of-stream error.
    /// </summary>
    /// <returns></returns>
    private Result<Unit> FetchOrEof()
    {
        var result = this.Fetch();
        if (result.IsFaulted)
        {
            return result;
        }

        if (this.IsBufferExhausted)
        {
            return new Result<Unit>(new EndOfStreamException());
        }

        return Unit.Default;
    }

    /// <summary>
    /// Advances the read position by `len` bytes, taking into account wrap-around.
    /// </summary>
    /// <param name="length"></param>
    private void Consume(int length)
    {
        this.ReadPos = (this.ReadPos + length) & this.RingMask;
    }

    /// <summary>
    /// Gets the largest contiguous slice of buffered data starting from the read position.
    /// </summary>
    /// <returns></returns>
    private ArraySegment<byte> ContinguousBuffer()
    {
        if (this.WritePos >= this.ReadPos)
        {
            return this.Ring.Slice(this.ReadPos, this.WritePos - this.ReadPos);
        }

        return this.Ring[ReadPos..];
    }

    private void Reset(ulong pos)
    {
        this.ReadPos = 0;
        this.WritePos = 0;
        this.ReadBlockLen = MIN_BLOCK_LEN;
        this.AbsPos = (long)pos;
        this.RelPos = 0;
    }

    public Result<Unit> ReadBufferExactly(Span<byte> buffer)
    {
        while (!buffer.IsEmpty)
        {
            try
            {
                int count = Read(buffer);

                if (count == 0)
                {
                    break;
                }

                buffer = buffer[count..];
            }
            catch (IOException ex) when (ex.InnerException is OperationCanceledException)
            {
                // Ignore and continue.
            }
        }

        if (!buffer.IsEmpty)
        {
            return new Result<Unit>(new EndOfStreamException());
        }
        
        return Unit.Default;
    }

    public int Read(Span<byte> buf)
    {
        int readLen = buf.Length;

        while (!buf.IsEmpty)
        {
            Fetch();

            try
            {
                int count = ReadContiguousBuf(ref buf);

                if (count == 0)
                {
                    break;
                }

                buf = buf.Slice(count);
                Consume(count);
            }
            catch (IOException ex) when (ex.InnerException is OperationCanceledException)
            {
                // Ignore and continue.
            }
        }

        return readLen - buf.Length;
    }

    private int ReadContiguousBuf(ref Span<byte> buf)
    {
        var continguousBuffer = this.ContinguousBuffer();

        var count = Math.Min(buf.Length, continguousBuffer.Count);
        continguousBuffer.Slice(0, count).AsSpan().CopyTo(buf);
        return count;
    }

    public Span<byte> ReadQuadBytes()
    {
        var bytes = new byte[4];

        var buf = this.ContinguousBuffer();

        if (buf.Count >= 4)
        {
            buf.Slice(0, 4).CopyTo(bytes);
            Consume(4);
        }
        else
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = ReadByte();
            }
        }

        return bytes;
    }

    public byte ReadByte()
    {
        // This function, read_byte, is inlined for performance. To reduce code bloat, place the
        // read-ahead buffer replenishment in a seperate function. Call overhead will be negligible
        // compared to the actual underlying read.
        if (this.IsBufferExhausted)
        {
            var fetchOrEofResult = this.FetchOrEof();
            if (fetchOrEofResult.IsFaulted)
            {
                throw fetchOrEofResult.Error();
            }
        }

        var value = this.Ring[this.ReadPos];
        this.Consume(1);
        return value;
    }

    private int UnreadBufferLength()
    {
        if (this.WritePos >= this.ReadPos)
        {
            return this.WritePos - this.ReadPos;
        }

        return this.WritePos + (this.Ring.Count - this.ReadPos);
    }

    public ulong Position()
    {
        return (ulong)(AbsPos - UnreadBufferLength());
    }

    public ulong SeekBuffered(ulong pos)
    {
        var oldPos = Position();

        // Forward seek.
        int delta = 0;
        if (pos > oldPos)
        {
            //            assert!(pos - old_pos < std::isize::MAX as u64);
            if (pos - oldPos > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(pos));
            }

            delta = (int)(pos - oldPos);
        }
        else if (pos < oldPos)
        {
            // Backward seek.
            if (oldPos - pos > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(pos));
            }

            delta = -(int)(oldPos - pos);
        }
        else
        {
            delta = 0;
        }

        return SeekBufferedRel(delta);
    }

    private ulong SeekBufferedRel(int delta)
    {
        if (delta < 0)
        {
            var absDelta = Math.Min(-delta, ReadBufferLen());
            ReadPos = (int)((ReadPos + Ring.Count - absDelta) & RingMask);
        }
        else if (delta > 0)
        {
            var absDelta = Math.Min(delta, UnreadBufferLength());
            ReadPos = (ReadPos + absDelta) & RingMask;
        }

        return Position();
    }

    private uint ReadBufferLen()
    {
        var unreadLen = this.UnreadBufferLength();

        return (uint)(Math.Min(this.Ring.Count, (uint)this.RelPos) - unreadLen);
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }

    public void EnsureSeekbackBuffer(int len)
    {
        int ringLen = Ring.Count;

        int newRingLen = (MAX_BLOCK_LEN + len).NextPowerOfTwo();

        if (ringLen < newRingLen)
        {
            byte[] newRing = new byte[newRingLen];

            int vec0Start = ReadPos;
            int vec0End = WritePos >= ReadPos ? WritePos : ringLen;
            int vec0Len = vec0End - vec0Start;

            int vec1Start = WritePos < ReadPos ? 0 : -1;
            int vec1End = WritePos < ReadPos ? WritePos : -1;
            int vec1Len = vec1Start >= 0 ? vec1End - vec1Start : 0;

            Array.Copy(Ring.Array, vec0Start, newRing, 0, vec0Len);
            if (vec1Start >= 0)
            {
                Array.Copy(Ring.Array, vec1Start, newRing, vec0Len, vec1Len);
                WritePos = vec0Len + vec1Len;
            }
            else
            {
                WritePos = vec0Len;
            }

            Ring = newRing;
            RingMask = newRingLen - 1;
            ReadPos = 0;
        }
    }

    public ulong Seek(SeekOrigin origin, ulong pos)
    {
        // The current position of the underlying reader is ahead of the current position of the
        // MediaSourceStream by how ever many bytes have not been read from the read-ahead buffer
        // yet. When seeking from the current position adjust the position delta to offset that
        // difference.

        ulong seekedTo = 0;
        switch (origin)
        {
            case SeekOrigin.Begin:
                seekedTo = (ulong)Inner.Seek((long)pos, SeekOrigin.Begin);
                break;
            case SeekOrigin.Current:
                var delta = (long)pos - this.UnreadBufferLength();
                seekedTo = (ulong)Inner.Seek(delta, SeekOrigin.Current);
                break;
            case SeekOrigin.End:
                seekedTo = (ulong)Inner.Seek((long)pos, SeekOrigin.End);
                break;
        }


        this.Reset(seekedTo);
        return pos;
    }
}