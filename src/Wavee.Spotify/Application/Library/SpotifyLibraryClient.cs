using System.Collections.Concurrent;
using System.Collections.Immutable;
using Mediator;
using System.Text.Json;
using NeoSmart.AsyncLock;
using Wavee.Spotify.Application.Common.Queries;
using Wavee.Spotify.Application.GraphQL.Queries;
using Wavee.Spotify.Application.Library.Query;
using Wavee.Spotify.Application.Metadata.Query;
using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Artist;
using Wavee.Spotify.Domain.Common;
using Wavee.Spotify.Domain.Library;
using Wavee.Spotify.Infrastructure.LegacyAuth;

namespace Wavee.Spotify.Application.Library;

internal class SpotifyLibraryClient : ISpotifyLibraryClient
{
    private readonly record struct LibraryKeyComposite(string User, SpotifyItemType Type);
    private readonly IMediator _mediator;
    private readonly SpotifyTcpHolder _tcpHolder;
    private static readonly AsyncLock _libraryPerUserLock = new AsyncLock();
    private static Dictionary<LibraryKeyComposite, IReadOnlyCollection<SpotifyLibraryItem<SpotifyId>>> _libraryPerUser = new();

    public SpotifyLibraryClient(IMediator mediator, SpotifyTcpHolder tcpHolder)
    {
        _mediator = mediator;
        _tcpHolder = tcpHolder;
    }
    public async Task<(SpotifyLibraryItem<SpotifySimpleArtist>[] Items, int Total)> GetArtists(
        string? query,
        SpotifyArtistLibrarySortField order,
        int offset, int limit,
        CancellationToken cancellationToken = default)
    {

        var user = _tcpHolder.WelcomeMessage.Result.CanonicalUsername;
        var key = new LibraryKeyComposite(user, SpotifyItemType.Artist);
        using (await _libraryPerUserLock.LockAsync(cancellationToken))
        {
            var recentlyPlayedTask = Task.Run(async () =>
            {
                if (order is SpotifyArtistLibrarySortField.Recents)
                {
                    return await _mediator.Send(new FetchRecentlyPlayedQuery
                    {
                        User = user,
                        Limit = 50,
                        Filter = "default,track,collection-new-episodes"
                    }, cancellationToken);
                }
                return null;
            }, cancellationToken);

            if (!_libraryPerUser.TryGetValue(key, out var libraryForUserAlreadyCached))
            {
                //Get 
                var items = await _mediator.Send(new FetchArtistCollectionQuery
                {
                    User = user,
                }, cancellationToken);
                libraryForUserAlreadyCached = items;
                _libraryPerUser[key] = libraryForUserAlreadyCached;
            }


            var uris = libraryForUserAlreadyCached.ToDictionary(x => x.Item.ToString(), x => x);
            var metadataRaw = await _mediator.Send(new FetchBatchedMetadataQuery
            {
                AllowCache = true,
                Uris = uris.Keys,
                Country = _tcpHolder.Country,
                ItemsType = SpotifyItemType.Artist
            }, cancellationToken);

            var metadata = metadataRaw
                .ToDictionary(x => x.Key,
                    x => global::Spotify.Metadata.Artist.Parser.ParseFrom(x.Value));

            var recentlyPlayed = await recentlyPlayedTask;
            var finalList = metadata.Select(x =>
            {
                var libraryItem = uris[x.Key];
                var recentlyPlayedItem = recentlyPlayed?.FirstOrDefault(f => f.Uri == x.Key)?.PlayedAt;
                return new SpotifyLibraryItem<SpotifySimpleArtist>
                {
                    Item = new SpotifySimpleArtist
                    {
                        Id = SpotifyId.FromUri(x.Key),
                        Name = x.Value.Name,
                        Images = BuildImages(x.Value)
                    },
                    AddedAt = libraryItem.AddedAt,
                    LastPlayedAt = recentlyPlayedItem
                };
            });


            return (FilterSortLimit(finalList, query, order, offset, limit), uris.Count);
        }


        // const string operationName = "libraryV3";
        // var variables = new Dictionary<string, object>
        // {
        //     { "filters", new[] { "Artists" } },
        //     {
        //         "order", order switch
        //         {
        //             SpotifyArtistLibrarySortField.Recents => "Recents",
        //             SpotifyArtistLibrarySortField.RecentlyAdded => "Recently Added",
        //             SpotifyArtistLibrarySortField.Alphabetical => "Alphabetical",
        //             _ => throw new ArgumentOutOfRangeException(nameof(order), order, null)
        //         }
        //     },
        //     { "textFilter", query ?? string.Empty },
        //     { "features", new[] { "LIKED_SONGS", "YOUR_EPISODES" } },
        //     { "limit", limit },
        //     { "offset", offset },
        //     { "flatten", false },
        //     { "expandedFolders", Array.Empty<string>() },
        //     { "folderUri", (string?)null },
        //     { "includeFoldersWhenFlattening", true },
        //     { "withCuration", false }
        // };
        //
        // const string hash = "17d801ba80f3a3d7405966641818c334fe32158f97e9e8b38f1a92f764345df9";
        //
        // using var response = await _mediator.Send(new GetSpotifyGraphQLQuery
        // {
        //     OperationName = operationName,
        //     Variables = variables,
        //     Hash = hash
        // }, cancellationToken);
        // await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        // using var jsonDocument = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        // var libraryV3 = jsonDocument.RootElement.GetProperty("data").GetProperty("me").GetProperty("libraryV3");
        // var totalCount = libraryV3.GetProperty("totalCount").GetInt32();
        // var items = libraryV3.GetProperty("items");
        // using var enumerator = items.EnumerateArray();
        // var output = new SpotifyLibraryItem<SpotifySimpleArtist>[items.GetArrayLength()];
        // var i = 0;
        // while (enumerator.MoveNext())
        // {
        //     var curr = enumerator.Current;
        //     var addedAt = curr.GetProperty("addedAt").GetProperty("isoString").GetDateTime();
        //     var item = curr.GetProperty("item").GetProperty("data");
        //     var uri = item.GetProperty("uri").GetString();
        //     var name = item.GetProperty("profile").GetProperty("name").GetString();
        //     var images = ParseVisuals(item.GetProperty("visuals").GetProperty("avatarImage"));
        //     var artist = new SpotifySimpleArtist
        //     {
        //         Id = SpotifyId.FromUri(uri),
        //         Name = name,
        //         Images = images
        //     };
        //     output[i++] = new SpotifyLibraryItem<SpotifySimpleArtist>
        //     {
        //         Item = artist,
        //         AddedAt = new DateTimeOffset(addedAt, TimeSpan.Zero)
        //     };
        // }
        //
        //
        // return (output, totalCount);
    }

