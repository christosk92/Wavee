using Wavee.Spotify.Common;
using Wavee.Spotify.InternalApi;

namespace Wavee.Spotify.Artist.Live;

internal readonly struct LiveArtistClient : IArtistClient
{
    private readonly IInternalApi _api;

    public LiveArtistClient(IInternalApi api)
    {
        _api = api;
    }

    public async Task<SpotifyArtist> GetArtistAsync(SpotifyId id, CancellationToken cancellationToken = default)
    {
        const string operationHash = "35648a112beb1794e39ab931365f6ae4a8d45e65396d641eeda94e4003d41497";
        const string operationName = "queryArtistOverview";
        var variables = new
        {
            uri = id.ToString(),
            locale = string.Empty,
            includePrerelease = false
        };

        using var httpResponse = await _api.GetPartner(operationName, operationHash, variables, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();
        await using var stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
        return SpotifyArtist.ParseFrom(stream);
    }
}