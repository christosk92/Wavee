using System.Security.Cryptography;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.Effects.Traits;
using LanguageExt.UnsafeValueAccess;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Wavee.Core.Contracts;
using Wavee.Core.Enums;
using Wavee.Core.Id;
using Wavee.Core.Infrastructure.Traits;
using Wavee.Spotify.Playback.Infrastructure.Key;
using Wavee.Spotify.Playback.Metadata;
using Wavee.Spotify.Playback.Playback;
using Aes = System.Runtime.Intrinsics.Arm.Aes;

namespace Wavee.Spotify.Playback.Infrastructure.Sys;

public static class SpotifyPlaybackRuntime<R> where R : struct
{
    public static Aff<R, SpotifyStream> LoadTrack<R>(string sp,
        TrackOrEpisode trackOrEpisode,
        Option<string> trackUid,
        Func<TrackOrEpisode, ITrack> mapper,
        PreferredQualityType preferredQuality,
        Func<ValueTask<string>> getBearer,
        Func<AudioId, ByteString, CancellationToken, Aff<R, Either<AesKeyError, AudioKey>>> fetchAudioKeyFunc,
        CancellationToken ct)
        where R : struct, HasAudioOutput<R>, HasWebsocket<R>, HasLog<R>, HasHttp<R>, HasDatabase<R> =>
        
}

public abstract class SpotifyStream : Stream, IAudioStream
{
    private readonly long _headerOffset;
    private readonly long _totalLength;
    private long _position;
    //private readonly Option<AesAudioDecrypt> _decrypt;

    protected SpotifyStream(
        long headerOffset,
        long totalLength,
        Option<CrossfadeController> crossfadeController,
        Option<AudioKey> audioKey)
    {
        Position = 0;
        _headerOffset = headerOffset;
        _totalLength = totalLength;
        CrossfadeController = crossfadeController;
    }

    public abstract byte[] GetChunk(int chunkIndex);

    public override int Read(byte[] buffer, int offset, int count)
    {
        return 0;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        Position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
        };
        return Position;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _totalLength - _headerOffset;

    public override long Position
    {
        get => Math.Max(0, _position - _headerOffset);
        set => _position = Math.Min(value + _headerOffset, Length);
    }

    public ITrack Track { get; }
    public Option<string> Uid { get; }

    public Stream AsStream()
    {
        return this;
    }

    public Option<CrossfadeController> CrossfadeController { get; }


    // private sealed class AesAudioDecrypt
    // {
    //     private static byte[] AUDIO_AES_IV = new byte[]
    //     {
    //         0x72, 0xe0, 0x67, 0xfb, 0xdd, 0xcb, 0xcf, 0x77, 0xeb, 0xe8, 0xbc, 0x64, 0x3f, 0x63, 0x0d, 0x93,
    //     };
    //
    //     private static BigInteger IvInt = new BigInteger(1, AUDIO_AES_IV);
    //     private static readonly BigInteger IvDiff = BigInteger.ValueOf(0x100);
    //     private readonly IBufferedCipher _cipher;
    //     private readonly KeyParameter _spec;
    //
    //     public AesAudioDecrypt(ReadOnlyMemory<byte> key)
    //     {
    //         _spec = ParameterUtilities.CreateKeyParameter("AES", key.ToArray());
    //         _cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
    //     }
    //
    //     public Unit Decrypt(byte[] buf, int chunkIndex)
    //     {
    //         var iv = IvInt.Add(
    //             BigInteger.ValueOf(SpotifyPlaybackConstants.ChunkSize * chunkIndex / 16));
    //         for (var i = 0; i < buf.Length; i += 4096)
    //         {
    //             _cipher.Init(true, new ParametersWithIV(_spec, iv.ToByteArray()));
    //
    //             var count = Math.Min(4096, buf.Length - i);
    //
    //             var processed = _cipher.DoFinal(buf,
    //                 i,
    //                 count,
    //                 buf, i);
    //             if (count != processed)
    //                 throw new IOException(string.Format("Couldn't process all data, actual: %d, expected: %d",
    //                     processed, count));
    //
    //             iv = iv.Add(IvDiff);
    //         }
    //
    //         return unit;
    //     }
    //
    //     public Unit Seek(long to, SeekOrigin origin)
    //     {
    //         throw new NotImplementedException();
    //     }
    // }
}

internal sealed class Aes128CtrAudioDecrypt
{
    private AesManaged aes;
    private ICryptoTransform decryptor;
    private CryptoStream cs;

    public Aes128CtrAudioDecrypt(byte[] key, byte[] iv)
    {
        aes = new AesManaged();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;

        decryptor = aes.CreateDecryptor(key, iv);
    }

    public byte[] Decrypt(byte[] cipher)
    {
        using var ms = new MemoryStream();
        cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write);
        cs.Write(cipher, 0, cipher.Length);
        cs.Close();
        return ms.ToArray();
    }

    public void Seek(long offset, SeekOrigin origin)
    {
        cs.FlushFinalBlock();
        cs.Seek(offset, origin);
    }
}