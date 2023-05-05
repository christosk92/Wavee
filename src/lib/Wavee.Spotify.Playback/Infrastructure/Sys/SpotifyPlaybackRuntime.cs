using System.Diagnostics.Contracts;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using Google.Protobuf;
using LanguageExt.Effects.Traits;
using LanguageExt.UnsafeValueAccess;
using Spotify.Metadata;
using Wavee.Common;
using Wavee.Infrastructure.Live;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Common;
using Wavee.Spotify.Infrastructure.Sys;
using Wavee.Spotify.Playback.Cdn;
using Wavee.Spotify.Playback.Infrastructure.Streams;
using Wavee.Spotify.Playback.Normalisation;

namespace Wavee.Spotify.Playback.Infrastructure.Sys;

public static class SpotifyPlaybackRuntime
{
    internal const int ChunkSize = 2 * 2 * 128 * 1024;
    internal const ulong SPOTIFY_OGG_HEADER_END = 0xa7;

    public static async ValueTask<ISpotifyStream> StreamAudio(this ISpotifyClient client, SpotifyId itemId,
        SpotifyPlaybackConfig config)
    {
        var runtime = WaveeCore.Runtime;

        var result = await StreamAudio<WaveeRuntime>(client, itemId, config).Run(runtime);

        return result.Match(
            Succ: s => s,
            Fail: e => throw e
        );
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Aff<RT, ISpotifyStream> StreamAudio<RT>(ISpotifyClient client, SpotifyId itemId,
        SpotifyPlaybackConfig config)
        where RT : struct, HasHttp<RT> =>
        from token in cancelToken<RT>()
        from metadata in FetchMetadata<RT>(client, itemId, token)
        from fileId in FindFileId<RT>(metadata, config.PreferredQuality)
            .Map(f => f.Match(
                Some: x => x,
                None: () =>
                {
                    var restrictions = CheckRestrictions(metadata);
                    throw restrictions;
                }))
        from audioKey in FetchAudioKey<RT>(client, itemId, fileId.FileId, token)
        from cdnUrl in ResolveCdnUrl<RT>(client, itemId, fileId.FileId, token)
        let isVorbis = fileId.Format is AudioFile.Types.Format.OggVorbis96 or AudioFile.Types.Format.OggVorbis160
            or AudioFile.Types.Format.OggVorbis320
        from encryptedStream in OpenEncryptedStream<RT>(cdnUrl.Urls.First(), metadata)
        from decryptedStream in OpenDecryptionStream<RT>(encryptedStream, audioKey, isVorbis)
        from subFile in ExtractSubFile<RT>(decryptedStream.Stream, decryptedStream.NormalisationDatas, isVorbis, fileId,
            metadata)
        select (ISpotifyStream)subFile;

    private static Error CheckRestrictions(TrackOrEpisode metadata)
    {
        throw new NotImplementedException();
    }

    private static Eff<Subfile<RT>> ExtractSubFile<RT>(DecryptedSpotifyStream<RT> decryptedStream,
        Option<NormalisationData> normData,
        bool isVorbis,
        AudioFile chosenFile,
        TrackOrEpisode metadata
    )
        where RT : struct, HasHttp<RT>
    {
        return SuccessEff(new Subfile<RT>(decryptedStream, normData, isVorbis ? SPOTIFY_OGG_HEADER_END : 0, chosenFile,
            metadata));
    }

    private static Aff<RT, EncryptedSpotifyStream<RT>> OpenEncryptedStream<RT>(
        MaybeExpiringUrl cdnUrl,
        TrackOrEpisode metadata, CancellationToken ct = default)
        where RT : struct, HasHttp<RT> =>
        from firstChunk in Http<RT>.GetWithContentRange(cdnUrl.Url, 0, ChunkSize, ct)
        let totalSize = GetTotalSizeFromContentRange(firstChunk.Content.Headers.ContentRange)
        let numberOfChunks = CalculateNumberOfChunks(totalSize, ChunkSize)
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
            from firstChunk in Http<RT>.GetWithContentRange(cdnUrl.Url, index * ChunkSize, ChunkSize, ct)
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

    private static Aff<RT, CdnUrl> ResolveCdnUrl<RT>(ISpotifyClient client, SpotifyId itemId, ByteString fileId,
        CancellationToken ct = default)
        where RT : struct, HasHttp<RT>
    {
        var cdnUrl = new CdnUrl(fileId, Empty);
        return CdnUrlFunctions.ResolveAudio(client, cdnUrl, ct).ToAff();
    }


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Aff<Either<AesKeyError, ReadOnlyMemory<byte>>> FetchAudioKey<RT>(ISpotifyClient client,
        SpotifyId itemId,
        ByteString fileId, CancellationToken ct)
        where RT : struct, HasCancel<RT> =>
        from audioKey in client.AudioKeys.GetAudioKey(itemId, fileId, ct).ToAff()
        select audioKey;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Aff<RT, TrackOrEpisode> FetchMetadata<RT>(ISpotifyClient client, SpotifyId itemId,
        CancellationToken ct)
        where RT : struct, HasCancel<RT> =>
        from mercuryResponse in (itemId.Type switch
        {
            AudioItemType.Track => client.Mercury.GetTrack(itemId.ToHex(), ct).ToAff()
                .Map(x => new TrackOrEpisode(Right(x))),
            AudioItemType.Episode => client.Mercury.GetEpisode(itemId.ToHex(), ct).ToAff()
                .Map(x => new TrackOrEpisode(Left(x))),
            _ => throw new NotImplementedException()
        })
        select mercuryResponse;


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
                    None: () => trackOrEpisode.FindFile(PreferredQualityType.Default))
                .Match(
                    Some: Some,
                    None: () => trackOrEpisode.FindAlternativeFile(quality)
                        .BiBind(
                            Some: Some,
                            None: () => trackOrEpisode.FindAlternativeFile(PreferredQualityType.Default))
                        .Match(
                            Some: Some,
                            None: () => None
                        )
                );

