using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Text.RegularExpressions;
using DynamicData;
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
        var sourceCache = new SourceCache<ISearchItem, ComposedKey>(x => x.Key)
            .DisposeWith(_disposables);

        var results = queries
            .SelectMany(query => query.Length > 0 ? Search(query) : Task.FromResult(Enumerable.Empty<ISearchItem>()))
            .ObserveOn(RxApp.MainThreadScheduler);

        sourceCache
            .RefillFrom(results)
            .DisposeWith(_disposables);

        Changes = sourceCache.Connect();
    }

    public IObservable<IChangeSet<ISearchItem, ComposedKey>> Changes { get; }


    private async Task<IEnumerable<ISearchItem>> Search(string query)
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
            var output = new List<ISearchItem>(10 * total + 1);
            for (var i = 0; i < total; i++)
            {
                var categoryName = categoriesOrder[i].GetString();
                var category = results.GetProperty(categoryName!);

                MutateItemsFromCategory(category, categoryName, output);
            }

            return output;
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to search");
            return Enumerable.Empty<ISearchItem>();
        }
    }

    private static void MutateItemsFromCategory(JsonElement category,
        string categoryName,
        List<ISearchItem> output)
    {
        var playlistRegex = PlaylistRegex();

        var hits = category.GetProperty("hits");
        for (int i = 0; i < hits.GetArrayLength(); i++)
        {
            var hit = hits[i];
            var uri = hit.GetProperty("uri").GetString();
            //playlistRegex
            var match = playlistRegex.Match(uri);
            if (match is
                {
                    Success: true
                })
            {
                var playlistId = match.Groups["playlistId"].Value;
                uri = $"spotify:playlist:{playlistId}";
            }

            var spotifyId = SpotifyId.FromUri(uri);
            var item = ParseHit(spotifyId, hit);
            output.Add(item);
        }
    }

    private static ISearchItem ParseHit(SpotifyId spotifyId, JsonElement hit)
    {
        switch (spotifyId.Type)
        {
            case AudioItemType.Track:
                break;
            case AudioItemType.Album:
                break;
            case AudioItemType.Artist:
                break;
            case AudioItemType.Playlist:
                break;
            case AudioItemType.PodcastEpisode:
                break;
            case AudioItemType.Unknown:
                break;
            case AudioItemType.PodcastShow:
                break;
            case AudioItemType.UserCollection:
                break;
            case AudioItemType.Prerelease:
                break;
            case AudioItemType.Concert:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return default;
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    [GeneratedRegex(@"spotify:user:(?<userId>.+):playlist:(?<playlistId>.+)")]
    private static partial Regex PlaylistRegex();
}

internal class FakeSearchItem : ISearchItem
{
    public FakeSearchItem(string query, string s, int i)
    {
        Name = query;
        Description = query;
        Key = new ComposedKey(s);
        Category = i.ToString();
        Keywords = new[] { "Fake" };
        IsDefault = false;
    }

    public string Name { get; }
    public string Description { get; }
    public ComposedKey Key { get; }
    public string? Icon { get; set; }
    public string Category { get; }
    public IEnumerable<string> Keywords { get; }
    public bool IsDefault { get; }
}