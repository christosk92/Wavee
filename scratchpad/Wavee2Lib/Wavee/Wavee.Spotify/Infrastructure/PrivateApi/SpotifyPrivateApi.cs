namespace Wavee.Spotify.Infrastructure.PrivateApi;

internal readonly struct SpotifyPrivateApi : ISpotifyPrivateApi
{
    private readonly Func<CancellationToken, Task<string>> _tokenFactory;

    public SpotifyPrivateApi(Func<CancellationToken, Task<string>> tokenFactory)
    {
        _tokenFactory = tokenFactory;
    }
}