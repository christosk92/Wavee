using Eum.Spotify;
using Wavee.Spotify.Infrastructure.Common;
using Wavee.Spotify.Infrastructure.Common.Mercury;
using Wavee.Spotify.Infrastructure.Common.Token;
using Wavee.Spotify.Infrastructure.Sys;
using Wavee.Spotify.Infrastructure.Traits;
using Wavee.Spotify.Models;

namespace Wavee.Spotify.Infrastructure.Live;

internal sealed class LiveSpotifyClient : ISpotifyClient
{
    private readonly SpotifySessionConfig _config;
    private readonly SpotifySession<Live.Runtime> _session;

    public LiveSpotifyClient(SpotifySessionConfig config)
    {
        _config = config;
        _session = new SpotifySession<Runtime>(Runtime.New());
    }

    public ITokenProvider TokenProvider => _session.TokenProvider;

    // |-----------------------------------------|
    // |                Core                     |
    // |-----------------------------------------|

    public IMercuryClient Mercury => _session.Mercury;
    public ITokenProvider Token => _session.TokenProvider;

    public ValueTask<APWelcome> Connect(LoginCredentials credentials, CancellationToken cancellationToken = default)
    {
        return _session.Connect(_config.DeviceId, credentials, cancellationToken);
    }


    // |-----------------------------------------|
    // |               Fields                    |
    // |-----------------------------------------|
    public ConnectionState ConnectionState => _session.ConnectionState.Value;
    public Option<string> CountryCode => _session.CountryCode.Value;
    public Option<HashMap<string, string>> ProductInfo => _session.ProductInfo.Value;
    public SpotifySessionConfig Config => _config;

    // |-----------------------------------------|
    // |               Events                    |
    // |-----------------------------------------|
    public IObservable<ConnectionState> ConnectionStateChanged => _session.ConnectionState.OnChange();
    public IObservable<Option<string>> CountryCodeChanged => _session.CountryCode.OnChange();
    public IObservable<Option<HashMap<string, string>>> ProductInfoChanged => _session.ProductInfo.OnChange();
}