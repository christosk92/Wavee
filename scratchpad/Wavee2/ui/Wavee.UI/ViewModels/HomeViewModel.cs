using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Wavee.Core.Contracts;
using Wavee.Core.Ids;
using Wavee.UI.States.Spotify;

namespace Wavee.UI.ViewModels;

public sealed class HomeViewModel : ReactiveObject, IDisposable
{
    private readonly SourceList<HomeItem> _items = new();
    private readonly IDisposable _cleanup;
    public HomeViewModel()
    {
        var main = _items
            .Connect()
            .GroupOn(x => x.Group)
            .Transform(x => new HomeGrouping(x))
            .ObserveOn(RxApp.MainThreadScheduler)
            .DisposeMany()
            .Bind(this.Groups)
            .Subscribe();

        //periodically fetch new items (every 5 minutes)
        var second = Observable
            .Timer(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5))
            .SelectMany(_ => FetchItems())
            .Subscribe(items =>
            {
                _items.Edit(f =>
                {
                    f.Clear();
                    f.AddRange(items);
                });
            });

        _cleanup = Disposable.Create(() =>
        {
            main.Dispose();
            second.Dispose();
        });
    }

    private async Task<IEnumerable<HomeItem>> FetchItems()
    {
        var response = await SpotifyState.Instance
            .GetHttpJsonDocument(SpotifyEndpoints.PublicApi.DesktopHome_20_10, CancellationToken.None)
            .IfLeft((_) => throw new Exception("Failed to fetch home items"))();
        if (response.IsFaulted)
        {
            throw new Exception("Failed to fetch home items");
        }

        using var home = response.Match(Succ: js => js, Fail: _ => throw new NotSupportedException());
        var groupResults = new List<HomeItem>();
        HomeGroup? currentGroup = null;
        if (home.RootElement.TryGetProperty("content", out var ct)
            && ct.TryGetProperty("items", out var items))
        {
            using var itemsArr = items.EnumerateArray();
            foreach (var group in itemsArr)
            {
                var title = group.GetProperty("name").GetString();
                var tagline = group.TryGetProperty("tag_line", out var t) ? t.GetString() : null;
                currentGroup = new HomeGroup(group.GetProperty("id").GetString(), title, tagline);

                var content = group.GetProperty("content");
                using var itemsInGroup = content.GetProperty("items").EnumerateArray();
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
                            groupResults.Add(new HomeItem(
                                Id: AudioId.FromUri(item.GetProperty("uri").GetString()),
                                Title: item.GetProperty("name").GetString()!,
                                ImageUrl: image,
                                Subtitle: item.GetProperty("description").GetString(),
                                Group: currentGroup
                            ));
                            break;
                        case "album":
                            groupResults.Add(new HomeItem(
                                Id: AudioId.FromUri(item.GetProperty("uri").GetString()),
                                Title: item.GetProperty("name").GetString()!,
                                ImageUrl: image,
                                Subtitle: $"{item.GetProperty("total_tracks").GetInt32()} tracks",
                                Group: currentGroup));
                            break;
                        case "artist":
                            groupResults.Add(new HomeItem(
                                Id: AudioId.FromUri(item.GetProperty("uri").GetString()),
                                Title: item.GetProperty("name").GetString()!,
                                ImageUrl: image,
                                Subtitle: item.GetProperty("followers").GetProperty("total").GetInt32().ToString(),
                                Group: currentGroup
                            ));
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        return groupResults;
    }

    public ObservableCollectionExtended<HomeGrouping> Groups { get; } = new();

    public void Dispose()
    {
        _items.Dispose();
        _cleanup.Dispose();
    }
}

public sealed record HomeItem(AudioId Id, HomeGroup Group, string Title, string Subtitle, string? ImageUrl)
{
    public string IdGroupKeyComposite => $"{Id}-{Group.Id}";
}

public sealed record HomeGroup(string Id, string Title, string? TagLine);

public class HomeGrouping : ObservableCollectionExtended<HomeItem>, IGrouping<HomeGroup, HomeItem>, IDisposable
{
    private readonly IDisposable _cleanUp;
    public HomeGrouping(IGroup<HomeItem, HomeGroup> group)
    {
        if (group == null)
        {
            throw new ArgumentNullException(nameof(group));
        }

        Key = group.GroupKey;
        _cleanUp = group.List.Connect().Bind(this).Subscribe();
    }

    public HomeGroup Key { get; private set; }

    public void Dispose()
    {
        _cleanUp.Dispose();
    }
}