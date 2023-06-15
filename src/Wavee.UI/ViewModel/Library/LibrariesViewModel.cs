using System.Reactive.Disposables;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using LanguageExt;
using ReactiveUI;
using Wavee.Core.Ids;
using Wavee.UI.Core;
using Wavee.UI.Core.Contracts.Library;
using static LanguageExt.Prelude;
namespace Wavee.UI.ViewModel.Library;

public class LibrariesViewModel : ObservableObject
{
    private readonly SourceCache<SpotifyLibaryItem, AudioId> _items = new(s => s.Id);
    private readonly IAppState _appState;
    private readonly CompositeDisposable _disposables;
    private int _tracksSaved;
    private int _albumsSaved;
    private int _artistsSaved;
    private ManualResetEvent _initialFetch = new(false);
    public LibrariesViewModel(IAppState appState)
    {
        _appState = appState;
        Instance = this;

        //A listener to track when items are added to the library
        var addedListener = _items
            .Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe((_) =>
            {
                AlbumsSaved = _items.Items.Count(i => i.Id.Type is AudioItemType.Album);
                ArtistsSaved = _items.Items.Count(i => i.Id.Type is AudioItemType.Artist);
                TracksSaved = _items.Items.Count(i => i.Id.Type is AudioItemType.Track);
                _initialFetch.Set();
            });

        //setup a listener for library changes
        var remoteListener = appState.Library.ListenForChanges
            .Subscribe(c =>
            {
                if (c.Removed)
                {
                    _items.Remove(c.Item);
                }
                else
                {
                    _items.AddOrUpdate(new SpotifyLibaryItem(
                        Id: c.Item,
                        AddedAt: c.AddedAt.IfNone(DateTimeOffset.UtcNow)
                    ));
                }
            });
        _disposables = new CompositeDisposable(addedListener, remoteListener);

        Task.Run(async () =>
        {
            var items = await FetchLibraryInitial(CancellationToken.None);

            foreach (var item in items)
            {
                _items.AddOrUpdate(item);
            }
        });
    }
    public static LibrariesViewModel Instance { get; private set; }

    public int TracksSaved
    {
        get => _tracksSaved;
        private set => SetProperty(ref _tracksSaved, value);
    }

    public int AlbumsSaved
    {
        get => _albumsSaved;
        private set => SetProperty(ref _albumsSaved, value);
    }

    public int ArtistsSaved
    {
        get => _artistsSaved;
        private set => SetProperty(ref _artistsSaved, value);
    }

    public bool InLibrary(AudioId id)
    {
        _initialFetch.WaitOne();

        if (_items.Lookup(id).HasValue)
        {
            return true;
        }
        return false;
    }

    public void SaveItem(Seq<AudioId> ids)
    {
        //kinda a fire and forget i guess? why is this void?

        //find which ones are already in the library
        var existing = ids.Filter(id => InLibrary(id));
        var newIds = ids.Filter(id => !InLibrary(id));

        if (existing.IsEmpty)
        {
            //if none are in the library, just add them all
            _ = Task.Run(async () => await _appState.Library.SaveItem(newIds, true, CancellationToken.None));
            return;
        }

        //if some are in the library, we need to remove those first
        _ = Task.Run(async () => await _appState.Library.SaveItem(existing, false, CancellationToken.None));

        //then add the new ones
        if (!newIds.IsEmpty)
        {
            _ = Task.Run(async () => await _appState.Library.SaveItem(newIds, true, CancellationToken.None));
        }
    }

    private async Task<Seq<SpotifyLibaryItem>> FetchLibraryInitial(CancellationToken ct = default)
    {
        //hm://collection/collection/{id}?format=json
        var tracksAndAlbums = _appState.Library.FetchTracksAndAlbums(ct).AsTask();
        var artists = _appState.Library.FetchArtists(ct).AsTask();
        var results = await Task.WhenAll(tracksAndAlbums, artists);
        return results.SelectMany(c => c).ToSeq();
    }
}