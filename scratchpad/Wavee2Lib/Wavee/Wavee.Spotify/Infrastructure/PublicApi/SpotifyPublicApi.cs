namespace Wavee.Spotify.Infrastructure.PublicApi;

internal readonly struct SpotifyPublicApi : ISpotifyPublicApi
{
    private readonly Func<CancellationToken, Task<string>> _tokenFactory;

    public SpotifyPublicApi(Func<CancellationToken, Task<string>> tokenFactory)
    {
        _tokenFactory = tokenFactory;
    }
}