using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using AesCtr;
using AesCtrBouncyCastle;
using Eum.Spotify.storage;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.Effects.Traits;
using LanguageExt.UnsafeValueAccess;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Spotify.Metadata;
using Wavee.Core.Contracts;
using Wavee.Core.Enums;
using Wavee.Core.Id;
using Wavee.Core.Infrastructure.Sys.IO;
using Wavee.Core.Infrastructure.Traits;
using Wavee.Spotify.Playback.Infrastructure.Key;
using Wavee.Spotify.Playback.Metadata;
using Wavee.Spotify.Playback.Playback;
using Wavee.Spotify.Playback.Playback.Cdn;
using Wavee.Spotify.Playback.Playback.Streams;
using Aes = System.Runtime.Intrinsics.Arm.Aes;

namespace Wavee.Spotify.Playback.Infrastructure.Sys;

public static class SpotifyPlaybackRuntime<R> where R : struct, HasHttp<R>
{
    /// <summary>
    /// The minimum size of a block that is requested from the Spotify servers in one request.
    /// This is the block size that is typically requested while doing a `seek()` on a file.
    /// Note: smaller requests can happen if part of the block is downloaded already.
    /// </summary>
    private const int MINIMUM_DOWNLOAD_SIZE = 64 * 1024;

    public static Aff<R, SpotifyStream> LoadTrack(string sp,
        TrackOrEpisode trackOrEpisode,
        Func<TrackOrEpisode, ITrack> mapper,
        PreferredQualityType preferredQuality,
        Func<ValueTask<string>> getBearer,
        Func<AudioId, ByteString, CancellationToken, Aff<R, Either<AesKeyError, AudioKey>>> fetchAudioKeyFunc,
        CancellationToken ct) =>
        from audioFile in Eff(() => trackOrEpisode.FindFile(preferredQuality).Match(
            Some: x => x,
            None: () => trackOrEpisode.FindAlternativeFile(preferredQuality)
                .Match(
                    Some: f => f,
                    None: () => throw new Exception("No audio file found")
                )
        ))
        from audioKey in fetchAudioKeyFunc(trackOrEpisode.Id, audioFile.FileId, ct).Map(x => x.Match(
            Left: err => None,
            Right: key => Some(key)
        ))
        //TODO: Cache
        from stream in OpenHttpEncryptedStream(getBearer, sp, audioFile.FileId, ct)
        from decryptedStream in OpenDecryptedStream(stream, audioKey)
        from offsetAndNormData in ReadNormalisationData(decryptedStream, audioFile.Format)
        select new SpotifyStream(decryptedStream, mapper(trackOrEpisode), offsetAndNormData.Item1,
            offsetAndNormData.Item2, stream.Length, None);

    private static Eff<(Option<NormalisationData> Normdata, long Offset)> ReadNormalisationData(Stream stream,
        AudioFile.Types.Format format)
    {
        var isOggVorbis = format == AudioFile.Types.Format.OggVorbis96 ||
                          format == AudioFile.Types.Format.OggVorbis160 ||
                          format == AudioFile.Types.Format.OggVorbis320;

        if (!isOggVorbis) return SuccessEff((Option<NormalisationData>.None, 0L));
        return Eff(() =>
        {
            var normData = NormalisationData.ParseFromOgg(stream);
            const ulong offset = SpotifyPlaybackConstants.SPOTIFY_OGG_HEADER_END;
            return (normData, (long)offset);
        });
    }

    private static Eff<AesCtrBouncyCastleStream> OpenDecryptedStream(Stream stream, Option<AudioKey> audioKey) =>
        Eff(() =>
        {
            var aes128 = new AesCtrBouncyCastleStream(stream, audioKey.ValueUnsafe().Key.ToArray(),
                SpotifyPlaybackConstants.AUDIO_AES_IV, SpotifyPlaybackConstants.ChunkSize);
            return aes128;
            // var aes128 = new Aes128CtrStream(stream, audioKey.ValueUnsafe().Key.ToArray(),
            //     SpotifyPlaybackConstants.AUDIO_AES_IV);
            // return new Aes128CtrWrapperStream(aes128);
        });

    private static Aff<R, HttpEncryptedSpotifyStream<R>> OpenHttpEncryptedStream(Func<ValueTask<string>> getBearer,
        string spClientUrl,
        ByteString fileId,
        CancellationToken ct) =>
        from base16Id in SuccessEff(ToBase16(fileId.Span))
        from bearer in getBearer().ToAff()
            .Map(x => new AuthenticationHeaderValue("Bearer", x))
        from storage in Http<R>.Get($"{spClientUrl}/storage-resolve/files/audio/interactive/{base16Id}", bearer,
                Option<HashMap<string, string>>.None, ct)
            .MapAsync(async x =>
            {
                await using var stream = await x.Content.ReadAsStreamAsync(ct);
                return StorageResolveResponse.Parser.ParseFrom(stream);
            })
        from cdnUrls in GetCdnUrls(fileId, storage)
        from totalLength in GetTotalLength(cdnUrls, ct)
        select new HttpEncryptedSpotifyStream<R>(cdnUrls, MINIMUM_DOWNLOAD_SIZE, totalLength);
        

    private static Aff<R, long> GetTotalLength(string url, CancellationToken ct = default) =>
        Http<R>.Head(url, Option<HashMap<string, string>>.None, ct)
            .Map(x => x.Content.Headers.ContentLength.Value);
    private static Eff<string> GetCdnUrls(ByteString fileId, StorageResolveResponse storage) => Eff(() =>
    {
        var maybeExpiring = MaybeExpiringUrl.From(storage);
        return new CdnUrl(fileId, maybeExpiring).Urls.First().Url;
    });

    private static string ToBase16(ReadOnlySpan<byte> raw)
    {
        //convert to hex
        var hex = new StringBuilder(raw.Length * 2);
        foreach (var b in raw)
        {
            hex.AppendFormat("{0:x2}", b);
        }

        return hex.ToString();
    }
}