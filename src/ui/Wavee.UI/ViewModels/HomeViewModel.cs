using System.Diagnostics;
using System.Text.Json;
using Eum.Spotify.playlist4;
using LanguageExt;
using ReactiveUI;
using Wavee.Core.Contracts;
using Wavee.Core.Ids;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Infrastructure.Traits;
using Wavee.UI.Models;

namespace Wavee.UI.ViewModels;

public sealed class HomeViewModel<R> : ReactiveObject, INavigableViewModel where R : struct, HasSpotify<R>
{
    private readonly R _runtime;
    private bool _isLoading;

    private static readonly Seq<HomeGroupView> _fakeShimmerData = Enumerable.Range(0, 4)
        .Select(_ => new HomeGroupView
        {
            Items = Enumerable.Range(0,
                    10)
                .Select(f => new SpotifyViewItem
                {
                    Id = default,
                    Title = null,
                    Image = null,
                    Description = default
                })
                .ToSeq(),
            Title = null
        }).ToSeq();

    private Seq<HomeGroupView> _items;

    public HomeViewModel(R runtime)
    {
        _runtime = runtime;
        _items = _fakeShimmerData;
        //generate a couple of rows (maybe 4)
        //of fake shimmer data (around 10)
        IsLoading = true;
    }


    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public void OnNavigatedTo(object? parameter)
    {

    }

    public void OnNavigatedFrom()
    {

    }

    public Seq<HomeGroupView> Items
    {
        get => _items;
        set => this.RaiseAndSetIfChanged(ref _items, value);
    }

    public async Task FetchAll()
    {
        const string types = "track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album";
        IsLoading = true;
        Items = _fakeShimmerData;

        var aff = await Spotify<R>.FetchDesktopHome(types).Run(_runtime);
        if (aff.IsFail)
        {
            //todo: show error
            return;
        }

        Seq<HomeGroupView> groupResults = LanguageExt.Seq<HomeGroupView>.Empty;
        using var home = aff.Match(Succ: x => x, Fail: _ => throw new InvalidOperationException());
        if (home.RootElement.TryGetProperty("content",out var ct)
            && ct.TryGetProperty("items", out var items))
        {
            using var itemsArr = items.EnumerateArray();
            foreach (var group in itemsArr)
            {
                var content = group.GetProperty("content");
                using var itemsInGroup = content.GetProperty("items").EnumerateArray();
                var result = LanguageExt.Seq<SpotifyViewItem>.Empty;
                foreach (var item in itemsInGroup)
                {
                    var type = item.GetProperty("type").GetString();
                    string? image = null; //TODO: Total violation of FP
                    if (item.TryGetProperty("images", out var imgs))
                    {
                        using var images = imgs.EnumerateArray();
                        var artwork = LanguageExt.Seq<Artwork>.Empty;
                        foreach (var jsonImage in images)
                        {
                            var h = jsonImage.TryGetProperty("height", out var height)
                                    && height.ValueKind is JsonValueKind.Number
                                ? height.GetInt32()
                                : Option<int>.None;
                            var w = jsonImage.TryGetProperty("width", out var width)
                                    && width.ValueKind is JsonValueKind.Number
                                ? width.GetInt32()
                                : Option<int>.None;

                            var url = jsonImage.GetProperty("url").GetString();
                            var relativeSize =
                                h.Map(x =>
                                    x switch
                                    {
                                        < 100 => ArtworkSizeType.Small,
                                        < 400 => ArtworkSizeType.Default,
                                        _ => ArtworkSizeType.Large
                                    }).IfNone(ArtworkSizeType.Default);

                            artwork = artwork.Add(new Artwork(url, w, h, relativeSize));
                        }

                        // we may have 3 images (large, medium, small)
                        //or 1 image (large)
                        //get medium if it exists, otherwise get large
                        image = artwork.Find(x => x.Size == ArtworkSizeType.Default)
                            .Match(x => x.Url, () => artwork.Find(x => x.Size == ArtworkSizeType.Default)
                                .Match(x => x.Url,
                                    () => artwork.HeadOrNone().Map(x => x.Url)))
                            .IfNone(string.Empty);
                    }

                    switch (type)
                    {
                        case "playlist":
                            result = result.Add(new SpotifyViewItem
                            {
                                Id = AudioId.FromUri(item.GetProperty("uri").GetString()),
                                Title = item.GetProperty("name").GetString()!,
                                Image = image,
                                Description = item.GetProperty("description").GetString()
                            });
                            break;
                        case "album":
                            result = result.Add(new SpotifyViewItem
                            {
                                Id = AudioId.FromUri(item.GetProperty("uri").GetString()),
                                Title = item.GetProperty("name").GetString()!,
                                Image = image,
                                Description = $"{item.GetProperty("total_tracks").GetInt32()} tracks"
                            });
                            break;
                        case "artist":
                            result = result.Add(new SpotifyViewItem
                            {
                                Id = AudioId.FromUri(item.GetProperty("uri").GetString()),
                                Title = item.GetProperty("name").GetString()!,
                                Image = image,
                                Description = item.GetProperty("followers").GetProperty("total").GetInt32().ToString()
                            });
                            break;
                        default:
                            break;
                    }
                }

                var title = group.GetProperty("name").GetString();
                var tagline = group.TryGetProperty("tag_line", out var t) ? t.GetString() : null;
                var groupResult = new HomeGroupView
                {
                    Items = result,
                    Title = title,
                    TagLine = tagline
                };
                groupResults = groupResults.Add(groupResult);
            }
        }

        Items = groupResults;
        IsLoading = false;
    }

    public Task FetchSongsOnly()
    {
        IsLoading = true;
        Items = _fakeShimmerData;
        return Task.CompletedTask;
    }
}

public readonly struct HomeGroupView
{
    public HomeGroupView()
    {

    }
    public required Seq<SpotifyViewItem> Items { get; init; }
    public required string Title { get; init; }
    public string? TagLine { get; init; }
}