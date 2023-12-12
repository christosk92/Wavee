using System.Text.Json;
using System.Threading;
using Mediator;
using Wavee.Spotify.Application.GraphQL.Queries;
using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Album;
using Wavee.Spotify.Domain.Artist;
using Wavee.Spotify.Domain.Common;
using Wavee.Spotify.Infrastructure.LegacyAuth;

namespace Wavee.Spotify.Application.Artist;

public interface ISpotifyArtistClient
{
    Task<(IReadOnlyCollection<SpotifySimpleAlbum> Albums, uint Total)> GetDiscographyAlbumsAsync(SpotifyId id, uint offset, uint limit, CancellationToken cancellationToken);
    Task<(IReadOnlyCollection<SpotifySimpleAlbum> Albums, uint Total)> GetDiscographySinglesAsync(SpotifyId id, uint offset, uint limit, CancellationToken cancellationToken);
    Task<(IReadOnlyCollection<SpotifySimpleAlbum> Albums, uint Total)> GetDiscographyCompilationsAsync(SpotifyId id, uint offset, uint limit, CancellationToken cancellationToken);
    Task<(IReadOnlyCollection<SpotifySimpleAlbum> Albums, uint Total)> GetDiscographyAllAsync(SpotifyId id, uint offset, uint limit, CancellationToken cancellationToken);
    Task<SpotifyArtistView> GetAsync(SpotifyId id, CancellationToken cancellationToken);
}
internal sealed class SpotifyArtistClient : ISpotifyArtistClient
{
    private readonly IMediator _mediator;
    private readonly HttpClient _httpClient;
    public SpotifyArtistClient(IMediator mediator, IHttpClientFactory httpClientFactory)
    {
        _mediator = mediator;
        _httpClient = httpClientFactory.CreateClient(Constants.SpotifyRemoteStateHttpClietn);
    }

    public Task<(IReadOnlyCollection<SpotifySimpleAlbum> Albums, uint Total)> GetDiscographyAlbumsAsync(SpotifyId id, uint offset, uint limit, CancellationToken cancellationToken)
    {
        const string operationHash = "40232560bd1ae023cfbc4602d67477ea832c82c969296fff1617664b72a17f5d";
        const string operationName = "queryArtistDiscographyAlbums";
        return GetDiscographyGroupSpecific(id, offset, limit, operationHash, operationName, "albums", cancellationToken);
    }

    public Task<(IReadOnlyCollection<SpotifySimpleAlbum> Albums, uint Total)> GetDiscographySinglesAsync(SpotifyId id, uint offset, uint limit, CancellationToken cancellationToken)
    {
        const string operationHash = "c63ec6380f6dd82c1f4cfbfb70a9ce60285719ebfe5fdefd2469239f7e1b8963";
        const string operationName = "queryArtistDiscographySingles";
        return GetDiscographyGroupSpecific(id, offset, limit, operationHash, operationName, "singles", cancellationToken);
    }

    public Task<(IReadOnlyCollection<SpotifySimpleAlbum> Albums, uint Total)> GetDiscographyCompilationsAsync(SpotifyId id, uint offset, uint limit, CancellationToken cancellationToken)
    {
        const string operationHash = "03524e8eea1267057dc1c6604534cdb0e6daa52f63c0b33510b32ea7a6c9ad9e";
        const string operationName = "queryArtistDiscographyCompilations";
        return GetDiscographyGroupSpecific(id, offset, limit, operationHash, operationName, "compilations", cancellationToken);
    }

    public Task<(IReadOnlyCollection<SpotifySimpleAlbum> Albums, uint Total)> GetDiscographyAllAsync(SpotifyId id, uint offset, uint limit, CancellationToken cancellationToken)
    {
        const string operationHash = "3f1c940cde61596bf4f534e5a736e6fac24d2a792cc81852820e20a93863a2b5";
        const string operationName = "queryArtistDiscographyAll";
        return GetDiscographyGroupSpecific(id, offset, limit, operationHash, operationName, "all", cancellationToken);
    }

