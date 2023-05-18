using DynamicData;
using ReactiveUI;
using System.Collections.ObjectModel;
using DynamicData.Binding;
using Wavee.Core.Id;
using Wavee.UI.Models;
using System.Reactive.Linq;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Infrastructure.Traits;

namespace Wavee.UI.ViewModels;

public sealed class ShellViewModel<R> : ReactiveObject where R : struct, HasFile<R>, HasDirectory<R>, HasLocalPath<R>
{
    private readonly ReadOnlyObservableCollection<PlaylistInfo> _playlistItemsView;
    private readonly SourceCache<PlaylistInfo, AudioId> _items = new(s => s.Id);
    private PlaylistSortProperty _playlistSort = PlaylistSortProperty.CustomIndex;
    private readonly R _runtime;
    public ShellViewModel(R runtime, User user)
    {
        User = user;

        _runtime = runtime;
        Instance = this;
        var sortExpressionObservable =
            this.WhenAnyValue(x => x.PlaylistSort)
                .Select(sortProperty =>
                {
                    switch (sortProperty)
                    {
                        case PlaylistSortProperty.CustomIndex:
                            return SortExpressionComparer<PlaylistInfo>.Ascending(t => t.Index);
                        case PlaylistSortProperty.Alphabetical:
                            return SortExpressionComparer<PlaylistInfo>.Ascending(t => t.Name);
                        default:
                            throw new ArgumentOutOfRangeException(nameof(sortProperty), sortProperty, null);
                    }
                });

        _items
            .Connect()
            .Sort(sortExpressionObservable)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _playlistItemsView)
            .Subscribe();
    }
    public User User { get; }
    public static ShellViewModel<R> Instance { get; private set; }
    public ReadOnlyObservableCollection<PlaylistInfo> Playlists => _playlistItemsView;

    public PlaylistSortProperty PlaylistSort
    {
        get => _playlistSort;
        set => this.RaiseAndSetIfChanged(ref _playlistSort, value);
    }
}