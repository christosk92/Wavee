using System;
using System.Linq;
using System.Reactive.Concurrency;
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

public class RecentlyPlayedRunner : PeriodicRunner
{
    private readonly SemaphoreSlim _semaphore;
    private readonly IAccountClient _accountClient;
    private readonly SourceCache<IHomeItem, ComposedKey> _items;

    public RecentlyPlayedRunner(IAccountClient accountClient, SourceCache<IHomeItem, ComposedKey> items,
        TimeSpan period, SemaphoreSlim semaphore) : base(period)
    {
        _accountClient = accountClient;
        _items = items;
        _semaphore = semaphore;
    }

    protected override async Task ActionAsync(CancellationToken cancel)
    {
        var group = _items.Items
            .Select(x => x.Group)
            .FirstOrDefault(x => x.Id == SpotifyConstants.RecentlyPlayedSection);

        var recentlyPlayed = await _accountClient!.Home.GetRecentlyPlayed(group, cancel);
        RxApp.MainThreadScheduler.Schedule(async () =>
        {
            await _semaphore.WaitAsync(cancel);
            _items.Edit(innerList =>
            {
                var itemsToRemove = innerList.Items
                    .Where(item => item.Group.Id == SpotifyConstants.RecentlyPlayedSection);
                innerList.Remove(itemsToRemove);
                innerList.AddOrUpdate(recentlyPlayed);
            });
            _semaphore.Release();
        });
    }
}