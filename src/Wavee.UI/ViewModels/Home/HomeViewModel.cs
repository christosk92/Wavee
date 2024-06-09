using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Wavee.Contracts.Common;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Clients;
using Wavee.Contracts.Interfaces.Contracts;
using Wavee.UI.Extensions;
using Wavee.UI.Services;
using Wavee.UI.WinUI;

namespace Wavee.UI.ViewModels.Home;

public sealed partial class HomeViewModel : ReactiveObject, IDisposable
{
    private readonly ReadOnlyObservableCollection<HomeItemGroup> _groups;
    private readonly SourceCache<IHomeItem, ComposedKey> _items = new(x => x.Key);
    [AutoNotify] private bool _isLoading;

    private CompositeDisposable? _disposable;
    private readonly CompositeDisposable _mainDisposable = new CompositeDisposable();

    public HomeViewModel(IObservable<IAccountClient> accountViewModel)
    {
        HomeDataRunner? homeDataRunner = null;
        RecentlyPlayedRunner? recentlyPlayedRunner = null;

        var semaphore = new SemaphoreSlim(1, 1);
        accountViewModel
            .Where(x => x is not null)
            .Select(x => (new HomeDataRunner(x, _items, TimeSpan.FromMinutes(15), semaphore),
                new RecentlyPlayedRunner(x, _items, TimeSpan.FromMinutes(15), semaphore)))
            .SelectMany(async x =>
            {
                _disposable?.Dispose();
                _disposable = new CompositeDisposable();

                homeDataRunner = x.Item1;
                recentlyPlayedRunner = x.Item2;
                _disposable.Add(homeDataRunner);
                _disposable.Add(recentlyPlayedRunner);

                Observable.FromEventPattern<TimeSpan>(homeDataRunner, nameof(HomeDataRunner.Tick))
                    .Select(_ => Unit.Default)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .SelectMany(async x =>
                    {
                        if (recentlyPlayedRunner.ExecuteTask is null)
                        {
                            await recentlyPlayedRunner.StartAsync(cancellationToken: CancellationToken.None);
                        }
                        else
                        {
                            recentlyPlayedRunner.TriggerRound();
                        }

                        return x;
                    }).Subscribe()
                    .DisposeWith(_disposable);

                homeDataRunner.IsLoading
                    .DistinctUntilChanged()
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(x => IsLoading = x)
                    .DisposeWith(_disposable);

                homeDataRunner.ExceptionTracker.LastExceptionObservable
                    .Where(x => x is not null)
                    .DistinctUntilChanged()
                    .Subscribe(x => { })
                    .DisposeWith(_disposable);

                await homeDataRunner.StartAsync(cancellationToken: CancellationToken.None);
                return Unit.Default;
            }).Subscribe()
            .DisposeWith(_mainDisposable);

        IsLoading = true;


        _items
            .Connect()
            .DisposeMany()
            .Group(s => s.Group)
            .Transform(group => new HomeItemGroup(group.Key.Id,
                group.Key.Title,
                group.Key.Pinned,
                group.Key.Order,
                group.Cache.Connect()))
            //.AutoRefresh(x => x.Pinned)
            .Sort(SortExpressionComparer<HomeItemGroup>.Ascending(x => x.Order))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _groups)
            .DisposeMany()
            .Subscribe()
            .DisposeWith(_mainDisposable);

        accountViewModel
            .Where(x=> x is not null)
            .SelectMany(client =>
                _items.Connect()
                    .WhereReasonsAre(ChangeReason.Add)
                    .Filter(item => item.Color == null)
                    .Buffer(TimeSpan.FromMilliseconds(500))
                    .SelectMany(batch =>
                        batch.SelectMany(changeSet => changeSet).Select(change => change.Current)
                            .Batch(50)) // Split buffer into chunks of 50
                    .Where(batch => batch.Count != 0)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .SelectMany(async batch =>
                    {
                        await FetchAndUpdateColors(client, batch);
                        return Unit.Default;
                    }))
            .Subscribe()
            .DisposeWith(_mainDisposable);
    }

    public ReadOnlyObservableCollection<HomeItemGroup> Groups => _groups;

    private async Task FetchAndUpdateColors(IAccountClient client, List<IHomeItem> items)
    {
        var dictionary = items.ToDictionary(item => item.MediumImageUrl, item => item.Color);
        await client!.Color.FetchColors(dictionary, CancellationToken.None);

        var allItems = _items;
        RxApp.MainThreadScheduler.Schedule(() =>
        {
            foreach (var item in items)
            {
                var lookup = allItems.Lookup(item.Key);
                if (lookup.HasValue)
                {
                    var val = lookup.Value;
                    val.Color = dictionary[item.MediumImageUrl];
                }
            }
        });
    }

    public void Dispose()
    {
        _items?.Dispose();
        _disposable?.Dispose();
        _mainDisposable.Dispose();
    }
}