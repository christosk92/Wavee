using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Clients.Mercury;
using Wavee.Spotify.Clients.Remote;

namespace Wavee.Spotify.Clients.Playback;

internal readonly struct PlaybackClient<RT> : IPlaybackClient where RT : struct, HasLog<RT>
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
            uri, None, None, false, true);
        
        _onPlaybackInfo.Iter(x => x(baseInfo));
        
        
        
        return new SpotifyPlaybackInfo();
    }
}

public readonly record struct SpotifyPlaybackInfo(Option<string> TrackUri,
    Option<string> ContextUri,
    Option<string> PlaybackId,
    Option<TimeSpan> Position,
    bool Paused,
    bool Buffering);