    private SpotifyLibraryItem<SpotifySimpleArtist>[] FilterSortLimit(
        IEnumerable<SpotifyLibraryItem<SpotifySimpleArtist>> finalList,
        string query,
        SpotifyArtistLibrarySortField order,
        int offset,
        int limit)
    {
        if (!string.IsNullOrEmpty(query))
        {
            finalList = finalList
                .Where(x => x.Item.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        finalList = order switch
        {
            SpotifyArtistLibrarySortField.Recents => finalList
                .OrderByDescending(x => x.LastPlayedAt)
                .ThenByDescending(x => x.AddedAt),
            SpotifyArtistLibrarySortField.RecentlyAdded => finalList.OrderByDescending(x => x.AddedAt),
            SpotifyArtistLibrarySortField.Alphabetical => finalList.OrderBy(f => f.Item.Name),
            _ => throw new ArgumentOutOfRangeException(nameof(order), order, null)
        };

        return finalList
            .Skip(offset)
            .Take(limit)
            .ToArray();
    }

    private IReadOnlyCollection<SpotifyImage> BuildImages(global::Spotify.Metadata.Artist artist)
    {
        var portraitrGroup = artist.PortraitGroup.Image;
        const string url = "https://i.scdn.co/image/";
        return portraitrGroup.Select(c =>
        {
            var id = SpotifyId.FromRaw(c.FileId.Span, SpotifyItemType.Unknown);
            var hex = id.ToBase16();
            var uri = $"{url}{hex}";
            return new SpotifyImage(
                Url: uri,
                Width: (ushort?)c.Width,
                Height: (ushort?)c.Height);
        }).ToImmutableArray();
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