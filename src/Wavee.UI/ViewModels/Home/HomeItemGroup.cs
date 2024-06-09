using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Wavee.Contracts.Common;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Contracts;
using Wavee.UI.WinUI;

namespace Wavee.UI.ViewModels.Home;

public sealed partial class HomeItemGroup : ReactiveObject, IDisposable
{
    [AutoNotify] private bool _pinned;
    private readonly CompositeDisposable _disposables = new();
    private readonly ReadOnlyObservableCollection<IHomeItem> _items;

    public HomeItemGroup(
        string id,
        string title,
        bool pinned,
        int order,
        IObservable<IChangeSet<IHomeItem, ComposedKey>> changes)
    {
        _pinned = pinned;
        Id = id;
        Title = title;
        Order = order;
        changes
            .Transform(x => x)
            .Sort(SortExpressionComparer<IHomeItem>.Ascending(x => x.Order))
            .Bind(out _items)
            .Sort(SortExpressionComparer<IHomeItem>.Ascending(x => x.Order))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe()
            .DisposeWith(_disposables);
    }

    public string Id { get; }
    public string Title { get; }
    public int Order { get; }
    public ReadOnlyObservableCollection<IHomeItem> Items => _items;

    public void Dispose()
    {
        _disposables.Dispose();
    }
}