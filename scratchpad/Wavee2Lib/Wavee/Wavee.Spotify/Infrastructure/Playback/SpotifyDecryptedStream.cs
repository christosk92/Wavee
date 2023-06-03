using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Spotify.Metadata;

namespace Wavee.Spotify.Infrastructure.Playback;

internal sealed class AesDecryptor
{
    private readonly IBufferedCipher _cipher;
    private readonly KeyParameter _spec;
    private static BigInteger IvInt;
    private readonly int _chunkSize;
    private static readonly BigInteger IvDiff = BigInteger.ValueOf(0x100);

    public AesDecryptor(byte[] key, byte[] iv, int chunkSize)
    {
        _chunkSize = chunkSize;
        IvInt = new BigInteger(1, iv);
        _spec = ParameterUtilities.CreateKeyParameter("AES", key);
        _cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
    }

    public void Decrypt(int chunkIndex, byte[] chunk)
    {
        var iv = IvInt.Add(
            BigInteger.ValueOf(_chunkSize * chunkIndex / 16));
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
}

public sealed class SpotifyDecryptedStream : Stream
{
    internal const int ChunkSize = 2 * 2 * 128 * 1024;

    public static byte[] AUDIO_AES_IV = new byte[]
    {
        0x72, 0xe0, 0x67, 0xfb, 0xdd, 0xcb, 0xcf, 0x77, 0xeb, 0xe8, 0xbc, 0x64, 0x3f, 0x63, 0x0d, 0x93,
    };

    private long _position;

    private readonly Dictionary<int, byte[]> _decryptedChunks = new();
    private readonly AudioFile _chosenFormat;
    private readonly AesDecryptor _decryptor;
    private Func<int, ValueTask<byte[]>> _getChunkFunc;
    private readonly long _offset;
    private readonly long _length;

    public SpotifyDecryptedStream(
        Func<int, ValueTask<byte[]>> getChunkFunc,
        long length,
        ReadOnlyMemory<byte> decryptionKey,
        AudioFile format)
    {
        _position = 0;
        _chosenFormat = format;
        _length = length;
        _getChunkFunc = getChunkFunc;
        _offset = format.Format switch
        {
            AudioFile.Types.Format.OggVorbis96 or AudioFile.Types.Format.OggVorbis160
                or AudioFile.Types.Format.OggVorbis320 => 0xa7,
            _ => 0
        };
        _decryptor = new AesDecryptor(
            key: decryptionKey.ToArray(),
            iv: AUDIO_AES_IV,
            chunkSize: ChunkSize);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        //Since random seeking may not be supported in the stream,
        //we can only fetch chunks 
        var chunkIndex = (int)(_position / ChunkSize);
        var chunkOffset = (int)(_position % ChunkSize);

        if (!_decryptedChunks.TryGetValue(chunkIndex, out var chunk))
        {
            chunk = _getChunkFunc(chunkIndex).Result;
            //preload ahead 2 chunks
            Task.Run(async () => { await _getChunkFunc(chunkIndex + 1); });
            Task.Run(async () => { await _getChunkFunc(chunkIndex + 2); });
            _decryptor.Decrypt(chunkIndex, chunk);
            _decryptedChunks.Add(chunkIndex, chunk);
        }

        var bytesToRead = Math.Max(0, Math.Min(count, chunk.Length - chunkOffset));
        Array.Copy(chunk, chunkOffset, buffer, offset, bytesToRead);
        _position += bytesToRead;
        return bytesToRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        _position = origin switch
        {
            SeekOrigin.Begin => offset + _offset,
            SeekOrigin.Current => _position + offset + _offset,
            SeekOrigin.End => offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
        };

        if (origin is SeekOrigin.End)
            return _position;
        return Math.Max(0, _position - _offset);
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _length;

    public override long Position
    {
        get => Math.Max(0, _position - _offset);
        set => _position = value + _offset;
    }

    public override void Flush()
    {
        throw new NotImplementedException();
    }
}