            return mainFile;
        });
    }
}

public readonly record struct TrackOrEpisode(Either<Episode, Track> Value)
{
    static TrackOrEpisode()
    {
        FormatsMap = new HashMap<PreferredQualityType, AudioFile.Types.Format[]>(new[]
        {
            (PreferredQualityType.Low, new[]
            {
                AudioFile.Types.Format.OggVorbis96,
                AudioFile.Types.Format.Mp396,
                AudioFile.Types.Format.Mp3160
            }),
            (PreferredQualityType.Default, new[]
            {
                AudioFile.Types.Format.OggVorbis160,
                AudioFile.Types.Format.Mp3160,

                AudioFile.Types.Format.OggVorbis320,
                AudioFile.Types.Format.Mp3256,

                AudioFile.Types.Format.Aac48,
                AudioFile.Types.Format.FlacFlac,

                AudioFile.Types.Format.Mp396,
                AudioFile.Types.Format.OggVorbis96,
            }),
            (PreferredQualityType.High, new[]
            {
                AudioFile.Types.Format.OggVorbis320,
                AudioFile.Types.Format.Mp3320
            }),
            (PreferredQualityType.Highest, new[]
            {
                AudioFile.Types.Format.OggVorbis320,
                AudioFile.Types.Format.Mp3256,
                AudioFile.Types.Format.Aac48,
                AudioFile.Types.Format.FlacFlac
            })
        });
    }

    public Option<AudioFile> FindFile(PreferredQualityType quality)
    {
        return Value.Match(
            Left: e =>
            {
                return e.Audio
                    .Find(f =>
                    {
                        var r = FormatsMap.Find(quality).Map(x => x.Contains(f.Format));
                        return r.Match(
                            Some: t => t,
                            None: () => false
                        );
                    });
            },
            Right: t =>
            {
                return t.File
                    .Find(f =>
                    {
                        var r = FormatsMap.Find(quality).Map(x => x.Contains(f.Format));
                        return r.Match(
                            Some: t => t,
                            None: () => false
                        );
                    });
            }
        );
        // var track = Track;
        // var episode = Episode;
        // return FormatsMap
        //     .Find(quality)
        //     .Bind(formats => track.Match(
        //         Some: t => t.File.Find(f => formats.Contains(f.Format)),
        //         None: () => episode.Match(
        //             Some: e => e.Audio.Find(f => formats.Contains(f.Format)),
        //             None: () => None
        //         )
        //     ));
    }

    public Option<AudioFile> FindAlternativeFile(PreferredQualityType quality)
    {
        throw new NotImplementedException();
    }

    private static HashMap<PreferredQualityType, AudioFile.Types.Format[]> FormatsMap { get; }
}