    private async Task<(IReadOnlyCollection<SpotifySimpleAlbum> Albums, uint Total)> GetDiscographyGroupSpecific(
        SpotifyId id, uint offset, uint limit,
        string operationHash, 
        string operationName,
        string key,
        CancellationToken cancellationToken)
    {
        var variables = new Dictionary<string, object>
        {
            ["uri"] = id.ToString(),
            ["offset"] = offset,
            ["limit"] = limit
        };

        using var response = await _mediator.Send(new GetSpotifyGraphQLQuery
        {
            OperationName = operationName,
            Variables = variables,
            Hash = operationHash
        }, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var jsondoc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var actualItems = jsondoc.RootElement.GetProperty("data")
            .GetProperty("artistUnion")
            .GetProperty("discography")
            .GetProperty(key);
        var total = actualItems.GetProperty("totalCount").GetUInt32();
        var items = actualItems.GetProperty("items");
        var output = new SpotifySimpleAlbum[items.GetArrayLength()];
        int i = 0;
        using var enumrator = items.EnumerateArray();
        while (enumrator.MoveNext())
        {
            var item = enumrator.Current;
            var releases = item.GetProperty("releases").GetProperty("items");
            using var releasesEnumrator = releases.EnumerateArray();
            var firstrelease = releasesEnumrator.MoveNext() ? releasesEnumrator.Current : default;
            var releaseDate = firstrelease.GetProperty("date");
            var year = releaseDate.GetProperty("year").GetUInt16();
            var album = new SpotifySimpleAlbum
            {
                Uri = SpotifyId.FromUri(firstrelease.GetProperty("uri")
                    .GetString()!),
                Name = firstrelease.GetProperty("name")
                    .GetString(),
                Images = GetImages(firstrelease.GetProperty("coverArt")
                    .GetProperty("sources")),
                ReleaseDate = new DateOnly(year,
                    1,
                    1),
                Type = firstrelease.GetProperty("type")
                    .GetString()!
            };
            output[i++] = album;
        }
        return (output, total);
    }

    public async Task<SpotifyArtistView> GetAsync(SpotifyId id, CancellationToken cancellationToken)
    {
        // const string metadataUri = "https://spclient.com/metadata/4/artist/";
        // var artistUri = metadataUri + id.ToBase16();
        // using var mercuryResponse = await _httpClient.GetAsync(artistUri, cancellationToken);
        // mercuryResponse.EnsureSuccessStatusCode();
        // using var rr = await mercuryResponse.Content.ReadAsStreamAsync(cancellationToken);
        //
        // var artistR = global::Spotify.Metadata.Artist.Parser.ParseFrom(rr);

        const string operationName = "queryArtistOverview";
        const string operationHash = "3a747b83568580814534e662a2569a6978ac3ad2e449ff751a859abe05dec995";
        const string locale = "";
        const bool includePrerelease = true;

        var variables = new Dictionary<string, object>
        {
            ["uri"] = id.ToString(),
            ["locale"] = locale,
            ["includePrerelease"] = includePrerelease
        };

        using var response = await _mediator.Send(new GetSpotifyGraphQLQuery
        {
            OperationName = operationName,
            Variables = variables,
            Hash = operationHash
        }, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var jsondoc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var artist = jsondoc.RootElement.GetProperty("data")
            .GetProperty("artistUnion");

        var parsed = SpotifyArtistViewParser.Parse(artist);
        return parsed;
    }

    private static SpotifyImage[] GetImages(JsonElement sources)
    {
        var output = new SpotifyImage[sources.GetArrayLength()];
        int i = 0;
        using var enumrator = sources.EnumerateArray();
        while (enumrator.MoveNext())
        {
            var item = enumrator.Current;
            var image = new SpotifyImage
            {
                Url = item.GetProperty("url").GetString(),
                Height = item.GetProperty("height").GetUInt16(),
                Width = item.GetProperty("width").GetUInt16()
            };
            output[i++] = image;
        }

        return output;
    }
}