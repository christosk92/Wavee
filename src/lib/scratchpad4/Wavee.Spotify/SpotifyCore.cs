using Wavee.Core.AudioCore;
using Wavee.Core.Contracts;
using Wavee.Core.Id;
using Wavee.Core.Infrastructure.Live;
using Wavee.Core.Infrastructure.Traits;
using Wavee.Spotify.Cache;
using Wavee.Spotify.Infrastructure;
using Wavee.Spotify.Infrastructure.Playback;
using Wavee.Spotify.Infrastructure.Remote;
using Wavee.Spotify.Models.Responses;
using Wavee.Spotify.Remote.Infrastructure;

namespace Wavee.Spotify;

internal sealed class SpotifyCore<R> : ISpotifyCore
    where R : struct, HasLog<R>, HasWebsocket<R>, HasHttp<R>, HasAudioOutput<R>,  HasDatabase<R>
{
    private readonly SpotifyConnection<R> _connection;
    private readonly SpotifyRemoteConnection<R> _remoteConnection;

    public SpotifyCore(
        SpotifyConnection<R> connection,
        SpotifyRemoteConnection<R> remoteConnection)
    {
        _connection = connection;
        _remoteConnection = remoteConnection;

        PlaybackClient = new SpotifyPlaybackClient<R>(
            connection: _connection,
            preferredQualityType: _connection.Config.Playback.PreferredQualityType,
            remoteConnection: _remoteConnection,
            runtime: _connection._runtime
        );
    }

    public string Id { get; } = ISpotifyCore.SourceId;

    public async ValueTask<ITrack> GetTrackAsync(string id, CancellationToken ct = default)
    {
        var countryCodeMaybe = await _connection.Info.CountryCode;
        var productInfoMaybe = await _connection.Info.ProductInfo;
        var countryCoe = countryCodeMaybe.IfNone("US");
        var cdnUrl = productInfoMaybe.Match(x => x["image_url"], () => "https://i.scdn.co/image/{image_id}");
        var track = await _connection.Mercury.GetTrack(new AudioId(id, AudioItemType.Track, ISpotifyCore.SourceId), ct);
        return SpotifyTrackResponse.From(countryCoe, cdnUrl, track);
    }

    public ISpotifyPlaybackClient PlaybackClient { get; }

    public ISpotifyRemoteClient RemoteClient =>
        new SpotifyRemote<R>(
            connection: _remoteConnection
        );

    public AudioId FromUri(string uri)
    {
        throw new NotImplementedException();
    }
}

public interface ISpotifyCore : IAudioCore
{
    const string SourceId = "spotify";
    ISpotifyRemoteClient RemoteClient { get; }
    ISpotifyPlaybackClient PlaybackClient { get; }
    AudioId FromUri(string uri);
}