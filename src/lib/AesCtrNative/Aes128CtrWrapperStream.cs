using AesCtr;

namespace AesCtrNative;

public class Aes128CtrWrapperStream : Stream
{
    private readonly Aes128CtrStream _stream;
    private byte[]? _bufferCache;

    public Aes128CtrWrapperStream(Aes128CtrStream decryptedOriginal)
    {
        _stream = decryptedOriginal;
    }

    public override void Flush()
    {
        _stream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int totalBytesRead = 0;
        //check if we have a cache
        if (_bufferCache is not null && _bufferCache.Length > 0)
        {
            //read those first
            var bytesToCopy = Math.Min(_bufferCache.Length, count);
            _bufferCache.AsSpan(0, bytesToCopy).CopyTo(buffer.AsSpan(offset));
            offset += bytesToCopy;
            count -= bytesToCopy;
            totalBytesRead += bytesToCopy;
            //remove the bytes we copied
            _bufferCache = _bufferCache.AsSpan(bytesToCopy).ToArray();
        }

        while (count > 0)
        {
            int chunkSize;
            if (_remainder > 0)
            {
                // if we have remainder from the last read, we need to read these bytes first
                chunkSize = (int)(_remainder + Math.Min(count, 0x4000 - _remainder));
                // Ensure we read in multiples of 16 bytes.
                chunkSize = ((chunkSize / 16) + (chunkSize % 16 > 0 ? 1 : 0)) * 16;
            }
            else
            {
                chunkSize = Math.Min(count, 0x4000); // Maximum chunk size is 0x4000 bytes.
                // Ensure we read in multiples of 16 bytes.
                chunkSize = ((chunkSize / 16) + (chunkSize % 16 > 0 ? 1 : 0)) * 16;
            }

            Span<byte> tempBuffer = new byte[chunkSize];
            int bytesRead = _stream.Read(tempBuffer);
            if (bytesRead == 0)
                return totalBytesRead; // End of stream.
            // If the chunk size is not a multiple of 16, we've read some padding, which we discard.
            if (chunkSize % 16 != 0)
            {
                bytesRead -= (16 - chunkSize % 16);
            }

            if (_remainder > 0)
            {
                bytesRead -= (int)_remainder;
            }

            //only read the bytes that we need
            var bytesToCopy = Math.Min(bytesRead, count);
            tempBuffer.Slice((int)_remainder, bytesToCopy).CopyTo(buffer.AsSpan(offset));
            //Array.Copy(tempBuffer, _remainder, buffer, offset, bytesRead);
            offset += bytesToCopy;
            totalBytesRead += bytesToCopy;
            count -= bytesToCopy;

            //if there's a discrepancy between the bytes we read and the bytes we need, cache the remainder
            var off = bytesRead - bytesToCopy;
            if (off > 0)
            {
                if (_bufferCache is null)
                {
                    //allocate new buffer
                    _bufferCache = new byte[off];
                }
                else
                {
                    //check if size is sufficient
                    if (_bufferCache.Length < off)
                    {
                        //resize
                        var original = _bufferCache.Length;
                        Array.Resize(ref _bufferCache, off);
                        //fill with zeros (after resize, the array is bigger, but the content is the same)
                        Array.Clear(_bufferCache, original, _bufferCache.Length - original);
                    }
                }

                //put remainder in cache
                tempBuffer.Slice(bytesRead - off, off).CopyTo(_bufferCache);
            }

            _remainder = 0;
        }
        
        return totalBytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        //we can only seek to positions that are a multiple of 16
        var position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => Length - offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
        };
        //since we can only seek to positions that are a multiple of 16, seek to the closest position that is a multiple of 16
        //and cache the remaining offset
        var originalPosition = position;
        var remainder = position % 16;
        position -= remainder;
        _remainder = remainder;

        //so lets say we seek to 167, we seek to 160 and cache 7
        //which means that for every read we need to skip 7 bytes
        var seekedTo = _stream.Seek(position, SeekOrigin.Begin);
        //clear cache if we seeked to a different position
        _bufferCache = Array.Empty<byte>();
        return originalPosition;
    }

    private long _remainder;

    public override void SetLength(long value)
    {
        _stream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _stream.Write(buffer, offset, count);
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => _stream.CanWrite;
    public override long Length => _stream.Length;

    public override long Position
    {
        get => _stream.Position;
        set => Seek(value, SeekOrigin.Begin);
    }
}