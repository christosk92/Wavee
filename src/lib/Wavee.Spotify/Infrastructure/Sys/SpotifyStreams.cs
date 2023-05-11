using System.Net.Http.Headers;
using Eum.Spotify.context;
using Google.Protobuf;
using LanguageExt.Common;
using Spotify.Metadata;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Cache;
using Wavee.Spotify.Cache.Domain.Chunks;
using Wavee.Spotify.Cache.Repositories;
using Wavee.Spotify.Clients.Mercury.Key;
using Wavee.Spotify.Clients.Mercury.Metadata;
using Wavee.Spotify.Clients.Playback;
using Wavee.Spotify.Clients.Playback.Cdn;
using Wavee.Spotify.Clients.Playback.Streams;

namespace Wavee.Spotify.Infrastructure.Sys;

internal static class SpotifyStreams<RT> where RT : struct, HasHttp<RT>, HasTrackRepo<RT>, HasFileRepo<RT>
{
    public static Aff<RT, IEncryptedSpotifyStream> OpenEncryptedFileStream(EncryptedAudioFile cachedFile,
        TrackOrEpisode metadata)
    {
        var totalSize = cachedFile.Data.Length;
        var numberOfChunks = CalculateNumberOfChunks(totalSize, SpotifyPlaybackConstants.ChunkSize);
        return SuccessAff(
            (IEncryptedSpotifyStream)new EncryptedSpotifyStream<RT>(metadata, cachedFile.Data,
                numberOfChunks, totalSize,
                i => GetChunkFromMemory(i, cachedFile),
                Option<Action<ReadOnlyMemory<byte>>>.None));
    }

    private static Task<ReadOnlyMemory<byte>> GetChunkFromMemory(int index, EncryptedAudioFile cachedFile)
    {
        var chunkStart = index * SpotifyPlaybackConstants.ChunkSize;
        var chunkEnd = Math.Min(chunkStart + SpotifyPlaybackConstants.ChunkSize, cachedFile.Data.Length);
        var chunkLength = chunkEnd - chunkStart;
        var chunk = cachedFile.Data.Slice(chunkStart, chunkLength);
        return Task.FromResult(chunk);
    }

    public static Aff<RT, IEncryptedSpotifyStream> OpenEncryptedStream(
        AudioFile file,
        MaybeExpiringUrl cdnUrl,
        TrackOrEpisode metadata, CancellationToken ct = default) =>
        from firstChunk in Http<RT>.GetWithContentRange(cdnUrl.Url, 0, SpotifyPlaybackConstants.ChunkSize, ct)
        let totalSize = GetTotalSizeFromContentRange(firstChunk.Content.Headers.ContentRange)
        let numberOfChunks = CalculateNumberOfChunks(totalSize, SpotifyPlaybackConstants.ChunkSize)
        from firstChunkMemory in Aff(async () =>
        {
            ReadOnlyMemory<byte> memory = await firstChunk.Content.ReadAsByteArrayAsync(ct);
            return memory;
        })
        from rt in Eff<RT, RT>((rt) => rt)
        select (IEncryptedSpotifyStream)new EncryptedSpotifyStream<RT>(metadata, firstChunkMemory,
            numberOfChunks, totalSize,
            i => GetChunk(i, cdnUrl, rt, ct),
            new Action<ReadOnlyMemory<byte>>(r => FinishedDownloading(file, r, rt)));

    private static async void FinishedDownloading(
        AudioFile file,
        ReadOnlyMemory<byte> fileData, RT runtime)
    {
        var aff = SpotifyCache<RT>.CacheFile(file, fileData);
        _ = await aff.Run(runtime);
    }

    public static Eff<RT, (DecryptedSpotifyStream<RT> Stream, Option<NormalisationData> NormalisationDatas)>
        OpenDecryptionStream(
            IEncryptedSpotifyStream encryptedSpotifyStream,
            Either<AesKeyError, AudioKey> key, bool isVorbis)
    {
        var decryptionStream = new DecryptedSpotifyStream<RT>(encryptedSpotifyStream, key);
        var normData = isVorbis ? NormalisationData.ParseFromOgg(decryptionStream) : Option<NormalisationData>.None;
        return SuccessEff((decryptionStream, normData));
    }

    public static Eff<SpotifyStream<RT>> ExtractFinalStream(DecryptedSpotifyStream<RT> decryptedStream,
        Option<NormalisationData> normData,
        bool isVorbis,
        AudioFile chosenFile,
        TrackOrEpisode metadata)
    {
        return SuccessEff(new SpotifyStream<RT>(decryptedStream,
            normData, isVorbis ? SpotifyPlaybackConstants.SPOTIFY_OGG_HEADER_END : 0,
            chosenFile,
            metadata));
    }

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
}