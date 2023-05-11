using Eum.Spotify.connectstate;
using Google.Protobuf;
using LanguageExt.Effects.Traits;
using Spotify.Metadata;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Clients.Info;
using Wavee.Spotify.Clients.Mercury;
using Wavee.Spotify.Clients.Mercury.Key;
using Wavee.Spotify.Clients.Mercury.Metadata;
using Wavee.Spotify.Clients.Remote;
using Wavee.Spotify.Configs;
using Wavee.Spotify.Id;
using Wavee.Spotify.Infrastructure.Sys;
using Wavee.VorbisDecoder.Convenience;

namespace Wavee.Spotify.Clients.Playback;

internal readonly struct PlaybackClient<RT> : IPlaybackClient where RT : struct, HasLog<RT>, HasHttp<RT>, HasAudioOutput<RT>
{
    private static AtomHashMap<Guid, Action<SpotifyPlaybackInfo>> _onPlaybackInfo =
        LanguageExt.AtomHashMap<Guid, Action<SpotifyPlaybackInfo>>.Empty;

    private readonly Guid _mainConnectionId;

    private readonly Func<ValueTask<string>> _getBearer;
    private readonly Func<SpotifyId, ByteString, CancellationToken, Aff<RT, Either<AesKeyError, AudioKey>>> _fetchAudioKeyFunc;
    private readonly IMercuryClient _mercury;
    private readonly RT _runtime;
    private readonly PreferredQualityType _preferredQuality;

    public PlaybackClient(Guid mainConnectionId,
        Func<ValueTask<string>> getBearer,
        Func<SpotifyId, ByteString, CancellationToken, Aff<RT, Either<AesKeyError, AudioKey>>> fetchAudioKeyFunc,
        IMercuryClient mercury, RT runtime,
        Action<SpotifyPlaybackInfo> onPlaybackInfo,
        PreferredQualityType preferredQuality)
    {
        _mainConnectionId = mainConnectionId;
        _getBearer = getBearer;
        _fetchAudioKeyFunc = fetchAudioKeyFunc;
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
            _getBearer, 
            _fetchAudioKeyFunc,
            _mercury, ct).Run(_runtime);

        var stream = trackStreamAff.ThrowIfFail();
        
        //start playing track
        //dummy< DO NOT USE IN PROD
        RT runtime1 = _runtime;
        await Task.Factory.StartNew(async () =>
        {
            AudioOutput<RT>.Start().Run(runtime1);
            await using var inputStream = stream.AsStream();
            await using var decoder = new VorbisWaveReader(inputStream, stream.Metadata.Duration, true);
            var buffer = new byte[decoder.WaveFormat.AverageBytesPerSecond];
            while (true)
            {
                var read = await decoder.ReadAsync(buffer, 0, buffer.Length, ct);
                if (read == 0)
                    break;

                await AudioOutput<RT>.Write(buffer).Run(runtime: runtime1);
            }
        });

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