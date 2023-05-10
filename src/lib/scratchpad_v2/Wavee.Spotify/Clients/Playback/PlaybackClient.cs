using LanguageExt.Effects.Traits;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Clients.Mercury;
using Wavee.Spotify.Clients.Mercury.Metadata;
using Wavee.Spotify.Clients.Remote;
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

    public PlaybackClient(Guid mainConnectionId, Func<ValueTask<string>> getBearer, IMercuryClient mercury, RT runtime,
        Action<SpotifyPlaybackInfo> onPlaybackInfo)
    {
        _mainConnectionId = mainConnectionId;
        _getBearer = getBearer;
        _mercury = mercury;
        _runtime = runtime;
        _onPlaybackInfo.AddOrUpdate(mainConnectionId, onPlaybackInfo);
    }


    public Guid Listen(Action<SpotifyPlaybackInfo> onPlaybackInfo)
    {
        var g = Guid.NewGuid();
        _onPlaybackInfo.AddOrUpdate(g, onPlaybackInfo);
        return g;
    }

    public async Task<SpotifyPlaybackInfo> PlayTrack(string uri, CancellationToken ct = default)
    {
        var baseInfo = new SpotifyPlaybackInfo(uri,
            None, LanguageExt.HashMap<string, string>.Empty,
            uri, None, None, false, true);

        _onPlaybackInfo.Iter(x => x(baseInfo));

        //start loading track
        var trackStreamAff = await SpotifyPlayback<RT>.LoadTrack(
            uri,
            _mainConnectionId,
            _getBearer,
            _mercury, ct).Run(_runtime);
        
        var stream = trackStreamAff.ThrowIfFail();
        _onPlaybackInfo.Iter(x =>
        {
            baseInfo = baseInfo.EnrichFrom(stream.Metadata); 
            x(baseInfo);
        });

        return baseInfo;
    }
}

public readonly record struct SpotifyPlaybackInfo(
    Option<string> TrackUri,
    Option<int> Duration,
    HashMap<string, string> Metadata,
    Option<string> ContextUri,
    Option<string> PlaybackId,
    Option<TimeSpan> Position,
    bool Paused,
    bool Buffering)
{
    public SpotifyPlaybackInfo EnrichFrom(TrackOrEpisode streamMetadata)
    {
        throw new NotImplementedException();
    }
}