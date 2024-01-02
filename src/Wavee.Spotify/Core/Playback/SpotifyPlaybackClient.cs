using System.Collections.Immutable;
using Spotify.Metadata;
using Wavee.Core.Enums;
using Wavee.Interfaces;
using Wavee.Spotify.Core.Exceptions;
using Wavee.Spotify.Core.Mappings;
using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Core.Models.Metadata;
using Wavee.Spotify.Core.Models.Track;
using Wavee.Spotify.Infrastructure.Playback;
using Wavee.Spotify.Interfaces.Clients;
using Wavee.Spotify.Interfaces.Clients.Playback;
using Wavee.Spotify.Interfaces.Models;
using static Spotify.Metadata.AudioFile.Types.Format;

namespace Wavee.Spotify.Core.Playback;

internal sealed class SpotifyPlaybackClient : ISpotifyPlaybackClient
{
    private readonly ISpotifyMetadataClient _metadataClient;
    private readonly ISpotifyStorageResolveService _storageResolveService;
    private readonly ISpotifyAudioKeyService _audioKeysService;
    private readonly IWaveeCachingProvider _cache;
    private readonly WaveeSpotifyConfig _config;

    public SpotifyPlaybackClient(ISpotifyMetadataClient metadataClient,
        ISpotifyStorageResolveService storageResolveService,
        ISpotifyAudioKeyService audioKeysService,
        IWaveeCachingProvider cache,
        WaveeSpotifyConfig config)
    {
        _metadataClient = metadataClient;
        _storageResolveService = storageResolveService;
        _audioKeysService = audioKeysService;
        _cache = cache;
        _config = config;
    }

    public ValueTask<SpotifyAudioStream> CreateStream(SpotifyId id, CancellationToken cancellationToken = default)
    {
        var trackOrEpisode = GetTrackOrEpisodeThrow(id, cancellationToken);
        if (trackOrEpisode.IsCompletedSuccessfully)
        {
            return CreateStream(trackOrEpisode.Result, cancellationToken);
        }

        return new ValueTask<SpotifyAudioStream>(CreateStreamAsync(trackOrEpisode, cancellationToken));
    }

    private async Task<SpotifyAudioStream> CreateStreamAsync(ValueTask<ISpotifyPlayableItem> trackOrEpisode,
        CancellationToken cancellationToken)
    {
        var trackOrEpisodeResult = await trackOrEpisode;
        return await CreateStream(trackOrEpisodeResult, cancellationToken);
    }

    private ValueTask<SpotifyAudioStream> CreateStream(ISpotifyPlayableItem result, CancellationToken cancellationToken)
    {
        switch (result)
        {
            case SpotifySimpleTrack track:
                return CreateTrackStream(track, cancellationToken);
            case SpotifySimpleEpisode episode:
                return CreateEpisodeStream(episode, cancellationToken);
            default:
                throw new SpotifyCannotPlayContentException(SpotifyCannotPlayReason.NotTrackOrEpisode);
        }
    }

    private ValueTask<SpotifyAudioStream> CreateEpisodeStream(SpotifySimpleEpisode episode,
        CancellationToken cancellationToken)
    {
        throw new SpotifyCannotPlayContentException(SpotifyCannotPlayReason.NotYetImplemented);
    }

    private ValueTask<SpotifyAudioStream> CreateTrackStream(SpotifySimpleTrack track,
        CancellationToken cancellationToken)
    {
        var preferedQuality = _config.Playback.PreferedQuality;
        var file = FindFile(track.AudioFiles, preferedQuality, true);

        const string bucket = "tracks";
        var audioKeycacheKey = $"spotify:audiokey:{file.FileIdBase16}";
        if (_cache.TryGetFile(bucket, file.FileIdBase16, out var fileStream)
            && _cache.TryGet(audioKeycacheKey, out var audioKey))
        {
            return new ValueTask<SpotifyAudioStream>(
                new SpotifyOfflineStream(
                    item: track,
                    file: file,
                    audioKey: new SpotifyAudioKey(audioKey, !audioKey.All(x => x is 0)),
                    isOgg: IsVorbis(file),
                    fileStream: fileStream
                )
            );
        }

        // We need to stream from CDN.
        return new ValueTask<SpotifyAudioStream>(StreamFromCdn(track, file, audioKeycacheKey, cancellationToken));
    }

