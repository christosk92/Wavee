using Eum.Spotify;
using LanguageExt;
using Wavee.Spotify;
using Wavee.Spotify.Infrastructure.Cache;
using Wavee.Spotify.Infrastructure.Mercury;
using Wavee.Spotify.Infrastructure.Remote.Messaging;
using static LanguageExt.Prelude;
namespace Wavee.UI.Infrastructure.Live;

internal sealed class LiveSpotify : Traits.SpotifyIO
{
    private Option<SpotifyClient> _connection = Option<SpotifyClient>.None;
    private readonly SpotifyConfig _config;

    public LiveSpotify(SpotifyConfig config)
    {
        _config = config;
    }

    public async ValueTask<Unit> Authenticate(LoginCredentials credentials, CancellationToken ct = default)
    {
        var core = await SpotifyClient.CreateAsync(credentials, _config, ct);
        _connection = Some(core);
        return Unit.Default;
    }

    public Option<APWelcome> WelcomeMessage()
    {
        var maybe = _connection.Map(x => x.WelcomeMessage);
        return maybe;
    }

    public Option<IObservable<SpotifyRemoteState>> ObserveRemoteState()
    {
        return _connection
            .Map(x => x.RemoteClient.StateChanged);
    }

    public Option<SpotifyCache> Cache()
    {
        return _connection
            .Map(x => x.Cache);
    }

    public Option<string> CountryCode()
    {
        return _connection
            .Bind(x => x.CountryCode);
    }

    public Option<string> CdnUrl()
    {
        return _connection
            .Bind(x => x.ProductInfo.Find("image_url"));
    }

    public MercuryClient Mercury()
    {
        return _connection
            .Map(x => x.MercuryClient)
            .IfNone(() => throw new InvalidOperationException("Mercury client not available"));
    }
}