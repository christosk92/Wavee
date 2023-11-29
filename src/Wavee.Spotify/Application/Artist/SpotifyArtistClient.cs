﻿using System.Text.Json;
using Mediator;
using Wavee.Spotify.Application.GraphQL.Queries;
using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Album;
using Wavee.Spotify.Domain.Common;

namespace Wavee.Spotify.Application.Artist;

public interface ISpotifyArtistClient
{
    Task<(IReadOnlyCollection<SpotifySimpleAlbum> Albums, uint Total)> GetDiscographyAllAsync(SpotifyId id, uint offset, uint limit, CancellationToken cancellationToken);
}
internal sealed class SpotifyArtistClient : ISpotifyArtistClient
{
    private readonly IMediator _mediator;

    public SpotifyArtistClient(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<(IReadOnlyCollection<SpotifySimpleAlbum> Albums, uint Total)> GetDiscographyAllAsync(SpotifyId id, uint offset, uint limit, CancellationToken cancellationToken)
    {
        //TODO: Caching
        const string operationHash = "3f1c940cde61596bf4f534e5a736e6fac24d2a792cc81852820e20a93863a2b5";
        const string operationName = "queryArtistDiscographyAll";
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
            .GetProperty("all");
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