    private async Task<SpotifyAudioStream> StreamFromCdn(SpotifySimpleTrack track, SpotifyAudioFile file,
        string audioKeycacheKey,
        CancellationToken cancellationToken)
    {
        var storageResolveTask = _storageResolveService.GetStorageFile(file, cancellationToken);
        //timeout of 5 seconds for the audio key
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
        var audioKeyTask = _audioKeysService.GetAudioKey(track.Uri, file.FileIdBase16, linkedCts.Token);
        await Task.WhenAll(storageResolveTask, audioKeyTask);

        // Store audio key 
        _cache.Set(audioKeycacheKey, audioKeyTask.Result.Key ?? new byte[16]);

        return new SpotifyCdnStream(
            item: track,
            file: storageResolveTask.Result,
            audioKey: audioKeyTask.Result,
            isOgg: IsVorbis(file)
        );
    }

    private static bool IsVorbis(SpotifyAudioFile file)
    {
        return file.Format is OggVorbis96 or OggVorbis160
            or OggVorbis320;
    }

    private static SpotifyAudioFile FindFile(ImmutableArray<SpotifyAudioFile> files,
        WaveeSpotifyPreferedQuality preferedQuality,
        bool vorbis)
    {
        return files.FirstOrDefault(x =>
        {
            if (vorbis)
            {
                return IsVorbis(x) && GetQuality(x) == preferedQuality;
            }

            return GetQuality(x) == preferedQuality;
        });
    }

    private static WaveeSpotifyPreferedQuality GetQuality(SpotifyAudioFile p0)
    {
        return p0.Format switch
        {
            Mp396 => WaveeSpotifyPreferedQuality.Low,
            AudioFile.Types.Format.Mp3160 => WaveeSpotifyPreferedQuality.Normal,
            Mp3320 => WaveeSpotifyPreferedQuality.High,
            OggVorbis96 => WaveeSpotifyPreferedQuality.Low,
            OggVorbis160 => WaveeSpotifyPreferedQuality.Normal,
            OggVorbis320 => WaveeSpotifyPreferedQuality.High,
            _ => WaveeSpotifyPreferedQuality.Low
        };
    }

    private ValueTask<ISpotifyPlayableItem> GetTrackOrEpisodeThrow(SpotifyId id, CancellationToken cancellationToken)
    {
        //Try hit cache
        switch (id.Type)
        {
            case AudioItemType.Track:
            {
                if (_cache.TryGet(id.ToString(), out var track))
                    return new ValueTask<ISpotifyPlayableItem>(Track.Parser.ParseFrom(track).MapToDto());
                break;
            }
            case AudioItemType.PodcastEpisode:
            {
                if (_cache.TryGet(id.ToString(), out var episode))
                    return new ValueTask<ISpotifyPlayableItem>(Episode.Parser.ParseFrom(episode).MapToDto());
                break;
            }
        }

        return new ValueTask<ISpotifyPlayableItem>(GetTrack(_metadataClient, id, cancellationToken));

        static async Task<ISpotifyPlayableItem> GetTrack(ISpotifyMetadataClient metadataClient,
            SpotifyId id,
            CancellationToken cancellationToken)
        {
            try
            {
                switch (id.Type)
                {
                    case AudioItemType.Track:
                    {
                        var track = await metadataClient.GetTrack(id, cancellationToken);
                        return track;
                    }
                    case AudioItemType.PodcastEpisode:
                    {
                        throw new SpotifyCannotPlayContentException(SpotifyCannotPlayReason.NotYetImplemented);
                    }
                    default:
                        throw new SpotifyCannotPlayContentException(SpotifyCannotPlayReason.NotTrackOrEpisode);
                }
            }
            catch (SpotifyTrackNotFoundException e)
            {
                throw new SpotifyCannotPlayContentException(SpotifyCannotPlayReason.InvalidTrack, e);
            }
        }
    }
}