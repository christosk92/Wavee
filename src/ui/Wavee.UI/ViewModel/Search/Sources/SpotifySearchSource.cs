using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using ReactiveUI;
using Wavee.UI.Common;
using Wavee.UI.User;
using Wavee.UI.ViewModel.Search.Patterns;

namespace Wavee.UI.ViewModel.Search.Sources;

public sealed class SpotifySearchSource : ReactiveObject, ISearchSource, IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public SpotifySearchSource(UserViewModel user, Subject<string> queries)
    {
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


    private static async Task<IEnumerable<ISearchItem>> Search(string query)
    {
        var random = new Random();
        var items = new List<ISearchItem>();
        for (var i = 0; i < 10; i++)
        {
            await Task.Delay(random.Next(100, 500));
            items.Add(new FakeSearchItem(query, $"Result {i}"));
        }
        return items;
        //return new List<ISearchItem>();
    }
    public void Dispose()
    {
        _disposables.Dispose();
    }
}

internal class FakeSearchItem : ISearchItem
{
    public FakeSearchItem(string query, string s)
    {
        Name = s;
        Description = query;
        Key = new ComposedKey();
        Category = "Fake";
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