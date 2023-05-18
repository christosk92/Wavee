using Eum.Spotify;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Wavee.Core.Infrastructure.Live;
using Wavee.Spotify;
using Wavee.Spotify.Configs;
using Wavee.Spotify.Infrastructure;

namespace Wavee.UI.Infrastructure.Live;

internal sealed class LiveSpotify : Traits.SpotifyIO
{
    private Option<SpotifyCore<WaveeRuntime>> _connection = Option<SpotifyCore<WaveeRuntime>>.None;
    private readonly SpotifyConfig _config;

    public LiveSpotify(SpotifyConfig config)
    {
        _config = config;
    }

    public async ValueTask<Unit> Authenticate(LoginCredentials credentials, CancellationToken ct = default)
    {
        var core = await SpotifyClient.Create(credentials, _config, Option<ILogger>.None, ct);
        _connection = Some((SpotifyCore<WaveeRuntime>)core);
        return Unit.Default;
    }

    public Option<APWelcome> WelcomeMessage()
    {
        var maybe = _connection.Bind(x => x.WelcomeMessage);
        return maybe;
    }
}