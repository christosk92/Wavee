using System.Diagnostics.Contracts;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using Eum.Spotify.connectstate;
using Eum.Spotify.context;
using Google.Protobuf;
using Spotify.Metadata;
using Wavee.Infrastructure.Live;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Playback.Cdn;
using Wavee.Spotify.Playback.SpApi;
using Wavee.Spotify.Playback.Streams;
using Wavee.Spotify.Playback.Sys;
using Wavee.Spotify.Sys.AudioKey;
using Wavee.Spotify.Sys.Common;

[assembly: InternalsVisibleTo("Wavee.Spotify.Remote")]
[assembly: InternalsVisibleTo("ConsoleTest")]

namespace Wavee.Spotify.Playback;

internal static class SpotifyPlayback<RT> where RT : struct, HasHttp<RT>
{
    public static Aff<RT, ISpotifyStream> Stream(
        TrackOrEpisode metadata,
        ContextTrack providedTrack,
        Func<SpotifyId, ByteString, CancellationToken, Aff<RT, Either<AesKeyError, ReadOnlyMemory<byte>>>>
            fetchAudioKeyFunc,
        Func<ValueTask<string>> fetchJwtFunc,
        PreferredQualityType preferredQualityType,
        CancellationToken ct) =>
        from fileId in FindFileId<RT>(metadata, preferredQualityType)
            .Map(f => f.Match(
                Some: x => x,
                None: () =>
                {
                    var restrictions = CheckRestrictions(metadata);
                    throw restrictions;
                }))
        from audioKey in fetchAudioKeyFunc(metadata.Id, fileId.FileId, ct)
            .Map(e => e.Match(
                Left: x => throw new Exception("Error fetching audio key"),
                Right: x => x))
        from cdnUrl in ResolveCdnUrl<RT>(fileId.FileId, fetchJwtFunc, ct)
        let isVorbis = fileId.Format is AudioFile.Types.Format.OggVorbis96 or AudioFile.Types.Format.OggVorbis160
            or AudioFile.Types.Format.OggVorbis320
        from encryptedStream in OpenEncryptedStream<RT>(cdnUrl.Urls.First(), metadata)
        from decryptedStream in OpenDecryptionStream<RT>(encryptedStream, audioKey, isVorbis)
        from subFile in ExtractSubFile<RT>(decryptedStream.Stream, decryptedStream.NormalisationDatas, isVorbis, fileId,
            metadata, providedTrack)
        select (ISpotifyStream)subFile;

    private static Error CheckRestrictions(TrackOrEpisode metadata)
    {
        throw new NotImplementedException();
    }

    private static Aff<RT, EncryptedSpotifyStream<RT>> OpenEncryptedStream<RT>(
        MaybeExpiringUrl cdnUrl,
        TrackOrEpisode metadata, CancellationToken ct = default)
        where RT : struct, HasHttp<RT> =>
        from firstChunk in Http<RT>.GetWithContentRange(cdnUrl.Url, 0, SpotifyPlaybackConstants.ChunkSize, ct)
        let totalSize = GetTotalSizeFromContentRange(firstChunk.Content.Headers.ContentRange)
        let numberOfChunks = CalculateNumberOfChunks(totalSize, SpotifyPlaybackConstants.ChunkSize)
        from firstChunkMemory in Aff(async () =>
        {
            ReadOnlyMemory<byte> memory = await firstChunk.Content.ReadAsByteArrayAsync(ct);
            return memory;
        })
        from rt in Eff<RT, RT>((rt) => rt)
        select new EncryptedSpotifyStream<RT>(cdnUrl, metadata, firstChunkMemory, numberOfChunks, totalSize,
            i => GetChunk(i, cdnUrl, rt, ct));

    private static async Task<ReadOnlyMemory<byte>> GetChunk<RT>(int index, MaybeExpiringUrl cdnUrl, RT hasHttp,
        CancellationToken ct) where RT : struct, HasHttp<RT>
    {
        var aff =
            from firstChunk in Http<RT>.GetWithContentRange(cdnUrl.Url, index * SpotifyPlaybackConstants.ChunkSize,
                SpotifyPlaybackConstants.ChunkSize, ct)
            from firstChunkMemory in Aff(async () =>
            {
                ReadOnlyMemory<byte> memory = await firstChunk.Content.ReadAsByteArrayAsync(ct);
                return memory;
            })
            select firstChunkMemory;

        var response = await aff.Run(hasHttp);
        //todo: error handlging on stream failure
        return response.Match(
            Succ: s => s,
            Fail: e => throw e
        );
    }

    private static long GetTotalSizeFromContentRange(ContentRangeHeaderValue contentRange)
    {
        return contentRange.Length ??
               throw new InvalidOperationException("Content-Range header is missing or invalid.");
    }

    private static int CalculateNumberOfChunks(long totalSize, int chunkSize)
    {
        return (int)Math.Ceiling((double)totalSize / chunkSize);
    }

    private static Eff<RT, (DecryptedSpotifyStream<RT> Stream, Option<NormalisationData> NormalisationDatas)>
        OpenDecryptionStream<RT>(
            EncryptedSpotifyStream<RT> encryptedSpotifyStream,
            Either<AesKeyError, ReadOnlyMemory<byte>> key, bool isVorbis)
        where RT : struct, HasHttp<RT>
    {
        var decryptionStream = new DecryptedSpotifyStream<RT>(encryptedSpotifyStream, key);
        var normData = isVorbis ? NormalisationData.ParseFromOgg(decryptionStream) : Option<NormalisationData>.None;
        return SuccessEff((decryptionStream, normData));
    }

    private static Aff<RT, CdnUrl> ResolveCdnUrl<RT>(ByteString fileId,
        Func<ValueTask<string>> fetchJwtFunc,
        CancellationToken ct = default)
        where RT : struct, HasHttp<RT> =>
        from jwt in fetchJwtFunc().ToAff()
            .Map(x => new AuthenticationHeaderValue("Bearer", x))
        from cdnUrl in SpApiRuntime<RT>.GetAudioStorage(jwt, fileId, ct)
        let urls = MaybeExpiringUrl.From(cdnUrl)
        let cdnUrlResposne = new CdnUrl(fileId, urls)
        select cdnUrlResposne;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Eff<RT, Option<AudioFile>> FindFileId<RT>(TrackOrEpisode trackOrEpisode,
        PreferredQualityType quality)
        where RT : struct
    {
        return Eff<RT, Option<AudioFile>>(_ =>
        {
            var mainFile = trackOrEpisode
                .FindFile(quality)
                .BiBind(
                    Some: Some,
                    None: () => trackOrEpisode.FindFile(PreferredQualityType.Normal))
                .Match(
                    Some: Some,
                    None: () => trackOrEpisode.FindAlternativeFile(quality)
                        .BiBind(
                            Some: Some,
                            None: () => trackOrEpisode.FindAlternativeFile(PreferredQualityType.Normal))
                        .Match(
                            Some: Some,
                            None: () => None
                        )
                );

            return mainFile;
        });
    }

    private static Eff<Subfile<RT>> ExtractSubFile<RT>(DecryptedSpotifyStream<RT> decryptedStream,
        Option<NormalisationData> normData,
        bool isVorbis,
        AudioFile chosenFile,
        TrackOrEpisode metadata,
        ContextTrack providedTrack
    )
        where RT : struct, HasHttp<RT>
    {
        return SuccessEff(new Subfile<RT>(decryptedStream,
            normData, isVorbis ? SpotifyPlaybackConstants.SPOTIFY_OGG_HEADER_END : 0,
            chosenFile,
            metadata, providedTrack));
    }
}