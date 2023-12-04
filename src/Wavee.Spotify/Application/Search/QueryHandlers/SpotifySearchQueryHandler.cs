using System.Collections.Immutable;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Mediator;
using Wavee.Spotify.Application.GraphQL.Queries;
using Wavee.Spotify.Application.Search.Queries;
using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Album;
using Wavee.Spotify.Domain.Artist;
using Wavee.Spotify.Domain.Common;
using Wavee.Spotify.Domain.Genres;
using Wavee.Spotify.Domain.Playlists;
using Wavee.Spotify.Domain.Podcasts;
using Wavee.Spotify.Domain.Tracks;
using Wavee.Spotify.Domain.User;

namespace Wavee.Spotify.Application.Search.QueryHandlers;

public sealed class SpotifySearchQueryHandler : IQueryHandler<SpotifySearchQuery, SpotifySearchResult>
{
    private readonly IMediator _mediator;
    public SpotifySearchQueryHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async ValueTask<SpotifySearchResult> Handle(SpotifySearchQuery query, CancellationToken cancellationToken)
    {
        const string hash = "21969b655b795601fb2d2204a4243188e75fdc6d3520e7b9cd3f4db2aff9591e";
        const string operationName = "searchDesktop";

        var variables = new Dictionary<string, object>
        {
            ["searchTerm"] = WebUtility.UrlEncode(query.Query),
            ["offset"] = query.Offset,
            ["limit"] = query.Limit,
            ["numberOfTopResults"] = query.NumberOfTopResults,
            ["includeAudiobooks"] = false,
        };

        using var res = await _mediator.Send(new GetSpotifyGraphQLQuery
        {
            OperationName = operationName,
            Variables = variables,
            Hash = hash
        }, cancellationToken);

        await using var stream = await res.Content.ReadAsStreamAsync(cancellationToken);
        using var jsonDoc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var searchV2 = jsonDoc.RootElement.GetProperty("data").GetProperty("searchV2");
        var chipOrders = searchV2.GetProperty("chipOrder").GetProperty("items");
        using var chipOrderEnumator = chipOrders.EnumerateArray();

        var result = ParseOutput(searchV2, chipOrderEnumator);
        return result;
    }

    private SpotifySearchResult ParseOutput(
        JsonElement searchV2,
        JsonElement.ArrayEnumerator chipOrderEnumator)
    {
        Span<SpotifySearchCategoryResult> output = new SpotifySearchCategoryResult[9];
        int order = -1;
        while (chipOrderEnumator.MoveNext())
        {
            var typeName = chipOrderEnumator.Current.GetProperty("typeName").GetString()!;

            var (key, propertyKey, isTopResults, isTracksResult) = typeName switch
            {
                "TOP_RESULTS" => ("top_results", "topResults", true, false),
                "TRACKS" => ("tracks", "tracksV2", false, true),
                _ => (typeName.ToLower(), typeName.ToLower(), false, false)
            };
            if (!searchV2.TryGetProperty(propertyKey, out var property))
            {
                continue;
            }
            order++;
            output[order] = ParseCategory(
                root: property,
                order: order,
                key: key,
                isNotTopResults: !isTopResults,
                isNotTrack: !isTracksResult
            );
        }

        return new SpotifySearchResult
        {
            Items = ImmutableArray.Create(output)
        };
    }


    private static SpotifySearchCategoryResult ParseCategory(JsonElement root,
        int order,
        string key,
        bool isNotTopResults = true,
        bool isNotTrack = true)
    {

        var items = isNotTopResults ? root.GetProperty("items") : root.GetProperty("itemsV2");

        // Span<ISpotifyItem> output = new ISpotifyItem[items.GetArrayLength()];

        ulong totalCount = 0;
        if (isNotTopResults)
        {
            totalCount = root.GetProperty("totalCount").GetUInt64();
        }
        else
        {
            totalCount = (ulong)items.GetArrayLength();
        }

        var output = CreateLazy(items, isNotTopResults, isNotTrack);
        return new SpotifySearchCategoryResult
        {
            Order = order,
            Total = totalCount,
            Items = ImmutableArray.Create(output),
            Key = key
        };
    }

    private static Span<ISpotifyItem> CreateLazy(JsonElement items,
        bool isNotTopResults,
        bool isNotTrack)
    {
        using var enumerator = items.EnumerateArray();
        int i = 0;
        Span<ISpotifyItem> output = new ISpotifyItem[items.GetArrayLength()];
        while (enumerator.MoveNext())
        {
            var curr = enumerator.Current;
            if (isNotTopResults && isNotTrack)
            {
                output[i++]= Parse(curr);
            }
            else
            {
                var actualItem = curr.GetProperty("item");
                output[i++]= Parse(actualItem);
            }
        }

        return output;
    }

    private static ISpotifyItem Parse(JsonElement item)
    {
        try
        {
            var data = item.GetProperty("data");
            ReadOnlySpan<char> uri = data.GetProperty("uri").GetString();
            ReadOnlySpan<char> typeName = data.GetProperty("__typename").GetString();
            switch (typeName)
            {
                case "Album":
                {
                    return new SpotifySimpleAlbum
                    {
                        Uri = SpotifyId.FromUri(uri),
                        Name = data.GetProperty("name").GetString(),
                        Images = ParseImages(data.GetProperty("coverArt")),
                        ReleaseDate = new DateOnly(year: data.GetProperty("date").GetProperty("year").GetInt16(), 1, 1),
                        Type = "ALBUM"
                    };
                    break;
                }
                case "Artist":
                {
                    return new SpotifySimpleArtist()
                    {
                        Uri = SpotifyId.FromUri(uri),
                        Name = data.GetProperty("profile").GetProperty("name").GetString(),
                        Images = ParseImages(data.GetProperty("visuals").GetProperty("avatarImage")),
                    };
                    break;
                }
                case "Episode":
                {
                    return new SpotifySimplePodcastEpisode()
                    {
                        Uri = SpotifyId.FromUri(uri)
                    };
                    break;
                }
                case "Genre":
                {
                    return new SpotifySimpleGenre()
                    {
                        Uri = default
                    };
                    break;
                }
                case "Playlist":
                {
                    return new SpotifySimplePlaylist()
                    {
                        Uri = default
                    };
                    break;
                }
                case "Track":
                {
                    return new SpotifySimpleTrack
                    {
                        Uri = SpotifyId.FromUri(uri),
                        Name = null
                    };
                    break;
                }
                case "Podcast":
                {
                    return new SpotifySimplePodcast
                    {
                        Uri = SpotifyId.FromUri(uri)
                    };
                    break;
                }
                case "User":
                {
                    return new SpotifySimpleUser()
                    {
                        Uri = default
                    };
                    break;
                }
            }

            return default;
        }
        catch (KeyNotFoundException)
        {
            return default;
        }
    }

    private static SpotifyImage[] ParseImages(JsonElement getProperty)
    {
        try
        {
            var sources = getProperty.GetProperty("sources");
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
                    Width = item.GetProperty("width").GetUInt16(),
                    ColorDark = ParseColorDark(getProperty)
                };
                output[i++] = image;
            }

            return output;
        }
        catch (InvalidOperationException)
        {
            return Array.Empty<SpotifyImage>();
        }
    }

    private static string? ParseColorDark(JsonElement getProperty)
    {
        if (getProperty.TryGetProperty("extractedColors", out var xtr))
        {
            var colorDark = xtr.GetProperty("colorDark");
            return colorDark.GetProperty("hex").ToString();
        }

        return null;
    }
}