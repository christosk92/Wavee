using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using DynamicData;
using LanguageExt;
using ReactiveUI;
using Serilog;
using Wavee.Id;
using Wavee.UI.Common;
using Wavee.UI.User;
using Wavee.UI.ViewModel.Search.Patterns;

namespace Wavee.UI.ViewModel.Search.Sources;

public sealed partial class SpotifySearchSource : ReactiveObject, ISearchSource, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly UserViewModel _user;
    public SpotifySearchSource(UserViewModel user, Subject<string> queries)
    {
        _user = user;

        var filtersSourceCache = new SourceCache<FilterItem, string>(x => x.Id)
            .DisposeWith(_disposables);

        var sourceCache = new SourceCache<ISearchItem, ComposedKey>(x => x.Key)
            .DisposeWith(_disposables);

        var results = queries
            .SelectMany(query => query.Length > 0 ? Search(query) : Task.FromResult(new SpotifySearchResult(
                Filters: Array.Empty<FilterItem>(),
                Results: Enumerable.Empty<ISearchItem>()
                )))
            .ObserveOn(RxApp.MainThreadScheduler);

        sourceCache
            .RefillFrom(results.Select(x => x.Results))
            .DisposeWith(_disposables);

        filtersSourceCache
            .RefillFrom(results.Select(x => x.Filters))
            .DisposeWith(_disposables);

        Changes = sourceCache.Connect();
        Filters = filtersSourceCache.Connect();
    }

    public IObservable<IChangeSet<ISearchItem, ComposedKey>> Changes { get; }
    public IObservable<IChangeSet<FilterItem, string>> Filters { get; }

    private readonly record struct SpotifySearchResult(IReadOnlyCollection<FilterItem> Filters, IEnumerable<ISearchItem> Results);
    private async Task<SpotifySearchResult> Search(string query)
    {
        try
        {
            var result = await _user.Client.Search.GetSearchResultsAsync(query, CancellationToken.None);
            using var jsonDocument = JsonDocument.Parse(result);
            var root = jsonDocument.RootElement;
            var categoriesOrder = root.GetProperty("categoriesOrder");
            var total = categoriesOrder.GetArrayLength();
            //since limit is 10
            //capacity will be 10 * total + 1 (tophit)
            var results = root.GetProperty("results");
            bool hasTopHit = false;
            var output = new List<ISearchItem>(10 * total + 1);
            var filters = new List<FilterItem>(total + 1)
            {
                new FilterItem
                {
                    Count = -1,
                    Id = "overview",
                    Title = "Overview"
                }
            };
            for (var i = 0; i < total; i++)
            {
                var categoryName = categoriesOrder[i].GetString();
                if (results.TryGetProperty(categoryName!, out var category))
                {
                    if (categoryName is "topHit")
                    {
                        hasTopHit = true;
                    }
                    MutateItemsFromCategory(category, categoryName, i, output, filters, hasTopHit);
                }
            }


            return new SpotifySearchResult(
                filters,
                Results: output
                );
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to search");
            return new SpotifySearchResult(Array.Empty<FilterItem>(), Enumerable.Empty<ISearchItem>());
        }
    }

    private static void MutateItemsFromCategory(JsonElement category,
        string categoryName,
        int categoryOrderIndex,
        List<ISearchItem> output,
        List<FilterItem> filtersOutput,
        bool hasTopHit)
    {
        var playlistRegex = PlaylistRegex();

        var hits = category.GetProperty("hits");
        for (int i = 0; i < hits.GetArrayLength(); i++)
        {
            var hit = hits[i];
            var uri = hit.GetProperty("uri").GetString();
            //playlistRegex
            var playlistMatch = playlistRegex.Match(uri);
            if (playlistMatch is
                {
                    Success: true
                })
            {
                var playlistId = playlistMatch.Groups["playlistId"].Value;
                uri = $"spotify:playlist:{playlistId}";

                var spotifyId = SpotifyId.FromUri(uri);
                var item = ParseHit(spotifyId, hit, categoryName, categoryOrderIndex, i);
                output.Add(item);
            }
            else if (uri.StartsWith("spotify:user:"))
            {
                //regular userId
                var item = ParseHit(uri, hit, categoryName, categoryOrderIndex, i);
                output.Add(item);
            }
            else
            {
                var spotifyId = SpotifyId.FromUri(uri);
                int offset = 0;
                string newCategoryName = categoryName;
                if (categoryName is "tracks" && hasTopHit)
                {
                    categoryOrderIndex = 0;
                    newCategoryName = "topHit";
                    offset = 1;
                }
                var item = ParseHit(spotifyId, hit, newCategoryName, categoryOrderIndex,
                    i + offset);
                output.Add(item);
            }
        }

        if (categoryName is not "topHit" and not "topRecommendations")
        {
            var total = category.GetProperty("total").GetInt64();
            filtersOutput.Add(new FilterItem
            {
                Id = categoryName,
                Count = total,
                Title = categoryName
            });
        }
    }

    private static ISearchItem ParseHit(Either<SpotifyId, string> spotifyIdOrUserId,
        JsonElement hit, string categoryName,
        int categoryIndex,
        int itemIndex)
    {
        try
        {
            return spotifyIdOrUserId.Match(
                Left: id =>
                {
                    switch (id.Type)
                    {
                        case AudioItemType.Track:
                            {
                                return new SpotifyTrackHit(
                                    id: id,
                                    name: hit.GetProperty("name").GetString()!,
                                    image: hit.TryGetProperty("image", out var a) ? a.GetString()! : null,
                                    artists: ParseNameAndUriAsArr(hit.GetProperty("artists")),
                                    album: ParseNameAndUri(hit.GetProperty("album")),
                                    duration: hit.GetProperty("duration").GetUInt32(),
                                    category: categoryName,
                                    categoryIndex: categoryIndex,
                                    itemIndex: itemIndex
                                );
                            }
                        case AudioItemType.Album:
                            {
                                return new SpotifyAlbumHit(
                                    id: id,
                                    name: hit.GetProperty("name").GetString()!,
                                    image: hit.TryGetProperty("image", out var b) ? b.GetString()! : null,

                                    artists: ParseNameAndUriAsArr(hit.GetProperty("artists")),
                                    category: categoryName,
                                    categoryIndex: categoryIndex,
                                    itemIndex: itemIndex
                                );
                            }
                        case AudioItemType.Artist:
                            return new SpotifyArtistHit(
                                id: id,
                                name: hit.GetProperty("name").GetString()!,
                                image: hit.TryGetProperty("image", out var c) ? c.GetString()! : null,
                                verified: hit.GetProperty("verified").GetBoolean(),
                                category: categoryName,
                                categoryIndex: categoryIndex,
                                itemIndex: itemIndex
                            );
                            break;
                        case AudioItemType.Playlist:
                            return new SpotifyPlaylistHit(
                                id: id,
                                name: hit.GetProperty("name").GetString()!,
                                image: hit.TryGetProperty("image", out var d) ? d.GetString()! : null,
                                followersCount: hit.GetProperty("followersCount").GetUInt64(),
                                author: hit.GetProperty("author").GetString(),
                                personalized: hit.GetProperty("personalized").GetBoolean(),
                                category: categoryName,
                                categoryIndex: categoryIndex,
                                itemIndex: itemIndex
                            );
                            break;
                        case AudioItemType.PodcastEpisode:
                            break;
                        case AudioItemType.PodcastShow:
                            break;
                        case AudioItemType.UserCollection:
                            break;
                        case AudioItemType.Prerelease:
                        case AudioItemType.Concert:
                        case AudioItemType.Unknown:
                            break;
                    }

                    return new UnknownSearchItem(
                        id: id.ToString(),
                    categoryIndex: categoryIndex,
                    itemIndex: itemIndex
                    );
                },
                Right: userId =>
                {
                    return new UnknownSearchItem(
                        id: userId,
                        categoryIndex: categoryIndex,
                        itemIndex: itemIndex
                    ) as ISearchItem;
                });
        }
        catch (Exception x)
        {
            Log.Error(x, "failed to deserialize.");
            return new UnknownSearchItem(
                id: spotifyIdOrUserId.ToString(),
                categoryIndex: categoryIndex,
                itemIndex: itemIndex
            );
        }
    }

    private static NameAndUri[] ParseNameAndUriAsArr(JsonElement getProperty)
    {
        var output = new NameAndUri[getProperty.GetArrayLength()];
        using var enumerator = getProperty.EnumerateArray();
        int index = 0;
        while (enumerator.MoveNext())
        {
            var curr = enumerator.Current;
            output[index++] = ParseNameAndUri(curr);
        }

        return output;
    }

    private static NameAndUri ParseNameAndUri(JsonElement property)
    {
        var uri = property.GetProperty("uri").GetString();
        var title = property.GetProperty("name").GetString();
        return new NameAndUri
        {
            Id = uri,
            Name = title
        };
    }
    public void Dispose()
    {
        _disposables.Dispose();
    }

    [GeneratedRegex(@"spotify:user:(?<userId>.+):playlist:(?<playlistId>.+)")]
    private static partial Regex PlaylistRegex();
}


