using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace Wavee.UI.ViewModels.Playlists;

public sealed class PlaylistsViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables;
    private readonly SourceCache<PlaylistOrFolder, string> _sourceCache = new SourceCache<PlaylistOrFolder, string>(x => x.Id);

    public PlaylistsViewModel()
    {
        var mapper = _sourceCache
            .Connect()
            .Transform(x =>
            {
                x.OriginalIndex = _sourceCache.Items.IndexOf(x);
                return x;
            })
            .Sort(SortExpressionComparer<PlaylistOrFolder>.Ascending(x => x.OriginalIndex))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(Items)
            .Subscribe();

        _disposables = new CompositeDisposable(mapper);
    }
    public ObservableCollectionExtended<PlaylistOrFolder> Items { get; } = new();

    public void Dispose()
    {
        _disposables.Dispose();
        _sourceCache.Dispose();
    }
}