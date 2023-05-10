using Eum.Spotify.connectstate;
using LanguageExt.Effects.Traits;
using Spotify.Metadata;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Clients.Info;
using Wavee.Spotify.Clients.Mercury;
using Wavee.Spotify.Clients.Mercury.Metadata;
using Wavee.Spotify.Clients.Remote;
using Wavee.Spotify.Configs;
using Wavee.Spotify.Id;
using Wavee.Spotify.Infrastructure.Sys;

namespace Wavee.Spotify.Clients.Playback;

internal readonly struct PlaybackClient<RT> : IPlaybackClient where RT : struct, HasLog<RT>, HasCancel<RT>
{
    private static AtomHashMap<Guid, Action<SpotifyPlaybackInfo>> _onPlaybackInfo =
        LanguageExt.AtomHashMap<Guid, Action<SpotifyPlaybackInfo>>.Empty;

    private readonly Guid _mainConnectionId;

    //private readonly Action<SpotifyPlaybackInfo> _onPlaybackInfo;
    private readonly Func<ValueTask<string>> _getBearer;
    private readonly IMercuryClient _mercury;
    private readonly RT _runtime;
    private readonly PreferredQualityType _preferredQuality;

    public PlaybackClient(Guid mainConnectionId, Func<ValueTask<string>> getBearer, IMercuryClient mercury, RT runtime,
        Action<SpotifyPlaybackInfo> onPlaybackInfo, PreferredQualityType preferredQuality)
    {
        _mainConnectionId = mainConnectionId;
        _getBearer = getBearer;
        _mercury = mercury;
        _runtime = runtime;
        _preferredQuality = preferredQuality;
        _onPlaybackInfo.AddOrUpdate(mainConnectionId, onPlaybackInfo);
    }


    public Guid Listen(Action<SpotifyPlaybackInfo> onPlaybackInfo)
    {
        var g = Guid.NewGuid();
        _onPlaybackInfo.AddOrUpdate(g, onPlaybackInfo);
        return g;
    }

    public async Task<SpotifyPlaybackInfo> PlayTrack(string uri,
        Option<PreferredQualityType> preferredQualityOverride,
        CancellationToken ct = default)
    {
        var baseInfo = new SpotifyPlaybackInfo(Option<ProvidedTrack>.None,
            None,
            uri, None, None, false, true);

        _onPlaybackInfo.Iter(x => x(baseInfo));

        //start loading track
        var trackStreamAff = await SpotifyPlayback<RT>.LoadTrack(
            SpotifyId.FromUri(uri),
            preferredQualityOverride.IfNone(_preferredQuality),
            _mainConnectionId,
            _getBearer,
            _mercury, ct).Run(_runtime);

        var stream = trackStreamAff.ThrowIfFail();
        _onPlaybackInfo.Iter(x =>
        {
            baseInfo = baseInfo
                .EnrichFrom(stream.Metadata)
                .WithPaused(false);
            x(baseInfo);
        });

        return baseInfo;
    }
}

public readonly record struct SpotifyPlaybackInfo(
    Option<ProvidedTrack> Track,
    Option<int> Duration,
    Option<string> ContextUri,
    Option<string> PlaybackId,
    Option<TimeSpan> Position,
    bool Paused,
    bool Buffering)
{
    public SpotifyPlaybackInfo WithPaused(bool b)
    {
        return this with
        {
            Paused = b,
            Buffering = false
        };
    }

    public SpotifyPlaybackInfo EnrichFrom(TrackOrEpisode streamMetadata)
    {
        var track = new ProvidedTrack();

        return this with
        {
            Track = streamMetadata
                .Value.Match(
                    Left: e => EnrichFromEpisode(e, track),
                    Right: t => EnrichFromTrack(t, track)
                ),
            Duration = streamMetadata.Duration
        };
    }

    private static ProvidedTrack EnrichFromTrack(Track track, ProvidedTrack providedTrack)
    {
        if (track.HasPopularity)
            providedTrack.Metadata["popularity"] = track.Popularity.ToString();
        if (track.HasExplicit)
            providedTrack.Metadata["explicit"] = track.Explicit.ToString();
        if (track.HasHasLyrics)
            providedTrack.Metadata["has_lyrics"] = track.HasLyrics.ToString();
        if (track.HasName)
            providedTrack.Metadata["title"] = track.Name;
        if (track.HasDiscNumber)
            providedTrack.Metadata["album_disc_number"] = track.DiscNumber.ToString();

        for (int i = 0; i < track.Artist.Count; i++)
        {
            var artist = track.Artist[i];
            if (artist.HasName)
            {
                providedTrack.Metadata["atist_name" + (i == 0 ? string.Empty : $":{i}")] = artist.Name;
            }

            if (artist.HasGid)
            {
                providedTrack.Metadata["artist_uri" + (i == 0 ? string.Empty : $":{i}")] =
                    SpotifyId.FromRaw(artist.Gid.Span, AudioItemType.Artist).ToUri();
            }
        }

        var album = track.Album;
        if (album.Disc.Count > 0)
        {
            providedTrack.Metadata["album_track_count"] = album.Disc.Sum(x => x.Track.Count).ToString();
            providedTrack.Metadata["album_disc_count"] = album.Disc.Count.ToString();
        }

        if (album.HasName)
            providedTrack.Metadata["album_title"] = album.Name;
        if (album.HasGid)
            providedTrack.Metadata["album_uri"] = SpotifyId.FromRaw(album.Gid.Span, AudioItemType.Album).ToUri();

        for (int i = 0; i < album.Artist.Count; i++)
        {
            var artist = album.Artist[i];
            if (artist.HasName)
            {
                providedTrack.Metadata["album_atist_name" + (i == 0 ? string.Empty : $":{i}")] = artist.Name;
            }

            if (artist.HasGid)
            {
                providedTrack.Metadata["album_artist_uri" + (i == 0 ? string.Empty : $":{i}")] =
                    SpotifyId.FromRaw(artist.Gid.Span, AudioItemType.Artist).ToUri();
            }
        }

        if (track.HasDiscNumber)
        {
            foreach (var disc in album.Disc)
            {
                if (disc.Number != track.DiscNumber) continue;

                for (int i = 0; i < disc.Track.Count; i++)
                {
                    if (disc.Track[i].Gid.Span.SequenceEqual(track.Gid.Span))
                    {
                        providedTrack.Metadata["album_track_number"] = (i + 1).ToString();
                        break;
                    }
                }
            }
        }

        if (album.CoverGroup is not null)
        {
            ImageId.PutAsMetadata(providedTrack, album.CoverGroup);
        }

        providedTrack.Uri = SpotifyId.FromRaw(track.Gid.Span, AudioItemType.Track).ToUri();
        return providedTrack;
    }

    private static ProvidedTrack EnrichFromEpisode(Episode episode, ProvidedTrack track)
    {
        throw new NotImplementedException();
    }
}