using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using DynamicData;
using Eum.Connections.Spotify;
using Eum.Connections.Spotify.Clients;
using Eum.Connections.Spotify.Models.Search;
using Eum.UI.Helpers;
using Eum.UI.Users;
using Eum.UI.ViewModels.Search.Patterns;
using Eum.UI.ViewModels.Search.SearchItems;
using ReactiveUI;

namespace Eum.UI.ViewModels.Search.Sources;

public class SpotifySearchSource : ReactiveObject, ISearchSource, IDisposable
{
    private const int MaxResultCount = 5;
    private const int MinQueryLength = 1;

    private readonly CompositeDisposable _disposables = new();
    private readonly SourceList<SearchGroup> _otherGroups = new();

    public SpotifySearchSource(IObservable<string> queries)
    {
        var sourceCache = new SourceCache<ISearchItem, ComposedKey>(x => x.Key)
            .DisposeWith(_disposables);

        var results = queries
            .Select(query =>
                query.Length >= MinQueryLength ? Search(query) : Task.FromResult(Enumerable.Empty<ISearchItem>()))
            .ObserveOn(RxApp.MainThreadScheduler);

        sourceCache
            .RefillFrom(results)
            .DisposeWith(_disposables);

        Changes = sourceCache.Connect();
        GroupChanges = _otherGroups
            .Connect()
            .ObserveOn(RxApp.MainThreadScheduler);
        _otherGroups.Add(new SearchGroup
        {
            Title = "All",
            Id = "all"
        });
        Changes.Subscribe(a =>
        {
            foreach (var change in a)
            {
                switch (change.Reason)
                {
                    case ChangeReason.Add:
                        if (change.Current.Category != "topHit"
                            && change.Current.Category != "topRecommendations")
                        {
                            var checkForExisting =
                                _otherGroups.Items.FirstOrDefault(a => a.Id == change.Current.Category);
                            if (checkForExisting == null)
                            {

                                _otherGroups.Add(new SearchGroup
                                {
                                    Id = change.Current.Category,
                                    Title = change.Current.Category switch
                                    {
                                        "tracks" => "Tracks",
                                        "artists" => "Artists",
                                        "albums" => "Albums",
                                        "playlists" => "Playlists",
                                        "shows" => "Shows",
                                        "audioepisodes" => "Podcasts",
                                        "profiles" => "Profiles",
                                        "genres" => "Genres & Moods",
                                        _ => $"Unknown Group ({change.Current.Category})"
                                    }
                                });
                            }
                        }
                        break;
                    case ChangeReason.Update:
                        break;
                    case ChangeReason.Remove:
                        var existingItem = _otherGroups.Items.FirstOrDefault(a => a.Id == change.Current.Category);
                        if (existingItem != null)
                        {
                            _otherGroups.Remove(existingItem);
                        }
                        break;
                    case ChangeReason.Refresh:
                        break;
                    case ChangeReason.Moved:
                        _otherGroups.Move(change.PreviousIndex, change.CurrentIndex);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }).DisposeWith(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    public IObservable<IChangeSet<ISearchItem, ComposedKey>> Changes { get; }
    public IObservable<IChangeSet<SearchGroup>> GroupChanges { get; }


    private static async Task<IEnumerable<ISearchItem>> Search(string query)
    {
        var cl = Ioc.Default.GetRequiredService<ISpotifyClient>();

        if (!_caches.TryGetValue(query, out var cached))
        {
            //check if cache item expired. Expires after 15 minutes.
            if ((DateTimeOffset.UtcNow - cached.cachedAt).TotalMinutes >= 15)
            {
                var data =
                    await cl.Search.FullSearch(new SearchRequest(query, "large", "", cl.AuthenticatedUser.CountryCode,
                        "en", cl.AuthenticatedUser.Username, MaxResultCount));
                _caches[query] = new SearchCacheEntry(data, DateTimeOffset.UtcNow);
            }
        }

        var results = new List<ISearchItem>();
        foreach (var category in cached.response.CategoriesOrder)
        {
            if (cached.response.Results.ContainsKey(category))
            {
                results.Add(cached.response.Results[category].Hits
                    .Select(a => new SpotifySearchItem("1", "1", "1", a.Id, category)));
            }
        }

        return results;
    }

    private static ConcurrentDictionary<string, SearchCacheEntry> _caches =
        new ConcurrentDictionary<string, SearchCacheEntry>();
}

internal record struct SearchCacheEntry(FullSearchResponse response, DateTimeOffset cachedAt); 
