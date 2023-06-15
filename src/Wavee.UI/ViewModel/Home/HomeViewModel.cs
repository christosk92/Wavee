using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.UI.Core;
using Wavee.UI.Core.Contracts.Common;
using Wavee.UI.Core.Logging;
namespace Wavee.UI.ViewModel.Home;

public class HomeViewModel : ObservableObject
{
    private bool _isLoading = true;
    private static readonly IReadOnlyList<ShimmerGroup> _shimmerItems;
    private IReadOnlyList<HomeGroup> _items;

    static HomeViewModel()
    {
        _shimmerItems = Enumerable.Range(0, 10)
            .Select(i => new ShimmerGroup(Enumerable.Range(0, 10).Select(j => new ShimmerItem()).ToList())).ToList();
    }

    private HomeViewType? _viewType;
    public async Task FetchView(HomeViewType view)
    {
        if (_viewType == view)
            return;
        _viewType = view;
        const int limit = 20;
        const int contentLimit = 10;
        const string types = "track,album,playlist,playlist_v2,artist,collection_artist,collection_album";

        try
        {
            var data = await Global.AppState.Home.GetHomeViewAsync(types, limit, contentLimit, CancellationToken.None);
            Items = data;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public IReadOnlyList<HomeGroup> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }
    public IReadOnlyList<ShimmerGroup> ShimmerItems => _shimmerItems;
}

public enum HomeViewType
{
    All,
    Songs,
    Podcasts
}

public class ShimmerGroup
{
    public ShimmerGroup(IReadOnlyList<ShimmerItem> items)
    {
        Items = items;
    }

    public IReadOnlyList<ShimmerItem> Items { get; }
}

public class ShimmerItem { }