using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Wavee.Core.Contracts;
using Wavee.Core.Ids;
using Wavee.UI.States.Spotify;

namespace Wavee.UI.ViewModels;

public sealed class HomeViewModel : ReactiveObject, IDisposable
{
    private readonly IDisposable _cleanup;

    public HomeViewModel()
    {
        //periodically fetch new items (every 5 minutes)
        _cleanup = Observable
            .Timer(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(3))
            .SelectMany(_ => FetchItems())
            .Subscribe(items =>
            {
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    foreach (var newGroup in items)
                    {
                        var existingGroup = Groups.FirstOrDefault(g => g.Id == newGroup.Id);
                        if (existingGroup == null)
                        {
                            // Group doesn't exist yet, add it.
                            Groups.Add(newGroup);
                        }
                        else
                        {
                            // Group already exists, update its items.
                            UpdateGroupItems(existingGroup, newGroup);
                        }

                        // Remove groups that no longer exist.
                        for (int i = Groups.Count - 1; i >= 0; i--)
                        {
                            if (!items.Any(ng => ng.Id == Groups[i].Id))
                            {
                                Groups.RemoveAt(i);
                            }
                        }
                    }
                });
            });
    }

    private void UpdateGroupItems(HomeGroup existingGroup, HomeGroup newGroup)
    {
        // Remove items that no longer exist in the new group.
        // Remove items that no longer exist in the new group.
        for (int i = existingGroup.Items.Count - 1; i >= 0; i--)
        {
            if (!newGroup.Items.Any(ni => ni.Id == existingGroup.Items[i].Id))
            {
                existingGroup.Items.RemoveAt(i);
            }
        }

        // Add or replace items that exist in the new group.
        foreach (var newItem in newGroup.Items)
        {
            var existingItem = existingGroup.Items.FirstOrDefault(ii => ii.Id == newItem.Id);
            if (existingItem != null)
            {
                // Item already exists, remove it.
                existingGroup.Items.Remove(existingItem);
            }

            // Add the item at the correct position.
            var newItemIndex = newGroup.Items.IndexOf(newItem);
            if (newItemIndex < existingGroup.Items.Count)
            {
                existingGroup.Items.Insert(newItemIndex, newItem);
            }
            else
            {
                existingGroup.Items.Add(newItem);
            }
        }
    }

    private static async Task<List<HomeGroup>> FetchItems()
    {
        var response = await SpotifyState.Instance
            .GetHttpJsonDocument(SpotifyEndpoints.PublicApi.DesktopHome_20_10, CancellationToken.None)
            .IfLeft((_) => throw new Exception("Failed to fetch home items"))();
        if (response.IsFaulted)
        {
            throw new Exception("Failed to fetch home items");
        }

        using var home = response.Match(Succ: js => js, Fail: _ => throw new NotSupportedException());
        var groupResults = new List<HomeGroup>();
        if (home.RootElement.TryGetProperty("content", out var ct)
            && ct.TryGetProperty("items", out var items))
        {
            using var itemsArr = items.EnumerateArray();
            foreach (var group in itemsArr)
            {
                var title = group.GetProperty("name").GetString();
                var tagline = group.TryGetProperty("tag_line", out var t) ? t.GetString() : null;
                var currentGroup = new HomeGroup(group.GetProperty("id").GetString(), title, tagline);

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
                            currentGroup.Items.Add(new HomeItem(
                                Id: AudioId.FromUri(item.GetProperty("uri").GetString()),
                                Title: item.GetProperty("name").GetString()!,
                                ImageUrl: image,
                                Subtitle: item.GetProperty("description").GetString()
                            ));
                            break;
                        case "album":
                            currentGroup.Items.Add(new HomeItem(
                                Id: AudioId.FromUri(item.GetProperty("uri").GetString()),
                                Title: item.GetProperty("name").GetString()!,
                                ImageUrl: image,
                                Subtitle: $"{item.GetProperty("total_tracks").GetInt32()} tracks"));
                            break;
                        case "artist":
                            currentGroup.Items.Add(new HomeItem(
                                Id: AudioId.FromUri(item.GetProperty("uri").GetString()),
                                Title: item.GetProperty("name").GetString()!,
                                ImageUrl: image,
                                Subtitle: item.GetProperty("followers").GetProperty("total").GetInt32().ToString()
                            ));
                            break;
                        default:
                            break;
                    }
                }
                groupResults.Add(currentGroup);
            }
        }

        return groupResults;
    }

    public ObservableCollectionExtended<HomeGroup> Groups { get; } = new();

    public void Dispose()
    {
        _cleanup.Dispose();
        Groups.Clear();
    }
}

public sealed class HomeItem
{
    public HomeItem(AudioId Id, string Title, string Subtitle, string? ImageUrl)
    {
        this.Id = Id;
        this.Title = Title;
        this.Subtitle = Subtitle;
        this.ImageUrl = ImageUrl;
    }

    public AudioId Id { get; init; }
    public string Title { get; init; }
    public string Subtitle { get; init; }
    public string? ImageUrl { get; init; }
}

public sealed class HomeGroup
{
    public HomeGroup(string Id, string Title, string? TagLine)
    {
        this.Id = Id;
        this.Title = Title;
        this.TagLine = TagLine;
    }

    public string Id { get; init; }
    public string Title { get; init; }
    public string? TagLine { get; init; }
    public ObservableCollection<HomeItem> Items { get; } = new();
}