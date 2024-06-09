using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using ReactiveUI;
using Wavee.Contracts.Common;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Clients;
using Wavee.Contracts.Interfaces.Contracts;
using Wavee.UI.Bases;
using Wavee.UI.Spotify;

namespace Wavee.UI.Services;

public sealed class HomeDataRunner : PeriodicRunner
{
    private readonly IAccountClient _accountClient;
    private readonly SourceCache<IHomeItem, ComposedKey> _items;
    private readonly SemaphoreSlim _semaphore;
    private readonly BehaviorSubject<bool> _isLoadingSubj = new(true);
    public HomeDataRunner(IAccountClient accountClient, SourceCache<IHomeItem, ComposedKey> items, TimeSpan period,
        SemaphoreSlim semaphore) : base(period)
    {
        _accountClient = accountClient;
        _items = items;
        _semaphore = semaphore;
    }

    public IObservable<bool> IsLoading => _isLoadingSubj;

    protected override async Task ActionAsync(CancellationToken cancel)
    {
        _isLoadingSubj.OnNext(true);
        var items = await _accountClient!.Home.GetItems(cancel);
        _isLoadingSubj.OnNext(false);

        RxApp.MainThreadScheduler.Schedule(async () =>
        {
            await _semaphore.WaitAsync(cancel);
            _items.Edit(innerList =>
            {
                var itemsToLoad = items;
                var itemsToRemove = innerList.Items
                    .Where(item => item.Group.Id != SpotifyConstants.RecentlyPlayedSection);
                innerList.Remove(itemsToRemove);
                innerList.AddOrUpdate(itemsToLoad);
            });
            _semaphore.Release();
        });
    }
}