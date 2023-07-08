using System;
using System.Text;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Spotify.Metadata;
using Wavee.Id;
using Wavee.Metadata.Artist;
using Wavee.Metadata.Common;
using Wavee.Sqlite.Entities;
using Wavee.Sqlite.Repository;
using Wavee.UI.Client.ExtendedMetadata;
using Wavee.UI.ViewModel.Playlist;

namespace Wavee.UI.Spotify.Clients;

internal sealed class SpotifyUIExtendedMetadataClient : IWaveeUIExtendedMetadataClient
{
    private readonly WeakReference<SpotifyClient> _spotifyClient;

    public SpotifyUIExtendedMetadataClient(SpotifyClient spotifyClient)
    {
        _spotifyClient = new WeakReference<SpotifyClient>(spotifyClient);
    }

    public async ValueTask<Dictionary<string, Either<WaveeUIEpisode, WaveeUITrack>>> GetTracks(string[] itemIds,
        bool returnData,
        CancellationToken ct = default)
    {
        //Strategy: Check which tracks are already cached, and only fetch the ones that are not cached
        if (!_spotifyClient.TryGetTarget(out var spotifyClient))
        {
            throw new InvalidOperationException("SpotifyClient is not available");
        }

        var trackids = itemIds.Where(x => x.StartsWith("spotify:track:")).ToArray();
        var episodeIds = itemIds.Where(x => x.StartsWith("spotify:episode:")).ToArray();

        var existingTracksTask = spotifyClient.Cache.GetTracksFromCache(trackids);
        var existingEpisodesTask = spotifyClient.Cache.GetEpisodesFromCache(episodeIds);

        await Task.WhenAll(existingTracksTask, existingEpisodesTask);

        var existingTracks = existingTracksTask.Result;
        var existingEpisodes = existingEpisodesTask.Result;

        var missingTracks = existingTracks.Where(x => x.Value.IsNone).Select(f => f.Key)
            .Select(x => (AudioItemType.Track, x))
            .Concat(existingEpisodes.Where(x => x.Value.IsNone).Select(f => f.Key)
                .Select(x => (AudioItemType.PodcastEpisode, x)))
            .GroupBy(x => x.Item1, x => x.x)
            .ToArray();

        var newTracks = new Dictionary<string, TrackWithExpiration>();
        var newEpisodes = new Dictionary<string, EpisodeWithExpiration>();
        if (missingTracks.Length > 0)
        {
            var tracks = await spotifyClient.Metadata.GetExtendedMetadataForItems(missingTracks, ct);
            foreach (var track in tracks)
            {
                if (track.Value.IsSome)
                {
                    track.Value.ValueUnsafe().Match(
                        Left: tr => newTracks[track.Key] = tr,
                        Right: ep => newEpisodes[track.Key] = ep
                    );
                }
                else
                {

                }
            }
        }

        if (newTracks.Count > 0)
        {
            await spotifyClient.Cache.AddTracksToCache(newTracks);
        }

        if (newEpisodes.Count > 0)
        {
            await spotifyClient.Cache.AddEpisodesToCache(newEpisodes);
        }

        if (returnData)
        {
            var result = new Dictionary<string, Either<WaveeUIEpisode, WaveeUITrack>>();
            foreach (var track in existingTracks)
            {
                result[track.Key] = track.Value.Match(
                    None: () => ToWaveUITrack(track.Key, newTracks[track.Key].Track),
                    Some: tr => ToWaveUITrack(track.Key, tr)
                );
            }

            foreach (var episode in existingEpisodes)
            {
                result[episode.Key] = episode.Value.Match(
                    None: () => ToWaveUIEpisode(episode.Key, newEpisodes[episode.Key].Track),
                    Some: tr => ToWaveUIEpisode(episode.Key, tr)
                );
            }

            return result;
        }
        return new Dictionary<string, Either<WaveeUIEpisode, WaveeUITrack>>(0);
    }

    private static WaveeUIEpisode ToWaveUIEpisode(string episodeKey, Episode episode)
    {
        return new WaveeUIEpisode
        {
            Covers = GetCoverImages(episode),
            Id = episodeKey,
        };
    }

    private static WaveeUIEpisode ToWaveUIEpisode(string episodeKey, CachedEpisode tr)
    {
        return new WaveeUIEpisode
        {
            Covers = Array.Empty<CoverImage>(),
            Id = episodeKey
        };
    }

    private static WaveeUITrack ToWaveUITrack(string trackKey, CachedTrack track)
    {
        var data = Track.Parser.ParseFrom(track.OriginalData);
        return ToWaveUITrack(trackKey, data);
    }

    private static WaveeUITrack ToWaveUITrack(string trackKey, Track track)
    {
        return new WaveeUITrack
        {
            Id = trackKey,
            Name = track.Name,
            Artists = track.Artist.Select(x =>
                    new TrackArtist(Id: SpotifyId.FromRaw(x.Gid.Span, AudioItemType.Artist, ServiceType.Spotify),
                        Name: x.Name))
                .Cast<ITrackArtist>().ToArray(),
            Album = new TrackAlbum(
                Id: SpotifyId.FromRaw(track.Album.Gid.Span, AudioItemType.Album, ServiceType.Spotify).ToString(),
                Name: track.Album.Name,
                Images: Array.Empty<ICoverImage>()
              ),
            DurationMs = track.Duration,
            TrackNumber = track.Number,
            DiscNumber = track.DiscNumber,
            Covers = GetCoverImages(track),
        };
    }
    private static CoverImage[] GetCoverImages(Episode episode)
    {
        const string cdnUrlImage = "https://i.scdn.co/image/{0}";
        return episode.CoverImage.Image
            .Select(x => new CoverImage(
                Url: CalculateUrl(x, cdnUrlImage),
                Width: x.HasWidth ? (ushort)x.Width : Option<ushort>.None,
                Height: x.HasHeight ? (ushort)x.Height : Option<ushort>.None
            ))
            .ToArray();
    }

    private static CoverImage[] GetCoverImages(Track track)
    {
        const string cdnUrlImage = "https://i.scdn.co/image/{0}";
        return track.Album.CoverGroup.Image
            .Select(x => new CoverImage(
                Url: CalculateUrl(x, cdnUrlImage),
                Width: x.HasWidth ? (ushort)x.Width : Option<ushort>.None,
                Height: x.HasHeight ? (ushort)x.Height : Option<ushort>.None
            ))
            .ToArray();
    }

    private static string CalculateUrl(Image image, string httpsIScdnCoImage)
    {
        //convert to hex id
        var sb = new StringBuilder();
        ReadOnlySpan<byte> span = image.FileId.Span;
        foreach (var b in span)
        {
            sb.Append(b.ToString("x2"));
        }

        return string.Format(httpsIScdnCoImage, sb.ToString());
    }
}