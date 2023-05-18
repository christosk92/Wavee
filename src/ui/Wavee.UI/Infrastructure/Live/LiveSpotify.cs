using Eum.Spotify;
using LanguageExt;
using Wavee.Spotify;
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
}