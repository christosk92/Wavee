using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace AesCtrBouncyCastle;

public sealed class AesCtrBouncyCastleStream : Stream
{
    private readonly Dictionary<int, ReadOnlyMemory<byte>> _cache = new();
    private readonly IBufferedCipher _cipher;
    private readonly KeyParameter _spec;
    private static BigInteger IvInt;
    private static readonly BigInteger IvDiff = BigInteger.ValueOf(0x100);
    private readonly Stream _stream;
    private int chunk_size;
    public AesCtrBouncyCastleStream(Stream stream, byte[] key, byte[] iv, int chunkSize)
    {
        _stream = stream;
        chunk_size = chunkSize;
        IvInt = new BigInteger(1, iv);
        _spec = ParameterUtilities.CreateKeyParameter("AES", key);
        _cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
    }
    
    public override int Read(byte[] buffer, int offset, int count)
    {
        //we can only decrypt whole chunks at once
        var chunkIndex = (int)(_stream.Position / chunk_size);
        var chunkOffset = (int)(_stream.Position % chunk_size);

        if(_cache.TryGetValue(chunkIndex, out var cachedChunk))
        {
            //we have the chunk in cache, copy it to the buffer
            var copy = Math.Min(cachedChunk.Length - chunkOffset, count);
            cachedChunk.Span.Slice(chunkOffset, copy).CopyTo(buffer.AsSpan(offset, copy));
            _stream.Position += copy;
            return copy;
        }
        
        var iv = IvInt.Add(
            BigInteger.ValueOf(chunk_size * chunkIndex / 16));
        
        //read whole chunk into buffer
        var tempBuffer = new byte[chunk_size];
        var positionBeforeRead = _stream.Position;
        var bytesRead = _stream.Read(tempBuffer, offset, chunk_size);
        
        for (var i = 0; i < tempBuffer.Length; i += 4096)
        {
            _cipher.Init(true, new ParametersWithIV(_spec, iv.ToByteArray()));

            var c = Math.Min(4096, tempBuffer.Length - i);
            var processed = _cipher.DoFinal(tempBuffer,
                i,
                c,
                tempBuffer, i);
            if (c != processed)
                throw new IOException(string.Format("Couldn't process all data, actual: %d, expected: %d",
                    processed, c));

            iv = iv.Add(IvDiff);
        }
        
        //copy the decrypted chunk to the buffer
        var copySize = Math.Min(bytesRead, count);
        tempBuffer.AsSpan(chunkOffset, copySize).CopyTo(buffer.AsSpan(offset, copySize));
        _cache.Add(chunkIndex, tempBuffer);
        //restore stream position
        _stream.Position = positionBeforeRead + copySize;
        return copySize;
    }

    public override void Flush()
    {
        _stream.Flush();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _stream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _stream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _stream.Write(buffer, offset, count);
    }

    public override bool CanRead => _stream.CanRead;
    public override bool CanSeek => _stream.CanSeek;
    public override bool CanWrite => _stream.CanWrite;
    public override long Length => _stream.Length;

    public override long Position
    {
        get => _stream.Position;
        set => _stream.Position = value;
    }
}