public class SpotifyPlaylistHit : ISearchItem
{
    public SpotifyPlaylistHit(SpotifyId id,
        string name,
        string image,
        ulong followersCount,
        string author,
        bool personalized,
        string category,
        int categoryIndex, int itemIndex)
    {
        Name = name;
        Key = new ComposedKey(id.ToString(), categoryIndex);
        Category = new CategoryComposite(
            Name: category,
            Index: categoryIndex);
        ItemIndex = itemIndex;
        Image = image;
        FollowersCount = followersCount;
        Author = author;
        Personalized = personalized;
        SpotifyId = id;
    }

    public string Name { get; }
    public string Image { get; }
    public ulong FollowersCount { get; }
    public string Author { get; }
    public bool Personalized { get; }
    public ComposedKey Key { get; }
    public CategoryComposite Category { get; }
    public int CategoryIndex { get; }
    public int ItemIndex { get; }
    public SpotifyId SpotifyId { get;  }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class SpotifyArtistHit : ISearchItem
{
    public SpotifyArtistHit(SpotifyId id,
        string name,
        string image,
        bool verified,
        string category,
        int categoryIndex, int itemIndex)
    {
        Name = name;
        Key = new ComposedKey(id.ToString(), categoryIndex);
        Category = new CategoryComposite(
            Name: category,
            Index: categoryIndex);
        ItemIndex = itemIndex;
        Image = image;
        Verified = verified;
        SpotifyId = id;
    }

    public string Name { get; }
    public string Image { get; }
    public bool Verified { get; }
    public ComposedKey Key { get; }
    public CategoryComposite Category { get; }
    public int CategoryIndex { get; }
    public int ItemIndex { get; }
    public SpotifyId SpotifyId { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class SpotifyAlbumHit : ISearchItem
{
    public SpotifyAlbumHit(SpotifyId id,
        string name,
        string image,
        NameAndUri[] artists,
        string category,
        int categoryIndex, int itemIndex)
    {
        Name = name;
        Key = new ComposedKey(id.ToString(), categoryIndex);
        Category = new CategoryComposite(
            Name: category,
            Index: categoryIndex);
        ItemIndex = itemIndex;
        Image = image;
        Artists = artists;
        SpotifyId = id;
    }

    public string Name { get; }
    public string Image { get; }
    public NameAndUri[] Artists { get; }
    public ComposedKey Key { get; }
    public CategoryComposite Category { get; }
    public int CategoryIndex { get; }
    public int ItemIndex { get; }
    public SpotifyId SpotifyId { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class SpotifyTrackHit : ISearchItem
{
    public SpotifyTrackHit(SpotifyId id,
        string name,
        string image,
        NameAndUri[] artists,
        NameAndUri album,
        uint duration,
        string category,
        int categoryIndex, int itemIndex)
    {
        Name = name;
        Key = new ComposedKey(id.ToString(), categoryIndex);
        Category = new CategoryComposite(
            Name: category,
            Index: categoryIndex);
        ItemIndex = itemIndex;
        Image = image;
        Artists = artists;
        Album = album;
        Duration = TimeSpan.FromMilliseconds(duration);
    }

    public string Name { get; }
    public string Image { get; }
    public NameAndUri[] Artists { get; }
    public NameAndUri Album { get; }
    public TimeSpan Duration { get; }
    public ComposedKey Key { get; }
    public CategoryComposite Category { get; }
    public int CategoryIndex { get; }
    public int ItemIndex { get; }
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}


public class NameAndUri
{
    public string Id { get; set; }
    public string Name { get; set; }
}

internal class UnknownSearchItem : ISearchItem
{
    public UnknownSearchItem(string id, int categoryIndex, int itemIndex)
    {
        Name = id;
        CategoryIndex = categoryIndex;
        ItemIndex = itemIndex;
        Description = "Unknown entity";
        Key = new ComposedKey(id, categoryIndex);
        Category = new CategoryComposite(
            Name: "unknown",
            Index: categoryIndex);
        Keywords = new[] { "unknown" };
        IsDefault = false;
    }

    public string Name { get; }
    public string Description { get; }
    public ComposedKey Key { get; }
    public string? Icon { get; set; }
    public CategoryComposite Category { get; }
    public int CategoryIndex { get; }
    public int ItemIndex { get; }
    public IEnumerable<string> Keywords { get; }
    public bool IsDefault { get; }
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}