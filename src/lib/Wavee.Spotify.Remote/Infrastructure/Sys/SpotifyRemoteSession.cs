using Wavee.Spotify.Remote.Infrastructure.Live;

namespace Wavee.Spotify.Remote.Infrastructure.Sys;

internal sealed class SpotifyRemoteSession<RT>
{
    readonly RT _runtime;

    public SpotifyRemoteSession(RT runtime)
    {
        _runtime = runtime;
    }

    public RT Runtime =>
        _runtime;
}