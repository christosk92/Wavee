using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
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
using Wavee.UI.ViewModels.Home;

namespace Wavee.UI.Services;

public sealed class HomeRunner : PeriodicRunner
{
    private readonly SourceCache<IHomeItem, ComposedKey> _items = new(x => x.Key);
    private readonly ReadOnlyObservableCollection<HomeItemGroup> _groups;
    private readonly BehaviorSubject<bool> _isLoadingSubj = new(true);
    private IAccountClient? _client;

    public HomeRunner(IObservable<IAccountClient?> client, TimeSpan period) : base(period)
    {
      
    }

    public IObservable<bool> IsLoading => _isLoadingSubj;
    public IObservable<bool> IsEmpty => _items.CountChanged.Select(x => x == 0);

    public ReadOnlyObservableCollection<HomeItemGroup> Groups => _groups;

    protected override async Task ActionAsync(CancellationToken cancel)
    {
        _isLoadingSubj.OnNext(true);
        try
        {
            var group = _items.Items
                .Select(x => x.Group)
                .FirstOrDefault(x => x.Id == SpotifyConstants.RecentlyPlayedSection);

            var recentlyPlayed = await _client!.Home.GetRecentlyPlayed(group, cancel);
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                _items.Edit(innerList =>
                {
                    var itemsToRemove = innerList.Items
                        .Where(item => item.Group.Id == SpotifyConstants.RecentlyPlayedSection);
                    innerList.Remove(itemsToRemove);

                    innerList.AddOrUpdate(recentlyPlayed);
                });
            });

            // RxApp.MainThreadScheduler.Schedule(() =>
            // {
            //     _items.Edit(innerList =>
            //     {
            //         try
            //         {
            //             innerList.Load(items);
            //         }
            //         catch (Exception e)
            //         {
            //             Debug.Write(e);
            //         }
            //     });
            // });
        }
        catch (Exception x)
        {
        }

        _isLoadingSubj.OnNext(false);
    }


    private async Task FetchAndUpdateColors(List<IHomeItem> items)
    {
        var dictionary = items.ToDictionary(item => item.MediumImageUrl, item => item.Color);
        await _client!.Color.FetchColors(dictionary, CancellationToken.None);

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

    public override void Dispose()
    {
        base.Dispose();

        _items.Dispose();
        _isLoadingSubj.Dispose();
    }
}