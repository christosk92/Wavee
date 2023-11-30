using Mediator;
using System.Text.Json;
using Wavee.Spotify.Application.GraphQL.Queries;
using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Artist;
using Wavee.Spotify.Domain.Common;
using Wavee.Spotify.Domain.Library;

namespace Wavee.Spotify.Application.Library;

internal class SpotifyLibraryClient : ISpotifyLibraryClient
{
    private readonly IMediator _mediator;
    public SpotifyLibraryClient(IMediator mediator)
    {
        _mediator = mediator;
    }
    public async Task<(SpotifyLibraryItem<SpotifySimpleArtist>[] Items, int Total)> GetArtists(
        string? query,
        SpotifyArtistLibrarySortField order, 
        int offset, int limit,
        CancellationToken cancellationToken = default)
    {
        const string operationName = "libraryV3";
        var variables = new Dictionary<string, object>
        {
            { "filters", new[] { "Artists" } },
            {
                "order", order switch
                {
                    SpotifyArtistLibrarySortField.Recents => "Recents",
                    SpotifyArtistLibrarySortField.RecentlyAdded => "Recently Added",
                    SpotifyArtistLibrarySortField.Alphabetical => "Alphabetical",
                    _ => throw new ArgumentOutOfRangeException(nameof(order), order, null)
                }
            },
            { "textFilter", query ?? string.Empty },
            { "features", new[] { "LIKED_SONGS", "YOUR_EPISODES" } },
            { "limit", limit },
            { "offset", offset },
            { "flatten", false },
            { "expandedFolders", Array.Empty<string>() },
            { "folderUri", (string?)null },
            { "includeFoldersWhenFlattening", true },
            { "withCuration", false }
        };

        const string hash = "17d801ba80f3a3d7405966641818c334fe32158f97e9e8b38f1a92f764345df9";

        using var response = await _mediator.Send(new GetSpotifyGraphQLQuery
        {
            OperationName = operationName,
            Variables = variables,
            Hash = hash
        }, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var libraryV3 = jsonDocument.RootElement.GetProperty("data").GetProperty("me").GetProperty("libraryV3");
        var totalCount = libraryV3.GetProperty("totalCount").GetInt32();
        var items = libraryV3.GetProperty("items");
        using var enumerator = items.EnumerateArray();
        var output = new SpotifyLibraryItem<SpotifySimpleArtist>[items.GetArrayLength()];
        var i = 0;
        while (enumerator.MoveNext())
        {
            var curr = enumerator.Current;
            var addedAt = curr.GetProperty("addedAt").GetProperty("isoString").GetDateTime();
            var item = curr.GetProperty("item").GetProperty("data");
            var uri = item.GetProperty("uri").GetString();
            var name = item.GetProperty("profile").GetProperty("name").GetString();
            var images = ParseVisuals(item.GetProperty("visuals").GetProperty("avatarImage"));
            var artist = new SpotifySimpleArtist
            {
                Id = SpotifyId.FromUri(uri),
                Name = name,
                Images = images
            };
            output[i++] = new SpotifyLibraryItem<SpotifySimpleArtist>
            {
                Item = artist,
                AddedAt = new DateTimeOffset(addedAt, TimeSpan.Zero)
            };
        }


        return (output, totalCount);
    }

    private static SpotifyImage[] ParseVisuals(JsonElement getProperty)
    {
        var sources = getProperty.GetProperty("sources");
        using var enumerator = sources.EnumerateArray();
        var output = new SpotifyImage[sources.GetArrayLength()];
        var i = 0;
        while (enumerator.MoveNext())
        {
            var curr = enumerator.Current;
            var url = curr.GetProperty("url").GetString();
            var width = curr.GetProperty("width").TryGetUInt16(out var w) ? w : (ushort?)null;
            var height = curr.GetProperty("height").TryGetUInt16(out var h) ? h : (ushort?)null;
            output[i++] = new SpotifyImage(url, width, height);
        }
        return output;
    }
}

public interface ISpotifyLibraryClient
{
    Task<(SpotifyLibraryItem<SpotifySimpleArtist>[] Items, int Total)> GetArtists(string? query, SpotifyArtistLibrarySortField order, int offset, int limit, CancellationToken cancellationToken = default);
}