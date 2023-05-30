using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Wavee.Core.Ids;

namespace Wavee.UI.ViewModels;

public sealed class HomeViewModel : ReactiveObject, IDisposable
{
    private readonly SourceList<HomeItem> _items = new();
    private readonly IDisposable _cleanup;
    public HomeViewModel()
    {
        _cleanup = _items
            .Connect()
            .GroupOn(x => x.Group)
            .Transform(x => new Grouping<HomeGroup, HomeItem>(x))
            .ObserveOn(RxApp.MainThreadScheduler)
            .DisposeMany()
            .Bind(this.Groups)
            .Subscribe();
    }

    public ObservableCollectionExtended<Grouping<HomeGroup, HomeItem>> Groups { get; } = new();

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

public class Grouping<TKey, TElement> : ObservableCollectionExtended<TElement>, IGrouping<TKey, TElement>, IDisposable
{
    private readonly IDisposable _cleanUp;
    public Grouping(IGroup<TElement, TKey> group)
    {
        if (group == null)
        {
            throw new ArgumentNullException(nameof(group));
        }

        Key = group.GroupKey;
        _cleanUp = group.List.Connect().Bind(this).Subscribe();
    }

    public TKey Key { get; private set; }

    public void Dispose()
    {
        _cleanUp.Dispose();
    }
}