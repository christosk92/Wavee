using LanguageExt.UnsafeValueAccess;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Clients.Mercury.Key;

namespace Wavee.Spotify.Clients.Playback.Streams;

internal sealed class DecryptedSpotifyStream<RT> : IDisposable where RT : struct, HasHttp<RT>
{
    private readonly EncryptedSpotifyStream<RT> _encryptedSpotifyStream;
    private readonly Option<IAudioDecrypt> _audioDecrypt;
    private readonly Option<ReadOnlyMemory<byte>>[] _decryptedChunks;
    private long _position;

    public DecryptedSpotifyStream(EncryptedSpotifyStream<RT> encryptedSpotifyStream,
        Either<AesKeyError, AudioKey> key)
    {
        _encryptedSpotifyStream = encryptedSpotifyStream;
        _decryptedChunks = new Option<ReadOnlyMemory<byte>>[encryptedSpotifyStream.NumberOfChunks];
        _audioDecrypt = key.Match(
            Left: _ => Option<IAudioDecrypt>.None,
            Right: x => Option<IAudioDecrypt>.Some(new AesAudioDecrypt(x.Key))
        );
    }

    public long Position
    {
        get
        {
            return _position;
        }
        set
        {
            _position = value;
            _encryptedSpotifyStream.Position = value;
        }
    }

    public long Length => _encryptedSpotifyStream.Length;

    public long Seek(long to, SeekOrigin begin)
    {
        Position = to;
        return _encryptedSpotifyStream.Seek(to, begin);
    }

    public int Read(Span<byte> buf)
    {
        //we can only decrypt whole chunks so we need to read the whole chunk and then decrypt it and copy it to the buffer

        //check to see which chunk we are in
        const int chunkSize = SpotifyPlaybackConstants.ChunkSize;
        var prevPos = _encryptedSpotifyStream.Position;
        var chunkIndex = (int)(Position / chunkSize);
        var chunkOffset = (int)(Position % chunkSize);

        bool wasNone = false;
        if (_decryptedChunks[chunkIndex].IsNone)
        {
            wasNone = true;
            _encryptedSpotifyStream.Seek(chunkIndex * chunkSize, SeekOrigin.Begin);

            //read chunk
            var chunk = new byte[chunkSize];
            var read = _encryptedSpotifyStream.Read(chunk);
            //decrypt
            if (_audioDecrypt.IsSome)   
            {
                _audioDecrypt.ValueUnsafe().Decrypt(chunk, chunkIndex);
            }

            _decryptedChunks[chunkIndex] = (ReadOnlyMemory<byte>)chunk.AsMemory();

            //seek back
            // _encryptedSpotifyStream.Seek(prevPos + len, SeekOrigin.Begin);
        }

        var chunkFinal = _decryptedChunks[chunkIndex].ValueUnsafe();
        //copy to buffer
        var len = Math.Min(buf.Length, chunkFinal.Length - chunkOffset);
        chunkFinal.Span.Slice(chunkOffset, len)
            .CopyTo(buf);
        Position += len;
        return len;
    }

    public void Dispose()
    {
        _encryptedSpotifyStream.Dispose();
        //clear decrypted chunks
        for (var i = 0; i < _decryptedChunks.Length; i++)
        {
            _decryptedChunks[i] = Option<ReadOnlyMemory<byte>>.None;
        }
    }
}

internal interface IAudioDecrypt
{
    Unit Decrypt(byte[] buf, int chunkIndex);
    Unit Seek(long to, SeekOrigin origin);
}

internal sealed class AesAudioDecrypt : IAudioDecrypt
{
    private static byte[] AUDIO_AES_IV = new byte[]
    {
        0x72, 0xe0, 0x67, 0xfb, 0xdd, 0xcb, 0xcf, 0x77, 0xeb, 0xe8, 0xbc, 0x64, 0x3f, 0x63, 0x0d, 0x93,
    };

    private static BigInteger IvInt = new BigInteger(1, AUDIO_AES_IV);
    private static readonly BigInteger IvDiff = BigInteger.ValueOf(0x100);
    private readonly IBufferedCipher _cipher;
    private readonly KeyParameter _spec;

    public AesAudioDecrypt(ReadOnlyMemory<byte> key)
    {
        _spec = ParameterUtilities.CreateKeyParameter("AES", key.ToArray());
        _cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
    }

    public Unit Decrypt(byte[] buf, int chunkIndex)
    {
        var iv = IvInt.Add(
            BigInteger.ValueOf(SpotifyPlaybackConstants.ChunkSize * chunkIndex / 16));
        for (var i = 0; i < buf.Length; i += 4096)
        {
            _cipher.Init(true, new ParametersWithIV(_spec, iv.ToByteArray()));

            var count = Math.Min(4096, buf.Length - i);

            var processed = _cipher.DoFinal(buf,
                i,
                count,
                buf, i);
            if (count != processed)
                throw new IOException(string.Format("Couldn't process all data, actual: %d, expected: %d",
                    processed, count));

            iv = iv.Add(IvDiff);
        }

        return unit;
    }

    public Unit Seek(long to, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }
}