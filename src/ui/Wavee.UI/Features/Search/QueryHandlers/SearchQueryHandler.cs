using System.Collections.Immutable;
using System.Globalization;
using Mediator;
using Wavee.Spotify.Application.Search.Queries;
using Wavee.Spotify.Common.Contracts;
using Wavee.Spotify.Domain.Album;
using Wavee.Spotify.Domain.Artist;
using Wavee.Spotify.Domain.Common;
using Wavee.UI.Features.Search.Queries;
using Wavee.UI.Features.Search.ViewModels;

namespace Wavee.UI.Features.Search.QueryHandlers;

public sealed class SearchQueryHandler : IQueryHandler<SearchQuery, IReadOnlyCollection<SearchGroupViewModel>>
{
    private readonly ISpotifyClient _spotifyClient;

    public SearchQueryHandler(ISpotifyClient spotifyClient)
    {
        _spotifyClient = spotifyClient;
    }

    public async ValueTask<IReadOnlyCollection<SearchGroupViewModel>> Handle(SearchQuery query,
        CancellationToken cancellationToken)
    {
        var items = await _spotifyClient.Search.SearchAsync(
            query: query.Query,
            offset: 0,
            limit: 10,
            numberOfTopResults: 3,
            cancellationToken: cancellationToken);

        return ParseGroups(items);
    }

    private static IReadOnlyCollection<SearchGroupViewModel> ParseGroups(SpotifySearchResult items)
    {
        Span<SearchGroupViewModel> groups = new SearchGroupViewModel[items.Items.Count];
        int i = 0;
        foreach (var group in items.Items.OrderBy(x => x.Order))
        {
            groups[i++] = new SearchGroupViewModel
            {
                Title = group.Key switch
                {
                    "top_results" => "Top Results",
                    _ => ToTitleCase(group.Key)
                },
                Total = group.Total,
                Items = ParseItems(group.Items),
                RenderingType = group.Key switch
                {
                    "top_results" => SearchGroupRenderingType.TopResults,
                    "tracks" => SearchGroupRenderingType.Tracks,
                    _ => SearchGroupRenderingType.Horizontal
                }
            };
        }

        return ImmutableArray.Create(groups);
    }

    private static string ToTitleCase(string groupKey)
    {
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(groupKey.Replace("_", " "));
    }

    private static IReadOnlyCollection<SearchItemViewModel> ParseItems(IReadOnlyCollection<ISpotifyItem> groupItems)
    {
        Span<SearchItemViewModel> items = new SearchItemViewModel[groupItems.Count];
        int i = 0;
        foreach (var item in groupItems)
        {
            SearchItemViewModel parsedItem = null;
            switch (item)
            {
                case SpotifySimpleArtist artist:
                    {
                        parsedItem = new SearchItemViewModel
                        {
                            Id = artist.Uri.ToString(),
                            Title = artist.Name,
                            Description = "Artist",
                            LargeImageUrl = GetLargeImage(artist.Images),
                            SmallImageUrl = GetSmallImage(artist.Images),
                            MediumImageUrl = GetMediumImage(artist.Images),
                            IsArtist = true,
                        };
                        break;
                    }
                case SpotifySimpleAlbum album:
                    {
                        parsedItem = new SearchItemViewModel
                        {
                            Id = album.Uri.ToString(),
                            Title = album.Name,
                            Description = $"Album / {album.ReleaseDate.Year}",
                            LargeImageUrl = GetLargeImage(album.Images),
                            SmallImageUrl = GetSmallImage(album.Images),
                            MediumImageUrl = GetMediumImage(album.Images),
                            IsArtist = false,
                        };
                        break;
                    }
                default:
                    parsedItem = new SearchItemViewModel
                    {
                        Id = null,
                        Title = null,
                        LargeImageUrl = null,
                        SmallImageUrl = null,
                        MediumImageUrl = "ms-appx:///Assets/AlbumPlaceholder.png",
                        IsArtist = false,
                        Description = null
                    };
                    break;
            }
            items[i++] = parsedItem;
        }
        return ImmutableArray.Create(items);
    }

    private static string GetMediumImage(IReadOnlyCollection<SpotifyImage> artistImages)
    {
        if (artistImages.Count is 0) return "ms-appx:///Assets/AlbumPlaceholder.png";
        var image = artistImages.OrderByDescending(x => x.Height).Skip(1).FirstOrDefault();
        return image.Url ?? artistImages.FirstOrDefault().Url ?? "ms-appx:///Assets/AlbumPlaceholder.png";
    }

    private static string GetSmallImage(IReadOnlyCollection<SpotifyImage> artistImages)
    {
        if (artistImages.Count is 0) return "ms-appx:///Assets/AlbumPlaceholder.png";
        var image = artistImages.MinBy(z => z.Height);
        return image.Url ?? "ms-appx:///Assets/AlbumPlaceholder.png";
    }

    private static string GetLargeImage(IReadOnlyCollection<SpotifyImage> artistImages)
    {
        if (artistImages.Count is 0) return "ms-appx:///Assets/AlbumPlaceholder.png";
        var image = artistImages.MaxBy(z => z.Height);
        return image.Url ?? "ms-appx:///Assets/AlbumPlaceholder.png";
    }
}