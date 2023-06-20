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

    public async Task<Paged<SpotifyArtistDiscographyGroup>> GetDiscographyAsync(SpotifyId id, DiscographyType type,
        int offset,
        int limit, CancellationToken cancellationToken = default)
    {
        const string operationHash_ALBUMS = "983072ae655f5de212747a41be0d7c4cc49559aa9fe8e32836369b6f130cac33";
        const string operationName_ALBUMS = "queryArtistDiscographyAlbums";
        
        const string operationHash_SINGLES = "e02547d028482cec098d8d31899afcf488026d5dbdc2fcb973f05657c9cd6797";
        const string operationName_SINGLES = "queryArtistDiscographySingles";
        
        const string operationHash_COMPILATIONS = "6702dd8b0d793fdb981e1ce508bce717e06e81b60d2a6fb7c1b79843ca55e901";
        const string operationName_COMPILATIONS = "queryArtistDiscographyCompilations";

        var variables = new
        {
            uri = id.ToString(),
            offset,
            limit
        };

        var (operationHash, operationName) = type switch
        {
            DiscographyType.Albums => (operationHash_ALBUMS, operationName_ALBUMS),
            DiscographyType.Singles => (operationHash_SINGLES, operationName_SINGLES),
            DiscographyType.Compilations => (operationHash_COMPILATIONS, operationName_COMPILATIONS),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        }; 
        
        using var httpResponse = await _api.GetPartner(operationName, operationHash, variables, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        await using var stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
        return Paged<SpotifyArtistDiscographyGroup>.ParsePagedDiscographyFrom(stream, type,offset, limit);
    }
}