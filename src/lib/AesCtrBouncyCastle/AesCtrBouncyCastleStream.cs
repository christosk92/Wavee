using System.Diagnostics;
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
    private long _position;

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
        var prevPos = _stream.Position;
        var chunkIndex = (int)(Position / chunk_size);
        var chunkOffset = (int)(Position % chunk_size);

        bool wasNone = false;
        if (!_cache.TryGetValue(chunkIndex, out var cachedChunk))
        {
            wasNone = true;
            _stream.Position = chunkIndex * chunk_size;

            var newChunk = new byte[chunk_size];
            _stream.Read(newChunk);
            Decrypt(chunkIndex, newChunk);

            cachedChunk = newChunk;
            _cache.Add(chunkIndex, newChunk);
        }

        var len = Math.Min(count, cachedChunk.Length - chunkOffset);
        Array.Copy(cachedChunk.ToArray(), chunkOffset, buffer, offset, len);
        Position += len;
        return len;
    }

    private void Decrypt(int chunkIndex, byte[] chunk)
    {
        var iv = IvInt.Add(
            BigInteger.ValueOf(chunk_size * chunkIndex / 16));
        for (var i = 0; i < chunk.Length; i += 4096)
        {
            _cipher.Init(true, new ParametersWithIV(_spec, iv.ToByteArray()));

            var c = Math.Min(4096, chunk.Length - i);
            var processed = _cipher.DoFinal(chunk,
                i,
                c,
                chunk, i);
            if (c != processed)
                throw new IOException(string.Format("Couldn't process all data, actual: %d, expected: %d",
                    processed, c));

            iv = iv.Add(IvDiff);
        }
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
        get { return _position; }
        set
        {
            _position = value;
            _stream.Position = value;
        }
    }
}